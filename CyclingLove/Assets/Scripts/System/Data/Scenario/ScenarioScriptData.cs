using System.Collections.Generic;

namespace System.Data.Scenario
{
    [Serializable]
    public class ScenarioScriptData
    {
        public string ScenarioId;
        public List<ScenarioScriptRecord> Records;
    }
    
    public enum ScenarioCommandType
    {
        None = 0,
        Message = 1,
        Effect = 2,
        Sound = 3,
    }
}