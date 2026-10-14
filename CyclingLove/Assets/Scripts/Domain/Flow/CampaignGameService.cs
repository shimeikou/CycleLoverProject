using System.Collections.Generic;
using System.Data.Schedule;
using System.Flow;
using System.GameSession;
using System.Linq;

namespace System.Service
{
    // Data-driven daily loop. Content is copied on construction and never mutated.
    public class CampaignGameService
    {
        public const int Version = PlayerSaveValidator.SchemaVersion;
        private readonly CyclingLove.Content.CampaignDefinition content;
        public CampaignGameService(CyclingLove.Content.CampaignDefinition definition)
        {
            content = (definition ?? throw new ArgumentNullException(nameof(definition))).Snapshot();
            content.Validate();
        }
        private int DayIndex(GamePlayerData d) => (new DateTime(d.Year, d.Month, d.Day) - new DateTime(content.StartYear, content.StartMonth, content.StartDay)).Days;
        private CyclingLove.Content.CampaignDay Day(GamePlayerData d) => content.Days[DayIndex(d)];
        private CyclingLove.Content.ActionDefinition Action(string id) => content.Actions.FirstOrDefault(a => a.Id == id);
        private string EventKey(GamePlayerData d) => $"{Day(d).Id}_{d.Timing}";
        public IReadOnlyList<System.Flow.GameActionOption> GetActionOptions(GamePlayerData d) =>
            d.SavedPhase == GameFlowPhase.Finished ? Array.Empty<System.Flow.GameActionOption>() :
            Day(d).ActionIds.Select(id => { var a = Action(id); return new System.Flow.GameActionOption {
                ActionId = id, Label = a.Name, Description = a.Describe(), CanExecute = CanChoose(d, id)
            }; }).ToArray();

        public GamePlayerData NewGame()
        {
            var data = new GamePlayerData { FlowVersion = Version, CampaignId = content.Id, ContentVersion = content.Version, SavedPhase = GameFlowPhase.Novel, Year = content.StartYear, Month = content.StartMonth, Day = content.StartDay };
            data.DayStartStats = Stats(data);
            return data;
        }

