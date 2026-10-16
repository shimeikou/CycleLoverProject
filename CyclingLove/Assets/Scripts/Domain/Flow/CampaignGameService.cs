using System.Collections.Generic;
using System.Data.Schedule;
using System.Flow;
using System.GameSession;
using System.Linq;
using CyclingLove.Calendar;
using CyclingLove.Equipment;

namespace System.Service
{
    // Data-driven daily loop. Content is copied on construction and never mutated.
    public class CampaignGameService
    {
        public const int Version = PlayerSaveValidator.SchemaVersion;
        private readonly CyclingLove.Content.CampaignDefinition content;
        private readonly BikeEquipmentCatalog equipment;
        private readonly AnnualCalendarService annualCalendar;
        private readonly AnnualProgressService annualProgress;
        public CampaignGameService(
            CyclingLove.Content.CampaignDefinition definition,
            BikeEquipmentCatalog equipmentCatalog = null,
            AnnualCalendarDefinition annualCalendarDefinition = null)
        {
            content = (definition ?? throw new ArgumentNullException(nameof(definition))).Snapshot();
            content.Validate();
            equipment = equipmentCatalog?.Snapshot();
            equipment?.ValidateLoadout(content.Rules.InitialPlayer.OwnedPartIds, content.Rules.InitialPlayer.EquippedBike);
            if (annualCalendarDefinition != null)
            {
                annualCalendar = new AnnualCalendarService(annualCalendarDefinition);
                annualProgress = new AnnualProgressService(annualCalendarDefinition);
                var firstDate = new DateTime(content.StartYear, content.StartMonth, content.StartDay);
                annualCalendar.ResolveDay(firstDate);
                annualCalendar.ResolveDay(firstDate.AddDays(content.Days.Length - 1));
            }
        }
        private int DayIndex(GamePlayerData d) => (new DateTime(d.Year, d.Month, d.Day) - new DateTime(content.StartYear, content.StartMonth, content.StartDay)).Days;
        private CyclingLove.Content.CampaignDay Day(GamePlayerData d) => content.Days[DayIndex(d)];
        private CyclingLove.Content.ActionDefinition Action(string id) => content.Actions.FirstOrDefault(a => a.Id == id);
        private CyclingLove.Content.CampaignChoiceDefinition Choice(GamePlayerData d, string id) =>
            (Action(d.SelectedActionId)?.Choices ?? Array.Empty<CyclingLove.Content.CampaignChoiceDefinition>()).FirstOrDefault(c => c.Id == id);
        private CyclingLove.Content.CampaignChoiceDefinition[] Choices(GamePlayerData d) =>
            Action(d.SelectedActionId)?.Choices ?? Array.Empty<CyclingLove.Content.CampaignChoiceDefinition>();
        private string EventKey(GamePlayerData d) => $"{Day(d).Id}_{d.Timing}";
        public IReadOnlyList<System.Flow.GameActionOption> GetActionOptions(GamePlayerData d)
        {
            if (d.SavedPhase == GameFlowPhase.NovelChoice)
                return Choices(d).Select(choice => new System.Flow.GameActionOption {
                    ActionId = choice.Id, Label = choice.Label, Description = choice.Description,
                    CanExecute = choice.CanChoose(d)
                }).ToArray();
            if (d.SavedPhase == GameFlowPhase.Finished) return Array.Empty<System.Flow.GameActionOption>();
            return Day(d).ActionIds.Select(id => { var a = Action(id); return new System.Flow.GameActionOption {
                ActionId = id, Label = a.Name, Description = a.Describe(), CanExecute = CanChoose(d, id)
            }; }).ToArray();
        }

        public GamePlayerData NewGame()
        {
            var data = new GamePlayerData { FlowVersion = Version, CampaignId = content.Id, ContentVersion = content.Version, SavedPhase = GameFlowPhase.Novel, Year = content.StartYear, Month = content.StartMonth, Day = content.StartDay };
            content.Rules.InitialPlayer.Apply(data);
            equipment?.ValidateLoadout(data.OwnedPartIds, data.EquippedBike);
            data.DayStartStats = Stats(data);
            return data;
        }

