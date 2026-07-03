using System.Data.Schedule;
using System.Event;
using System.GameSession;
using System.Collections.Generic;
using System.Linq;

namespace System.Service
{
    public sealed class GameDayProgressionService
    {
        public EventApplicationResult CompleteCurrentEvent(GameEvent currentEvent, GamePlayerData playerData, GameDate currentDay)
        {
            if (currentEvent == null || playerData == null || currentDay == null)
            {
                return null;
            }

            var before = PlayerDataSnapshot.Capture(playerData);
            currentEvent.ApplyRewards(playerData);
            SyncPlayerData(playerData, currentDay);
            var after = PlayerDataSnapshot.Capture(playerData);

            return EventApplicationResult.Create(currentEvent, before, after);
        }

        public bool Advance(GamePlayerData playerData, GameDate currentDay)
        {
            if (playerData == null || currentDay == null)
            {
                return false;
            }

            var advancedWithinDay = currentDay.ToNextActionTiming();
            if (!advancedWithinDay)
            {
                currentDay.IncrementDay();
            }

            SyncPlayerData(playerData, currentDay);
            return advancedWithinDay;
        }

        public void SyncPlayerData(GamePlayerData playerData, GameDate currentDay)
        {
            playerData.Year = currentDay.Year;
            playerData.Month = currentDay.Month;
            playerData.Day = currentDay.Day;
            playerData.Timing = currentDay.ActionTiming;
        }
    }

    public sealed class EventApplicationResult
    {
        public string EventKey;
        public int MoneyDelta;
        public int PhysicalDelta;
        public int ToughnessDelta;
        public int SmartDelta;
        public int EmpathyDelta;
        public int AppealDelta;
        public List<string> AddedClearedEventKeys = new();
        public List<string> AddedRouteFlags = new();
        public List<string> AddedMetHeroineIds = new();

        public bool HasAnyChange =>
            MoneyDelta != 0 ||
            PhysicalDelta != 0 ||
            ToughnessDelta != 0 ||
            SmartDelta != 0 ||
            EmpathyDelta != 0 ||
            AppealDelta != 0 ||
            AddedClearedEventKeys.Count > 0 ||
            AddedRouteFlags.Count > 0 ||
            AddedMetHeroineIds.Count > 0;

        public string ToDebugText()
        {
            var parts = new List<string>();

            AddDelta(parts, "Money", MoneyDelta);
            AddDelta(parts, "Physical", PhysicalDelta);
            AddDelta(parts, "Toughness", ToughnessDelta);
            AddDelta(parts, "Smart", SmartDelta);
            AddDelta(parts, "Empathy", EmpathyDelta);
            AddDelta(parts, "Appeal", AppealDelta);

            if (AddedClearedEventKeys.Count > 0)
            {
                parts.Add($"Cleared+[{string.Join(", ", AddedClearedEventKeys)}]");
            }

            if (AddedRouteFlags.Count > 0)
            {
                parts.Add($"RouteFlags+[{string.Join(", ", AddedRouteFlags)}]");
            }

            if (AddedMetHeroineIds.Count > 0)
            {
                parts.Add($"MetHeroines+[{string.Join(", ", AddedMetHeroineIds)}]");
            }

            return parts.Count == 0 ? "no rewards" : string.Join(" | ", parts);
        }

        public static EventApplicationResult Create(GameEvent currentEvent, PlayerDataSnapshot before, PlayerDataSnapshot after)
        {
            return new EventApplicationResult
            {
                EventKey = currentEvent.EventKey,
                MoneyDelta = after.Money - before.Money,
                PhysicalDelta = after.Physical - before.Physical,
                ToughnessDelta = after.Toughness - before.Toughness,
                SmartDelta = after.Smart - before.Smart,
                EmpathyDelta = after.Empathy - before.Empathy,
                AppealDelta = after.Appeal - before.Appeal,
                AddedClearedEventKeys = after.ClearedEventKeys.Except(before.ClearedEventKeys).ToList(),
                AddedRouteFlags = after.ActiveRouteFlags.Except(before.ActiveRouteFlags).ToList(),
                AddedMetHeroineIds = after.MetHeroineIds.Except(before.MetHeroineIds).ToList(),
            };
        }

        private static void AddDelta(List<string> parts, string label, int delta)
        {
            if (delta == 0)
            {
                return;
            }

            var sign = delta > 0 ? "+" : string.Empty;
            parts.Add($"{label} {sign}{delta}");
        }
    }

    public sealed class PlayerDataSnapshot
    {
        public int Money;
        public int Physical;
        public int Toughness;
        public int Smart;
        public int Empathy;
        public int Appeal;
        public List<string> ClearedEventKeys = new();
        public List<string> ActiveRouteFlags = new();
        public List<string> MetHeroineIds = new();

        public static PlayerDataSnapshot Capture(GamePlayerData playerData)
        {
            return new PlayerDataSnapshot
            {
                Money = playerData.Money,
                Physical = playerData.Physical,
                Toughness = playerData.Toughness,
                Smart = playerData.Smart,
                Empathy = playerData.Empathy,
                Appeal = playerData.Appeal,
                ClearedEventKeys = new List<string>(playerData.ClearedEventKeys ?? new List<string>()),
                ActiveRouteFlags = new List<string>(playerData.ActiveRouteFlags ?? new List<string>()),
                MetHeroineIds = new List<string>(playerData.MetHeroineIds ?? new List<string>()),
            };
        }
    }
}
