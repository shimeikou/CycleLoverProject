using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace System
{
    public static class GameSession
    {
        
        // ========= 現在のセッション状態 =========
        public static bool IsNewGame = true;
        public static string CurrentSaveSlot = "1";
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
        
        public static async UniTask SetUpPlayerData(int loadDataSlot = 1)
        {
            if (IsNewGame)
            {
                PlayerData = new GamePlayerData();
                GameLogger.LogInfo("[GameSession] New game started.");
            }
            else
            {
                PlayerData = await SaveDataService.Load(CurrentSaveSlot);
                GameLogger.LogInfo("[GameSession] Loaded save slot " + CurrentSaveSlot);
            }
        }
    }
}