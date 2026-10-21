using System;
using System.Collections.Generic;
using System.GameSession;
using System.Linq;
using System.Service;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CyclingLove.Editor
{
    public sealed class CampaignTuningWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/Scripts/Editor/CampaignTuningWindow.uxml";
        private const string DefaultHeroineId = "hero_bikeshop";

        [MenuItem("Tools/Cycling Love/Campaign Tuning")]
        public static void ShowWindow()
        {
            var window = GetWindow<CampaignTuningWindow>();
            window.titleContent = new GUIContent("Campaign Tuning");
            window.minSize = new Vector2(720, 620);
        }

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                rootVisualElement.Add(new Label("Campaign tuning layout is missing."));
                return;
            }

            visualTree.CloneTree(rootVisualElement);
            SetDefaults();
            rootVisualElement.Q<Button>("evaluateButton").clicked += Evaluate;
            Evaluate();
        }

        private void SetDefaults()
        {
            SetInt("yearField", 2026);
            SetInt("monthField", 4);
            SetInt("dayField", 1);
            SetInt("moneyField", 1000);
            SetInt("staminaField", 100);
            SetInt("physicalField", 5);
            SetInt("toughnessField", 5);
            SetInt("smartField", 5);
            SetInt("empathyField", 5);
            SetInt("appealField", 5);
            rootVisualElement.Q<TextField>("heroineIdField").value = DefaultHeroineId;
        }

        private void Evaluate()
        {
            try
            {
                var date = new DateTime(GetInt("yearField"), GetInt("monthField"), GetInt("dayField"));
                var playerData = BuildPlayerData(date);
                var report = CampaignTuningEvaluator.Evaluate(
                    CampaignContentLoader.LoadAnnual(),
                    AnnualCalendarContentLoader.LoadRuntime(),
                    playerData,
                    date);
                Render(report);
            }
            catch (Exception ex)
            {
                SetText("statusLabel", "入力またはデータを確認してください: " + ex.Message);
                SetText("dayResult", string.Empty);
                SetText("eventResult", string.Empty);
                SetText("choiceResult", string.Empty);
                SetText("goalResult", string.Empty);
                SetText("endingResult", string.Empty);
                Debug.LogError("Campaign tuning evaluation failed: " + ex);
            }
        }

        private GamePlayerData BuildPlayerData(DateTime date)
        {
            var data = new GamePlayerData
            {
                Year = date.Year,
                Month = date.Month,
                Day = date.Day,
                Money = GetInt("moneyField"),
                Stamina = GetInt("staminaField"),
                Physical = GetInt("physicalField"),
                Toughness = GetInt("toughnessField"),
                Smart = GetInt("smartField"),
                Empathy = GetInt("empathyField"),
                Appeal = GetInt("appealField"),
                RegionalContribution = GetInt("regionalContributionField"),
                PublicAwareness = GetInt("publicAwarenessField"),
                FoxFavor = GetInt("foxFavorField"),
                ActiveRouteFlags = ParseIds("routeFlagsField"),
                ClearedEventKeys = ParseIds("clearedEventsField"),
                CompletedQuestIds = ParseIds("completedQuestsField")
            };

            var heroineId = rootVisualElement.Q<TextField>("heroineIdField").value?.Trim();
            if (!string.IsNullOrEmpty(heroineId))
            {
                data.HeroineProgresses.Add(new HeroineProgress
                {
                    HeroineId = heroineId,
                    Affection = GetInt("affectionField"),
                    RouteStep = GetInt("routeStepField"),
                    IsMet = rootVisualElement.Q<Toggle>("metToggle").value,
                    IsUnlocked = rootVisualElement.Q<Toggle>("unlockedToggle").value
                });
                if (data.HeroineProgresses[0].IsMet) data.MetHeroineIds.Add(heroineId);
            }

            return data;
        }

        private void Render(CampaignTuningReport report)
        {
            var slots = report.Day.SpecialSlotIds.Length == 0
                ? "なし"
                : string.Join(", ", report.Day.SpecialSlotIds);
            SetText("statusLabel", "評価完了。セーブデータは変更していません。");
            SetText("dayResult",
                $"{report.Day.Date:yyyy-MM-dd} / {report.Day.SeasonDisplayName} / " +
                $"{(report.Day.IsHoliday ? "休日" : "平日")} / 特別枠: {slots}");

            var eventLines = report.MatchingEvents.Length == 0
                ? new[] { "成立イベントなし" }
                : report.MatchingEvents.Select((value, index) =>
                    $"{(index == 0 ? "採用" : "候補")}  P{value.Priority}  {value.Id}  [{value.Day.Title}]");
            SetText("eventResult", string.Join("\n", eventLines));

            var choiceLines = report.Choices.Length == 0
                ? new[] { "選択肢なし" }
                : report.Choices.Select(value =>
                {
                    var conditions = value.Conditions.Length == 0
                        ? "条件なし"
                        : string.Join(" / ", value.Conditions.Select(condition => condition.DisplayText));
                    return $"{(value.CanChoose ? "選択可" : "選択不可")}  {value.ChoiceId}  {value.Label}\n  {conditions}";
                });
            SetText("choiceResult", string.Join("\n", choiceLines));

            SetText("goalResult", string.Join("\n", report.SeasonGoals.Select(value =>
                $"{value.SeasonId}: {(value.Succeeded ? "達成" : "未達")} / {value.ResultFlag} / 締切 {value.Deadline:yyyy-MM-dd}")));

            SetText("endingResult",
                $"{report.Ending.Id} / {(report.Ending.IsSuccess ? "成功" : "未達")} / " +
                $"Heroine: {TextOrDash(report.Ending.HeroineId)} / Flag: {report.Ending.ResultFlag}");
        }

        private int GetInt(string name) => rootVisualElement.Q<IntegerField>(name).value;
        private void SetInt(string name, int value) => rootVisualElement.Q<IntegerField>(name).value = value;
        private void SetText(string name, string value) => rootVisualElement.Q<Label>(name).text = value;

        private List<string> ParseIds(string fieldName) =>
            rootVisualElement.Q<TextField>(fieldName).value
                .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

        private static string TextOrDash(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}
