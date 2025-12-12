namespace System.Event
{
    [Serializable]
    public class GameEvent
    {
        public string EventKey;
        
        public GameEventType Type;
        
        public string Parameter;
        
        
        public void Execute()
        {
            
        }
    }
    
    public enum GameEventType
    {
        None,
        Scenario,
        FreeTime,
        Event,
    }
}