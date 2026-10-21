using System;
using CyclingLove.State;
namespace CyclingLove.Session
{
    [Serializable]
    public class GameSystemConfig
    {
        public string GameVersion = "1.0.0";
        public DateTime LastPlayed = DateTime.Now;
        public int TotalPlayTimeMinutes = 0;
        public int LaunchCount = 0;
        public ProfileProgress Profile = new();
        public AudioSettingsData Audio = new();
    }

    [Serializable]
    public sealed class AudioSettingsData
    {
        public int Version = 1;
        public float MasterVolume = 1f;
        public float BgmVolume = 1f;
        public float AmbientVolume = 1f;
        public float SeVolume = 1f;
        public float VoiceVolume = 1f;
        public bool Muted;

        public void Normalize()
        {
            Version = 1;
            MasterVolume = Clamp(MasterVolume);
            BgmVolume = Clamp(BgmVolume);
            AmbientVolume = Clamp(AmbientVolume);
            SeVolume = Clamp(SeVolume);
            VoiceVolume = Clamp(VoiceVolume);
        }

        public AudioSettingsData Snapshot() => new()
        {
            Version = Version,
            MasterVolume = MasterVolume,
            BgmVolume = BgmVolume,
            AmbientVolume = AmbientVolume,
            SeVolume = SeVolume,
            VoiceVolume = VoiceVolume,
            Muted = Muted
        };

        public void CopyFrom(AudioSettingsData source)
        {
            if (source == null) return;
            Version = source.Version;
            MasterVolume = source.MasterVolume;
            BgmVolume = source.BgmVolume;
            AmbientVolume = source.AmbientVolume;
            SeVolume = source.SeVolume;
            VoiceVolume = source.VoiceVolume;
            Muted = source.Muted;
            Normalize();
        }

        private static float Clamp(float value) => Math.Clamp(value, 0f, 1f);
    }
}
