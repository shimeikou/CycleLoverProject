using System.Data.Schedule;
using System.Service;
using Cysharp.Threading.Tasks;
using Util;

namespace System.GameSession
{
    public static class GameSession
    {
        
        // ========= 現在のセッション状態 =========
        public static bool IsNewGame = true;
        public static string CurrentSaveSlot = "1";
        private static bool IsInitialized = false;
        
        public static readonly GameDate CurrentDay = new ();

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
        
        public static async UniTask SetUpPlayerData()
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

            CurrentDay.Year = PlayerData.Year;
            CurrentDay.Month = PlayerData.Month;
            CurrentDay.Day = PlayerData.Day;
            CurrentDay.ActionTiming = (ActionTiming)PlayerData.Timing;
            
        }

        public static UniTask Save()
        {
            return UniTask.CompletedTask;
        }
    }
}