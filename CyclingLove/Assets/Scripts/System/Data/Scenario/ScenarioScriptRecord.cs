namespace System.Data.Scenario
{
    [System.Serializable]
    public class ScenarioScriptRecord
    {
        public ScenarioCommandType CommandType;

        public string Speaker;
        public string Text;
        
        public string TargetId;
        
        public string AssetId;
        
        public float Value;
        
        public string Param1;
        public string Param2;
        public string Param3;
    }
    
    
}