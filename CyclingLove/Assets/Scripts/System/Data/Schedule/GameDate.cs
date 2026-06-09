namespace System.Data.Schedule
{
    public class GameDate
    {
        public int Year;
        public int Month;
        public int Day;
        public ActionTiming ActionTiming;

        public void IncrementDay()
        {
            Day++;

            // 今の月の最終日を取得
            var lastDay = DateTime.DaysInMonth(Year, Month);

            if (Day <= lastDay) return;
            Day = 1;
            Month++;

            if (Month <= 12) return;
            Month = 1;
            Year++;
        }
        public bool ToNextActionTiming()
        {
            if (ActionTiming == ActionTiming.Extra)
            {
                ActionTiming = ActionTiming.Morning;
                return false;
            }
            ActionTiming++;
            return true;
        }

        public override string ToString()
        {
            return $"{Year}-{Month}-{Day} {ActionTiming}";
        }
    }
}