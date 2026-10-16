using System.Collections.Generic;
using System.Data.Schedule;
using System.GameSession;

namespace System.Event
{
    [Serializable]
    public class GameEvent
    {
        public string EventKey;
        public GameEventType Type = GameEventType.None;
        public string ScenarioId;
        public string ActionId;
        public string TargetHeroineId;
        public string DisplayName;
        public string Description;
        public bool AutoAdvance = true;
        public List<EventCondition> Conditions = new();
        public List<EventReward> Rewards = new();
        public GameEventState State = GameEventState.None;

        public GameEventType Execute()
        {
            return Type;
        }

        public bool CanExecute(GamePlayerData playerData, GameDate currentDate)
        {
            if (Conditions == null || Conditions.Count == 0)
            {
                return true;
            }

            foreach (var condition in Conditions)
            {
                if (!condition.IsSatisfied(playerData, currentDate))
                {
                    return false;
                }
            }

            return true;
        }

        public void ApplyRewards(GamePlayerData playerData)
        {
            if (Rewards == null)
            {
                return;
            }

            foreach (var reward in Rewards)
            {
                reward.Apply(playerData, EventKey, TargetHeroineId);
            }
        }

        public void Start()
        {
            State = GameEventState.Start;
        }

        public bool Running()
        {
            State = GameEventState.Running;
            return true;
        }

        public void Finish()
        {
            State = GameEventState.Finish;
        }
    }

    public enum GameEventType
    {
        None,
        Scenario,
        FreeTime,
        Event,
        Shop,
        Race,
        System,
        Summary,
    }

    public enum GameEventState
    {
        None,
        Start,
        Running,
        Finish,
    }

    public enum ConditionType
    {
        None,
        Always,
        YearEquals,
        MonthEquals,
        DayEquals,
        TimingEquals,
        IsHoliday,
        MinPhysical,
        MinToughness,
        MinSmart,
        MinEmpathy,
        MinAppeal,
        EventCleared,
        RouteFlagActive,
        HeroineMet,
    }

    public enum RewardType
    {
        None,
        Money,
        Physical,
        Toughness,
        Smart,
        Empathy,
        Appeal,
        ClearEvent,
        AddRouteFlag,
        MarkHeroineMet,
    }

    [Serializable]
    public class EventCondition
    {
        public ConditionType Type = ConditionType.None;
        public int IntValue;
        public bool BoolValue;
        public string StringValue;
        public ActionTiming TimingValue = ActionTiming.Morning;

        public bool IsSatisfied(GamePlayerData playerData, GameDate currentDate)
        {
            return Type switch
            {
                ConditionType.None => true,
                ConditionType.Always => true,
                ConditionType.YearEquals => currentDate.Year == IntValue,
                ConditionType.MonthEquals => currentDate.Month == IntValue,
                ConditionType.DayEquals => currentDate.Day == IntValue,
                ConditionType.TimingEquals => currentDate.ActionTiming == TimingValue,
                ConditionType.IsHoliday => BoolValue == IsHoliday(currentDate),
                ConditionType.MinPhysical => playerData.Physical >= IntValue,
                ConditionType.MinToughness => playerData.Toughness >= IntValue,
                ConditionType.MinSmart => playerData.Smart >= IntValue,
                ConditionType.MinEmpathy => playerData.Empathy >= IntValue,
                ConditionType.MinAppeal => playerData.Appeal >= IntValue,
                ConditionType.EventCleared => playerData.ClearedEventKeys?.Contains(StringValue) == true,
                ConditionType.RouteFlagActive => playerData.ActiveRouteFlags?.Contains(StringValue) == true,
                ConditionType.HeroineMet => playerData.MetHeroineIds?.Contains(StringValue) == true,
                _ => false,
            };
        }

        private static bool IsHoliday(GameDate currentDate)
        {
            return currentDate.ActionTiming == ActionTiming.Special ||
                   currentDate.ToString().Contains("Sun") ||
                   currentDate.ToString().Contains("Sat");
        }
    }

    [Serializable]
    public class EventReward
    {
        public RewardType Type = RewardType.None;
        public int IntValue;
        public string StringValue;

        public void Apply(GamePlayerData playerData, string currentEventKey, string heroineId)
        {
            playerData.ClearedEventKeys ??= new List<string>();
            playerData.ActiveRouteFlags ??= new List<string>();
            playerData.MetHeroineIds ??= new List<string>();

            switch (Type)
            {
                case RewardType.None:
                    break;
                case RewardType.Money:
                    playerData.Money += IntValue;
                    break;
                case RewardType.Physical:
                    playerData.Physical += IntValue;
                    break;
                case RewardType.Toughness:
                    playerData.Toughness += IntValue;
                    break;
                case RewardType.Smart:
                    playerData.Smart += IntValue;
                    break;
                case RewardType.Empathy:
                    playerData.Empathy += IntValue;
                    break;
                case RewardType.Appeal:
                    playerData.Appeal += IntValue;
                    break;
                case RewardType.ClearEvent:
                    AddUnique(playerData.ClearedEventKeys, string.IsNullOrWhiteSpace(StringValue) ? currentEventKey : StringValue);
                    break;
                case RewardType.AddRouteFlag:
                    AddUnique(playerData.ActiveRouteFlags, StringValue);
                    break;
                case RewardType.MarkHeroineMet:
                    var metId = string.IsNullOrWhiteSpace(StringValue) ? heroineId : StringValue;
                    AddUnique(playerData.MetHeroineIds, metId);
                    if (!string.IsNullOrWhiteSpace(metId))
                    {
                        var progress = playerData.EnsureHeroineProgress(metId);
                        progress.IsMet = true;
                        progress.IsUnlocked = true;
                        if (progress.RouteStep == 0) progress.RouteStep = 1;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || values.Contains(value))
            {
                return;
            }

            values.Add(value);
        }
    }
}