        public void Validate(GamePlayerData data)
        {
            PlayerSaveMigration.UpgradeToCurrent(data);
            PlayerSaveValidator.Validate(data);
            equipment?.ValidateLoadout(data.OwnedPartIds, data.EquippedBike);
            bool legacy = string.IsNullOrEmpty(data.CampaignId) && data.ContentVersion == 0;
            if (legacy ? content.Id != "prototype" || content.Version != 1 : data.CampaignId != content.Id || data.ContentVersion != content.Version)
                throw new InvalidOperationException("セーブとシナリオのバージョンが一致しません。");
            if (data.Stamina > content.Rules.MaxStamina) throw new InvalidOperationException("体力が上限を超えています。");
            int day = DayIndex(data);
            bool valid = data.SavedPhase switch
            {
                GameFlowPhase.Novel => day >= 0 && day < content.Days.Length &&
                    (data.Timing == ActionTiming.Morning && string.IsNullOrEmpty(data.SelectedChoiceId) ||
                     data.Timing == ActionTiming.Afternoon && Day(data).ActionIds.Contains(data.SelectedActionId) &&
                     (string.IsNullOrEmpty(data.SelectedChoiceId) || Choice(data, data.SelectedChoiceId) != null)) &&
                    data.NarrativePage < Narrative(data).Length,
                GameFlowPhase.ActionSelection => day >= 0 && day < content.Days.Length && data.Timing == ActionTiming.Afternoon &&
                    data.NarrativePage == 0 && string.IsNullOrEmpty(data.SelectedChoiceId),
                GameFlowPhase.NovelChoice => day >= 0 && day < content.Days.Length && data.Timing == ActionTiming.Afternoon && data.NarrativePage == 0 &&
                    Day(data).ActionIds.Contains(data.SelectedActionId) && string.IsNullOrEmpty(data.SelectedChoiceId) && Choices(data).Length > 0,
                GameFlowPhase.DailySummary => day >= 0 && day < content.Days.Length && data.Timing == ActionTiming.Evening && data.NarrativePage == 0 &&
                    Day(data).ActionIds.Contains(data.SelectedActionId) && (Choices(data).Length == 0 || Choice(data, data.SelectedChoiceId) != null),
                GameFlowPhase.Finished => day == content.Days.Length && data.Timing == ActionTiming.Morning && data.NarrativePage == 0,
                _ => false
            };
            if (!valid) throw new InvalidOperationException("セーブデータの進行位置が不正です。");
        }

        public bool CanChoose(GamePlayerData data, string action)
        {
            int index = DayIndex(data);
            var definition = Action(action);
            return index >= 0 && index < content.Days.Length && definition != null &&
                Day(data).ActionIds.Contains(action) && data.Stamina >= definition.StaminaCost && data.Money >= definition.MoneyCost;
        }

        public bool CanChooseChoice(GamePlayerData data, string choice) =>
            data.SavedPhase == GameFlowPhase.NovelChoice && Choice(data, choice)?.CanChoose(data) == true;

        public GameSceneRequest Request(GamePlayerData data)
        {
            Validate(data);
            var phase = data.SavedPhase;
            var visual = ResolveVisual(data);
            var calendarDay = phase == GameFlowPhase.Finished
                ? null
                : annualCalendar?.ResolveDay(new DateTime(data.Year, data.Month, data.Day));
            var title = phase switch
            {
                GameFlowPhase.ActionSelection => "午後は、何をしよう？",
                GameFlowPhase.NovelChoice => "どう答える？",
                GameFlowPhase.DailySummary => $"{DayIndex(data) + 1}日目の振り返り",
                GameFlowPhase.Finished => content.EndingTitle,
                _ => data.Timing == ActionTiming.Morning ? Day(data).Title :
                    Choice(data, data.SelectedChoiceId)?.Label ?? ActionName(data.SelectedActionId)
            };
            return new GameSceneRequest
            {
                Phase = phase, SceneType = phase == GameFlowPhase.ActionSelection || phase == GameFlowPhase.NovelChoice ? GameFlowSceneType.ActionSelection :
                    phase == GameFlowPhase.DailySummary || phase == GameFlowPhase.Finished ? GameFlowSceneType.Summary : GameFlowSceneType.Novel,
                UnitySceneName = "MainGameScene", Timing = data.Timing,
                MaxStamina = content.Rules.MaxStamina,
                DayNumber = Math.Min(DayIndex(data) + 1, content.Days.Length), TotalDays = content.Days.Length,
                CurrentDateText = $"{data.Month}月{data.Day}日", DisplayName = title, Revision = data.Revision,
                SeasonId = calendarDay?.SeasonId, SeasonDisplayName = calendarDay?.SeasonDisplayName,
                IsHoliday = calendarDay?.IsHoliday == true, IsAnnualFinalDay = calendarDay?.IsFinalDay == true,
                SpecialSlotIds = calendarDay?.SpecialSlotIds ?? Array.Empty<string>(),
                EventKey = phase == GameFlowPhase.Finished ? null : EventKey(data), ActionId = data.SelectedActionId,
                ScenarioId = visual.scenario, NodeId = visual.node,
                BackgroundId = visual.background, CharacterId = visual.character,
                OutfitId = visual.outfit, ExpressionId = visual.expression,
                BodyText = phase switch
                {
                    GameFlowPhase.Novel => NarrativeBody(data),
                    GameFlowPhase.NovelChoice => string.IsNullOrWhiteSpace(Action(data.SelectedActionId).ChoicePrompt) ? "返事を選んでください。" : Action(data.SelectedActionId).ChoicePrompt,
                    GameFlowPhase.DailySummary => Summary(data),
                    GameFlowPhase.Finished => content.EndingBody,
                    _ => content.ChoiceDescription
                },
                ContinueLabel = phase == GameFlowPhase.DailySummary ? "翌日へ" : phase == GameFlowPhase.Finished ? "タイトルへ" :
                    phase == GameFlowPhase.ActionSelection || phase == GameFlowPhase.NovelChoice ? "選ぶ" : data.NarrativePage < Narrative(data).Length - 1 ? "次へ" : "読み終える"
            };
        }

