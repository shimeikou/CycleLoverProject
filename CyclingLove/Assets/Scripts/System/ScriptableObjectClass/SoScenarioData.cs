using System.Data.Scenario;
using UnityEngine;

namespace System.ScriptableObjectClass
{
    [CreateAssetMenu(fileName = "SO_ScenarioData", menuName = "ScriptableObjects/ScenarioData", order = 0)]
    public class SoScenarioData : ScriptableObject
    {
        public ScenarioScriptData ScenarioData;
    }
}