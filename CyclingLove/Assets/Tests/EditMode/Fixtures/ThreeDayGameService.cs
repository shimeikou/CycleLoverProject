using System;
using CyclingLove.Flow;
using CyclingLove.Services;
namespace CyclingLove.Tests
{
    // Compatibility facade for the original prototype API; runtime uses CampaignGameService.
    public sealed class ThreeDayGameService : CampaignGameService
    {
        public const string Training = "act_training_roadwork_001";
        public const string Town = "act_town_walk_001";
        public const string Rest = "act_rest_home_001";
        public static readonly string[] Actions = { Training, Town, Rest };
        public ThreeDayGameService() : base(CampaignContentLoader.LoadPrototype()) { }
    }
}