        private (string scenario, string node, string background, string character, string outfit, string expression) ResolveVisual(GamePlayerData data)
        {
            if (data.SavedPhase == GameFlowPhase.Finished)
                return (content.ScenarioId, "ending", null, null, null, null);
            var day = Day(data);
            if (data.Timing == ActionTiming.Morning)
                return (
                    Pick(day.MorningScenarioId, content.ScenarioId),
                    Pick(day.MorningNodeId, $"{day.Id}_morning"),
                    day.MorningBackgroundId,
                    day.MorningCharacterId,
                    day.MorningOutfitId,
                    day.MorningExpressionId
                );
            var action = Action(data.SelectedActionId);
            var choice = Choice(data, data.SelectedChoiceId);
            return (
                Pick(choice?.ScenarioId, action?.ScenarioId, day.AfternoonScenarioId, content.ScenarioId),
                Pick(choice?.NodeId, action?.NodeId, day.AfternoonNodeId, $"{day.Id}_afternoon"),
                Pick(choice?.BackgroundId, action?.BackgroundId, day.AfternoonBackgroundId),
                Pick(choice?.CharacterId, action?.CharacterId, day.AfternoonCharacterId),
                Pick(choice?.OutfitId, action?.OutfitId, day.AfternoonOutfitId),
                Pick(choice?.ExpressionId, action?.ExpressionId, day.AfternoonExpressionId)
            );
        }

