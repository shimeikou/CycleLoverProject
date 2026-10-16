using System.Data.Schedule;
using System.Service;
using Cysharp.Threading.Tasks;
using Util;

namespace System.GameSession
{
    public static class GameSession
    {
        
        // ========= 現在のセッション状態 =========
        public static string CurrentSaveSlot = "bikeshop_short";
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
            
            IsInitialized = true;
        }
        
        public static UniTask Save()
        {
            return SaveDataService.Save(PlayerData, CurrentSaveSlot);
        }
    }
}
