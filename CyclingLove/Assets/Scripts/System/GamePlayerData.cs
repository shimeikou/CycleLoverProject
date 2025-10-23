namespace System
{
    [Serializable]
    public class GamePlayerData
    {
        // ========= 基本プロフィール =========
        public string PlayerName = "主人公";
        public int Age = 28;

        // ========= ゲーム進行 =========
        public int Day = 1;
        public int Money = 1000;
        public string CurrentScene = "MainScene";
        public string LastVisitedPlace = "Home";

        // ========= ステータス =========
        public int Physical = 5;   // 体力・フィジカル
        public int Heart = 5;      // 精神力・ハート
        public int Smart = 5;      // 知識・スマート
        public int Charm = 5;      // 魅力・チャーム
        public int Appeal = 5;     // 表現力・アピール
        
    }
}