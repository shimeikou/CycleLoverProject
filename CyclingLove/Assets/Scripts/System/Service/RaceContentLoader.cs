using CyclingLove.Racing;

namespace CyclingLove.Services
{
    public static class RaceContentLoader
    {
        public const string RuntimeResourceKey = "Content/Races";

        public static RaceCatalog LoadRuntime()
        {
            var catalog = JsonContentReader.Load<RaceCatalog>(RuntimeResourceKey);
            catalog.Validate();
            return catalog;
        }
    }
}
