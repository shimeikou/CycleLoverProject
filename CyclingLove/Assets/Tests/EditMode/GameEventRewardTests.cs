using NUnit.Framework;
using System.Event;
using System.GameSession;

namespace Tests.EditMode
{
    public class GameEventRewardTests
    {
        [Test]
        public void ApplyRewards_AddsFlagsAndStatsWithoutDuplicates()
        {
            var playerData = new GamePlayerData();
            var gameEvent = new GameEvent
            {
                EventKey = "evt_2026_04_03_afternoon_001",
                TargetHeroineId = "hero_bikeshop",
                Rewards =
                {
                    new EventReward { Type = RewardType.Physical, IntValue = 1 },
                    new EventReward { Type = RewardType.Toughness, IntValue = 2 },
                    new EventReward { Type = RewardType.Empathy, IntValue = 3 },
                    new EventReward { Type = RewardType.AddRouteFlag, StringValue = "route_bikeshop_intro_started" },
                    new EventReward { Type = RewardType.MarkHeroineMet },
                    new EventReward { Type = RewardType.ClearEvent }
                }
            };

            gameEvent.ApplyRewards(playerData);
            gameEvent.ApplyRewards(playerData);

            Assert.That(playerData.Physical, Is.EqualTo(7));
            Assert.That(playerData.Toughness, Is.EqualTo(9));
            Assert.That(playerData.Empathy, Is.EqualTo(11));
            Assert.That(playerData.ActiveRouteFlags.Count, Is.EqualTo(1));
            Assert.That(playerData.MetHeroineIds.Count, Is.EqualTo(1));
            Assert.That(playerData.ClearedEventKeys.Count, Is.EqualTo(1));
            Assert.That(playerData.ActiveRouteFlags, Does.Contain("route_bikeshop_intro_started"));
            Assert.That(playerData.MetHeroineIds, Does.Contain("hero_bikeshop"));
            Assert.That(playerData.ClearedEventKeys, Does.Contain("evt_2026_04_03_afternoon_001"));
        }
    }
}