        private static string Pick(params string[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        public bool Submit(GamePlayerData data, GameSceneResult input)
        {
            Validate(data);
            if (input == null || input.ExpectedRevision != data.Revision) return false;
            switch (data.SavedPhase)
            {
                case GameFlowPhase.ActionSelection:
                    if (input.CompletionType != GameFlowCompletionType.ActionSelected || !CanChoose(data, input.SelectedActionId)) return false;
                    data.SelectedActionId = input.SelectedActionId;
                    data.SelectedChoiceId = null;
                    data.SavedPhase = GameFlowPhase.Novel;
                    break;
                case GameFlowPhase.NovelChoice:
                    if (input.CompletionType != GameFlowCompletionType.NovelChoiceSelected || !CanChooseChoice(data, input.SelectedChoiceId)) return false;
                    data.SelectedChoiceId = input.SelectedChoiceId;
                    data.SavedPhase = GameFlowPhase.Novel;
                    break;
                case GameFlowPhase.Novel:
                    if (input.CompletionType != GameFlowCompletionType.NovelClosed) return false;
                    if (data.NarrativePage < Narrative(data).Length - 1) { data.NarrativePage++; break; }
                    if (data.Timing == ActionTiming.Afternoon && string.IsNullOrEmpty(data.SelectedChoiceId) && Choices(data).Length > 0)
                    {
                        data.NarrativePage = 0;
                        data.SavedPhase = GameFlowPhase.NovelChoice;
                        break;
                    }
                    CompleteEvent(data);
                    data.NarrativePage = 0;
                    if (data.Timing == ActionTiming.Morning)
                    {
                        data.Timing = ActionTiming.Afternoon;
                        data.SavedPhase = GameFlowPhase.ActionSelection;
                    }
                    else
                    {
                        data.Timing = ActionTiming.Evening;
                        data.SavedPhase = GameFlowPhase.DailySummary;
                    }
                    break;
                case GameFlowPhase.DailySummary:
                    if (input.CompletionType != GameFlowCompletionType.SummaryClosed) return false;
                    annualProgress?.ResolveDueMilestones(data);
                    // Overnight recovery belongs to the next day's opening, not the displayed daily delta.
                    var nextDate = new DateTime(data.Year, data.Month, data.Day).AddDays(1);
                    data.Year = nextDate.Year; data.Month = nextDate.Month; data.Day = nextDate.Day;
                    data.Timing = ActionTiming.Morning;
                    data.SelectedActionId = null;
                    data.SelectedChoiceId = null;
                    data.Stamina = (int)Math.Min(content.Rules.MaxStamina, (long)data.Stamina + content.Rules.OvernightRecovery);
                    data.DayStartStats = Stats(data);
                    data.SavedPhase = DayIndex(data) >= content.Days.Length ? GameFlowPhase.Finished : GameFlowPhase.Novel;
                    break;
                default: return false;
            }
            data.Revision++;
            return true;
        }

        private void CompleteEvent(GamePlayerData d)
        {
            string key = EventKey(d);
            if (d.ClearedEventKeys.Contains(key)) return;
            if (d.Timing == ActionTiming.Morning) Day(d).MorningEffects.Apply(d, content.Rules.MaxStamina);
            else
            {
                var a = Action(d.SelectedActionId);
                d.Stamina -= a.StaminaCost; d.Money -= a.MoneyCost;
                a.Effects.Apply(d, content.Rules.MaxStamina);
                Choice(d, d.SelectedChoiceId)?.Effects.Apply(d, content.Rules.MaxStamina);
                Day(d).AfternoonEffects.Apply(d, content.Rules.MaxStamina);
            }
            d.ClearedEventKeys.Add(key);
        }

        private string NarrativeBody(GamePlayerData d)
        {
            var pages = Narrative(d);
            var body = pages[d.NarrativePage];
            if (d.NarrativePage != pages.Length - 1) return body;
            string effects = d.Timing == ActionTiming.Morning ? Day(d).MorningEffects.Describe() : Action(d.SelectedActionId).Describe();
            if (d.Timing == ActionTiming.Afternoon && Choice(d, d.SelectedChoiceId) is { } choice)
            {
                var choiceEffects = choice.Effects.Describe();
                if (!string.IsNullOrEmpty(choiceEffects)) effects = string.IsNullOrEmpty(effects) ? choiceEffects : effects + " / " + choiceEffects;
            }
            if (d.Timing == ActionTiming.Afternoon)
            {
                var daily = Day(d).AfternoonEffects.Describe();
                if (!string.IsNullOrEmpty(daily)) effects = string.IsNullOrEmpty(effects) ? daily : effects + " / " + daily;
            }
            return string.IsNullOrEmpty(effects) ? body : body + "\n［" + effects + "］";
        }
        private string[] Narrative(GamePlayerData d) => d.Timing == ActionTiming.Morning ? Day(d).MorningPages :
            !string.IsNullOrEmpty(d.SelectedChoiceId) ? Choice(d, d.SelectedChoiceId).Pages :
            Day(d).AfternoonPages.Concat(Action(d.SelectedActionId).Pages).ToArray();
        public string ActionName(string id) => Action(id)?.Name ?? "自由行動";
        private static int[] Stats(GamePlayerData d) => new[] { d.Money, d.Physical, d.Toughness, d.Smart, d.Empathy, d.Appeal, d.Stamina };
        private string Summary(GamePlayerData d)
        {
            var now = Stats(d);
            var names = new[] { "資金", "フィジカル", "タフネス", "スマート", "エンパシー", "アピール", "体力" };
            var lines = new List<string> { "午後の行動：" + ActionName(d.SelectedActionId), "" };
            if (Choice(d, d.SelectedChoiceId) is { } choice) lines.Insert(1, "会話の選択：" + choice.Label);
            for (int i = 0; i < now.Length; i++) { int delta = now[i] - d.DayStartStats[i]; if (delta != 0) lines.Add($"{names[i]}　{delta:+0;-0;0}　（現在 {now[i]}）"); }
            var heroine = d.HeroineProgresses?.FirstOrDefault(x => x != null && x.IsUnlocked);
            if (heroine != null) lines.Add($"好感度　現在 {heroine.Affection} / 100\nルート段階　{heroine.RouteStep}");
            if (!string.IsNullOrEmpty(Day(d).SummaryNote)) lines.Add("\n" + Day(d).SummaryNote);
            lines.Add($"\n翌朝は体力が{content.Rules.OvernightRecovery}回復します（上限{content.Rules.MaxStamina}）。");
            return string.Join("\n", lines);
        }
    }
}
