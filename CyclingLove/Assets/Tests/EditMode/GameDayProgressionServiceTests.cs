using NUnit.Framework;
using System.Data.Schedule;
using System.Event;
using System.GameSession;
using System.Service;

namespace Tests.EditMode
{
    public class GameDayProgressionServiceTests
    {
        [Test]
        public void Advance_MovesThroughTimingsAndThenAdvancesDay()
        {
            var service = new GameDayProgressionService();
            var playerData = new GamePlayerData
            {
                Year = 2026,
                Month = 4,
                Day = 1,
                Timing = ActionTiming.Morning
            };

            var currentDay = new GameDate
            {
                Year = 2026,
                Month = 4,
                Day = 1,
                ActionTiming = ActionTiming.Morning
            };

            Assert.That(service.Advance(playerData, currentDay), Is.True);
            Assert.That(currentDay.ActionTiming, Is.EqualTo(ActionTiming.Afternoon));
            Assert.That(playerData.Timing, Is.EqualTo(ActionTiming.Afternoon));

            Assert.That(service.Advance(playerData, currentDay), Is.True);
            Assert.That(currentDay.ActionTiming, Is.EqualTo(ActionTiming.Evening));

            Assert.That(service.Advance(playerData, currentDay), Is.True);
            Assert.That(currentDay.ActionTiming, Is.EqualTo(ActionTiming.Special));

            Assert.That(service.Advance(playerData, currentDay), Is.False);
            Assert.That(currentDay.ActionTiming, Is.EqualTo(ActionTiming.Morning));
            Assert.That(currentDay.Day, Is.EqualTo(2));
            Assert.That(playerData.Day, Is.EqualTo(2));
            Assert.That(playerData.Timing, Is.EqualTo(ActionTiming.Morning));
        }

        [Test]
        public void CompleteCurrentEvent_AppliesRewardsAndSyncsPlayerDate()
        {
            var service = new GameDayProgressionService();
            var playerData = new GamePlayerData
            {
                Year = 2026,
                Month = 4,
                Day = 1,
                Timing = ActionTiming.Morning,
                Money = 1000
            };

            var currentDay = new GameDate
            {
                Year = 2026,
                Month = 4,
                Day = 3,
                ActionTiming = ActionTiming.Evening
            };

            var gameEvent = new GameEvent
            {
                EventKey = "evt_2026_04_03_evening_001",
                TargetHeroineId = "hero_bikeshop",
                Rewards =
                {
                    new EventReward { Type = RewardType.Money, IntValue = 250 },
                    new EventReward { Type = RewardType.MarkHeroineMet },
                    new EventReward { Type = RewardType.ClearEvent }
                }
            };

            service.CompleteCurrentEvent(gameEvent, playerData, currentDay);

            Assert.That(playerData.Money, Is.EqualTo(1250));
            Assert.That(playerData.MetHeroineIds, Does.Contain("hero_bikeshop"));
            Assert.That(playerData.ClearedEventKeys, Does.Contain("evt_2026_04_03_evening_001"));
            Assert.That(playerData.Day, Is.EqualTo(3));
            Assert.That(playerData.Timing, Is.EqualTo(ActionTiming.Evening));
        }
    }
}
