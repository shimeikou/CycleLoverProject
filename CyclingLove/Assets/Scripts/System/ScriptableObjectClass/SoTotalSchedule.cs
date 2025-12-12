using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace System.ScriptableObjectClass
{
    [CreateAssetMenu(fileName = "SO_TotalSchedule", menuName = "ScriptableObjects/TotalSchedule", order = 0)]
    public class SoTotalSchedule : ScriptableObject
    {
        public List<KeyReferencePair> MonthScheduleDataPairs;
    }
    
    [Serializable]
    public class KeyReferencePair
    {
        public string Key;
        public AssetReference Value;
    }
}