        public void Validate(GamePlayerData data)
        {
            PlayerSaveValidator.Validate(data);
            bool legacy = string.IsNullOrEmpty(data.CampaignId) && data.ContentVersion == 0;
            if (legacy ? content.Id != "prototype" || content.Version != 1 : data.CampaignId != content.Id || data.ContentVersion != content.Version)
                throw new InvalidOperationException("セーブとシナリオのバージョンが一致しません。");
            int day = DayIndex(data);
            bool valid = data.SavedPhase switch
            {
                GameFlowPhase.Novel => day >= 0 && day < content.Days.Length &&
                    (data.Timing == ActionTiming.Morning || data.Timing == ActionTiming.Afternoon && Day(data).ActionIds.Contains(data.SelectedActionId)) &&
                    data.NarrativePage < Narrative(data).Length,
                GameFlowPhase.ActionSelection => day >= 0 && day < content.Days.Length && data.Timing == ActionTiming.Afternoon && data.NarrativePage == 0,
                GameFlowPhase.DailySummary => day >= 0 && day < content.Days.Length && data.Timing == ActionTiming.Evening && data.NarrativePage == 0 && Day(data).ActionIds.Contains(data.SelectedActionId),
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

        public GameSceneRequest Request(GamePlayerData data)
        {
            Validate(data);
            var phase = data.SavedPhase;
            var title = phase switch
            {
                GameFlowPhase.ActionSelection => "午後は、何をしよう？",
                GameFlowPhase.DailySummary => $"{DayIndex(data) + 1}日目の振り返り",
                GameFlowPhase.Finished => content.EndingTitle,
                _ => data.Timing == ActionTiming.Morning ? Day(data).Title : ActionName(data.SelectedActionId)
            };
            return new GameSceneRequest
            {
                Phase = phase, SceneType = phase == GameFlowPhase.ActionSelection ? GameFlowSceneType.ActionSelection :
                    phase == GameFlowPhase.DailySummary || phase == GameFlowPhase.Finished ? GameFlowSceneType.Summary : GameFlowSceneType.Novel,
                UnitySceneName = "MainGameScene", Timing = data.Timing,
                DayNumber = Math.Min(DayIndex(data) + 1, content.Days.Length), TotalDays = content.Days.Length,
                CurrentDateText = $"{data.Month}月{data.Day}日", DisplayName = title, Revision = data.Revision,
                EventKey = phase == GameFlowPhase.Finished ? null : EventKey(data), ActionId = data.SelectedActionId,
                BodyText = phase switch
                {
                    GameFlowPhase.Novel => Narrative(data)[data.NarrativePage],
                    GameFlowPhase.DailySummary => Summary(data),
                    GameFlowPhase.Finished => content.EndingBody,
                    _ => content.ChoiceDescription
                },
                ContinueLabel = phase == GameFlowPhase.DailySummary ? "翌日へ" : phase == GameFlowPhase.Finished ? "タイトルへ" : phase == GameFlowPhase.ActionSelection ? "選ぶ" : data.NarrativePage < Narrative(data).Length - 1 ? "次へ" : "読み終える"
            };
        }

        public bool Submit(GamePlayerData data, GameSceneResult input)
        {
            Validate(data);
            if (input == null || input.ExpectedRevision != data.Revision) return false;
            switch (data.SavedPhase)
            {
                case GameFlowPhase.ActionSelection:
                    if (input.CompletionType != GameFlowCompletionType.ActionSelected || !CanChoose(data, input.SelectedActionId)) return false;
                    data.SelectedActionId = input.SelectedActionId;
                    data.SavedPhase = GameFlowPhase.Novel;
                    break;
                case GameFlowPhase.Novel:
                    if (input.CompletionType != GameFlowCompletionType.NovelClosed) return false;
                    if (data.NarrativePage < Narrative(data).Length - 1) { data.NarrativePage++; break; }
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
                    // Overnight recovery belongs to the next day's opening, not the displayed daily delta.
                    var nextDate = new DateTime(data.Year, data.Month, data.Day).AddDays(1);
                    data.Year = nextDate.Year; data.Month = nextDate.Month; data.Day = nextDate.Day;
                    data.Timing = ActionTiming.Morning;
                    data.SelectedActionId = null;
                    data.Stamina = Math.Min(100, data.Stamina + content.OvernightRecovery);
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
            if (d.Timing == ActionTiming.Morning) Day(d).MorningEffects.Apply(d);
            else
            {
                var a = Action(d.SelectedActionId);
                d.Stamina -= a.StaminaCost; d.Money -= a.MoneyCost;
                a.Effects.Apply(d);
                Day(d).AfternoonEffects.Apply(d);
            }
            d.ClearedEventKeys.Add(key);
        }

        private string[] Narrative(GamePlayerData d) => d.Timing == ActionTiming.Morning ? Day(d).MorningPages :
            Day(d).AfternoonPages.Concat(Action(d.SelectedActionId).Pages).ToArray();
        public string ActionName(string id) => Action(id)?.Name ?? "自由行動";
        private static int[] Stats(GamePlayerData d) => new[] { d.Money, d.Physical, d.Toughness, d.Smart, d.Empathy, d.Appeal, d.Stamina };
        private string Summary(GamePlayerData d)
        {
            var now = Stats(d);
            var names = new[] { "資金", "フィジカル", "タフネス", "スマート", "エンパシー", "アピール", "体力" };
            var lines = new List<string> { "午後の行動：" + ActionName(d.SelectedActionId), "" };
            for (int i = 0; i < now.Length; i++) { int delta = now[i] - d.DayStartStats[i]; if (delta != 0) lines.Add($"{names[i]}　{delta:+0;-0;0}　（現在 {now[i]}）"); }
            if (!string.IsNullOrEmpty(Day(d).SummaryNote)) lines.Add("\n" + Day(d).SummaryNote);
            lines.Add($"\n翌朝は体力が{content.OvernightRecovery}回復します（上限100）。");
            return string.Join("\n", lines);
        }
    }
}
