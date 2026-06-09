namespace System.Event
{
    [Serializable]
    public class GameEvent
    {
        public string EventKey;
        
        public GameEventType Type = GameEventType.None;
        
        public string Parameter;
        
        public GameEventState State = GameEventState.None;
        
        public GameEventType Execute()
        {

            return GameEventType.None;
        }

        public void Start()
        {
         
        }
        public bool Running()
        {

            return false;   
        }
        public void Finish()
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
    
    public enum GameEventState
    {
        None,
        Start,
        Running,
        Finish,
    }
}