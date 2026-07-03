using NUnit.Framework;
using System.Data.Schedule;
using System.Event;
using System.GameSession;
using System.Service;

namespace Tests.EditMode
{
    public class GameEventResolverServiceTests
    {
        [Test]
        public void ResolveEvent_ReturnsExpectedSampleEventForDateAndTiming()
        {
            var resolver = new GameEventResolverService();
            var playerData = new GamePlayerData();
            var currentDate = new GameDate
            {
                Year = 2026,
                Month = 4,
                Day = 3,
                ActionTiming = ActionTiming.Afternoon
            };

            var scheduleDay = SampleScheduleFactory.CreateInitialThreeDays()[2];
            var eventCatalog = SampleGameEventFactory.CreateInitialThreeDays();

            var gameEvent = resolver.ResolveEvent(
                scheduleDay,
                ActionTiming.Afternoon,
                eventCatalog,
                playerData,
                currentDate);

            Assert.That(gameEvent, Is.Not.Null);
            Assert.That(gameEvent.EventKey, Is.EqualTo("evt_2026_04_03_afternoon_001"));
            Assert.That(gameEvent.TargetHeroineId, Is.EqualTo("hero_bikeshop"));
        }

        [Test]
        public void ResolveEvent_UsesFallbackWhenFixedEventCannotExecute()
        {
            var resolver = new GameEventResolverService();
            var playerData = new GamePlayerData();
            var currentDate = new GameDate
            {
                Year = 2026,
                Month = 4,
                Day = 10,
                ActionTiming = ActionTiming.Afternoon
            };

            var scheduleDay = new ScheduleDay
            {
                Year = 2026,
                Month = 4,
                Day = 10,
                Slots =
                {
                    new ScheduleSlot(ActionTiming.Afternoon, ScheduleSlotType.ConditionalEvent)
                    {
                        FixedEventKey = "evt_locked",
                        FallbackEventKey = "evt_fallback"
                    }
                }
            };

            var eventCatalog = new[]
            {
                new GameEvent
                {
                    EventKey = "evt_locked",
                    Conditions =
                    {
                        new EventCondition
                        {
                            Type = ConditionType.MinAppeal,
                            IntValue = 99
                        }
                    }
                },
                new GameEvent
                {
                    EventKey = "evt_fallback"
                }
            };

            var gameEvent = resolver.ResolveEvent(
                scheduleDay,
                ActionTiming.Afternoon,
                eventCatalog,
                playerData,
                currentDate);

            Assert.That(gameEvent, Is.Not.Null);
            Assert.That(gameEvent.EventKey, Is.EqualTo("evt_fallback"));
        }
    }
}
