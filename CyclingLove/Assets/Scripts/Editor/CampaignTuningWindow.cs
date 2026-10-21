using System.Collections.Generic;
using System.Linq;
using System;
using CyclingLove.Flow;
using CyclingLove.Manager;
using CyclingLove.Services;
using CyclingLove.Session;
using CyclingLove.State;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine;

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
            rootVisualElement.Q<Button>("loadLiveButton").clicked += () => RunLive(LoadLive);
            rootVisualElement.Q<Button>("applyLiveButton").clicked += () => RunLive(ApplyLive);
            rootVisualElement.Q<Button>("startAtDateButton").clicked += () => RunLive(StartAtDate);
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
                var date = ReadDate();
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
                if (ex is not InvalidOperationException and not ArgumentException)
                    Debug.LogError("Campaign tuning evaluation failed: " + ex);
            }
        }

        private GamePlayerData BuildPlayerData(DateTime date)
        {
            var data = new GamePlayerData { Year = date.Year, Month = date.Month, Day = date.Day };
            ApplyInputs(data);
            return data;
        }

        private void ApplyInputs(GamePlayerData data)
        {
            data.Money = GetInt("moneyField");
            data.Stamina = GetInt("staminaField");
            data.Physical = GetInt("physicalField");
            data.Toughness = GetInt("toughnessField");
            data.Smart = GetInt("smartField");
            data.Empathy = GetInt("empathyField");
            data.Appeal = GetInt("appealField");
            data.RegionalContribution = GetInt("regionalContributionField");
            data.PublicAwareness = GetInt("publicAwarenessField");
            data.FoxFavor = GetInt("foxFavorField");
            data.ActiveRouteFlags = ParseIds("routeFlagsField");
            data.ClearedEventKeys = ParseIds("clearedEventsField");
            data.CompletedQuestIds = ParseIds("completedQuestsField");

            var heroineId = rootVisualElement.Q<TextField>("heroineIdField").value?.Trim();
            if (string.IsNullOrEmpty(heroineId)) return;
            var progress = data.EnsureHeroineProgress(heroineId);
            progress.Affection = GetInt("affectionField");
            progress.RouteStep = GetInt("routeStepField");
            progress.IsMet = rootVisualElement.Q<Toggle>("metToggle").value;
            progress.IsUnlocked = rootVisualElement.Q<Toggle>("unlockedToggle").value;
            data.MetHeroineIds.Remove(heroineId);
            if (progress.IsMet) data.MetHeroineIds.Add(heroineId);
        }

        private void RunLive(Action action)
        {
            if (!EditorApplication.isPlaying || GameSession.SystemConfig == null)
            {
                SetText("statusLabel", "Playを開始してタイトル画面を表示してから使ってください。");
                return;
            }
            try { action(); }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                SetText("statusLabel", "反映できませんでした: " + ex.Message);
            }
            catch (Exception ex)
            {
                SetText("statusLabel", "反映できませんでした: " + ex.Message);
                Debug.LogError("Campaign debug operation failed: " + ex);
            }
        }

        private void LoadLive()
        {
            var data = GameSession.PlayerData ?? throw new InvalidOperationException("Play中のゲームがありません。");
            SetInt("yearField", data.Year);
            SetInt("monthField", data.Month);
            SetInt("dayField", data.Day);
            SetInt("moneyField", data.Money);
            SetInt("staminaField", data.Stamina);
            SetInt("physicalField", data.Physical);
            SetInt("toughnessField", data.Toughness);
            SetInt("smartField", data.Smart);
            SetInt("empathyField", data.Empathy);
            SetInt("appealField", data.Appeal);
            SetInt("regionalContributionField", data.RegionalContribution);
            SetInt("publicAwarenessField", data.PublicAwareness);
            SetInt("foxFavorField", data.FoxFavor);
            SetIds("routeFlagsField", data.ActiveRouteFlags);
            SetIds("clearedEventsField", data.ClearedEventKeys);
            SetIds("completedQuestsField", data.CompletedQuestIds);

            var heroineField = rootVisualElement.Q<TextField>("heroineIdField");
            var progress = data.FindHeroineProgress(heroineField.value?.Trim()) ??
                data.HeroineProgresses.FirstOrDefault(value => value != null && value.IsUnlocked);
            if (progress != null)
            {
                heroineField.value = progress.HeroineId;
                SetInt("affectionField", progress.Affection);
                SetInt("routeStepField", progress.RouteStep);
                rootVisualElement.Q<Toggle>("metToggle").value = progress.IsMet;
                rootVisualElement.Q<Toggle>("unlockedToggle").value = progress.IsUnlocked;
            }
            Evaluate();
            SetText("statusLabel", "Play中の値を読み込みました。");
        }

        private void ApplyLive()
        {
            var live = GameSession.PlayerData ?? throw new InvalidOperationException("Play中のゲームがありません。");
            var data = JsonUtility.FromJson<GamePlayerData>(JsonUtility.ToJson(live));
            ApplyInputs(data);
            data.Revision++;
            CreateFlow().Validate(data);
            ReplaceSession(data);
            SetText("statusLabel", "Play中の値を書き換えました。日付と進行位置は変えていません。");
        }

        private void StartAtDate()
        {
            var date = ReadDate();
            var flow = CreateFlow();
            var data = flow.NewGame(GameSession.SystemConfig.Profile);
            ApplyInputs(data);
            flow.MoveToMorning(data, date);
            ReplaceSession(data);
            SetText("statusLabel", $"{date:yyyy-MM-dd} の朝から開始しました。保存すると通常のセーブに上書きされます。");
        }

        private static CampaignGameService CreateFlow() => new(
            CampaignContentLoader.LoadAnnual(), EquipmentContentLoader.LoadRuntime(), AnnualCalendarContentLoader.LoadRuntime());

        private static void ReplaceSession(GamePlayerData data)
        {
            GameSession.PlayerData = data;
            GameFlowManager.Instance.BuildNextRequest();
            SceneManager.LoadScene("MainGameScene");
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

        private DateTime ReadDate()
        {
            var year = GetInt("yearField");
            var month = GetInt("monthField");
            var day = GetInt("dayField");
            if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
                throw new InvalidOperationException($"日付が正しくありません: {year}/{month}/{day}");
            return new DateTime(year, month, day);
        }

        private int GetInt(string name) => rootVisualElement.Q<IntegerField>(name).value;
        private void SetInt(string name, int value) => rootVisualElement.Q<IntegerField>(name).value = value;
        private void SetText(string name, string value) => rootVisualElement.Q<Label>(name).text = value;
        private void SetIds(string name, IEnumerable<string> values) =>
            rootVisualElement.Q<TextField>(name).value = string.Join("\n", values ?? Array.Empty<string>());

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
