using CyclingLove.Audio;

namespace CyclingLove.Services
{
    public static class AudioContentLoader
    {
        public const string RuntimeResourceKey = "Content/AudioCatalog";

        public static AudioContentCatalog LoadRuntime()
        {
            var catalog = JsonContentReader.Load<AudioContentCatalog>(RuntimeResourceKey);
            catalog.Validate();
            return catalog;
        }
    }
}
