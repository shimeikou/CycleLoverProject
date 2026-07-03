using System.Collections.Generic;
using System.Data.Schedule;
using System.Event;
using System.GameSession;
using System.Linq;

namespace System.Service
{
    public sealed class GameEventResolverService
    {
        public GameEvent ResolveEvent(
            ScheduleDay scheduleDay,
            ActionTiming timing,
            IReadOnlyCollection<GameEvent> eventCatalog,
            GamePlayerData playerData,
            GameDate currentDate)
        {
            if (scheduleDay == null || eventCatalog == null)
            {
                return null;
            }

            var slot = scheduleDay.GetSlot(timing);
            if (slot == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(slot.FixedEventKey))
            {
                return FindByKey(slot.FixedEventKey, eventCatalog, playerData, currentDate) ??
                       FindByKey(slot.FallbackEventKey, eventCatalog, playerData, currentDate);
            }

            var candidate = slot.CandidateEventKeys?
                .Select(eventKey => FindByKey(eventKey, eventCatalog, playerData, currentDate))
                .FirstOrDefault(gameEvent => gameEvent != null);

            return candidate ?? FindByKey(slot.FallbackEventKey, eventCatalog, playerData, currentDate);
        }

        private static GameEvent FindByKey(
            string eventKey,
            IEnumerable<GameEvent> eventCatalog,
            GamePlayerData playerData,
            GameDate currentDate)
        {
            if (string.IsNullOrWhiteSpace(eventKey))
            {
                return null;
            }

            var gameEvent = eventCatalog.FirstOrDefault(x => x.EventKey == eventKey);
            if (gameEvent == null)
            {
                return null;
            }

            return gameEvent.CanExecute(playerData, currentDate) ? gameEvent : null;
        }
    }
}
