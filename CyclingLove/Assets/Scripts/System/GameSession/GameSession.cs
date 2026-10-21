using System;
using CyclingLove.Progression;
using CyclingLove.Services;
using CyclingLove.State;
using Cysharp.Threading.Tasks;
using Util;

namespace CyclingLove.Session
{
    public static class GameSession
    {
        
        // ========= 現在のセッション状態 =========
        public static string CurrentSaveSlot = "annual_base";
        private static bool IsInitialized = false;
        

        // ========= データ構造 =========
        public static GameSystemConfig SystemConfig;
        public static GamePlayerData PlayerData;
        
        public static async UniTask InitConfig()
        {
            if (IsInitialized)
                return;

            GameLogger.LogInfo("[GameSession] Initialize session...");

            // SystemDataは常にロード（存在しなければ新規作成）
            SystemConfig = await SaveDataService.LoadSystem();
            SystemConfig ??= new GameSystemConfig();
            SystemConfig.Profile ??= new ProfileProgress();
            SystemConfig.Audio ??= new AudioSettingsData();
            ProfileProgressService.Normalize(SystemConfig.Profile);
            SystemConfig.Audio.Normalize();
            
            IsInitialized = true;
        }
        
        public static UniTask Save()
        {
            return SaveDataService.Save(PlayerData, CurrentSaveSlot);
        }
    }
}
