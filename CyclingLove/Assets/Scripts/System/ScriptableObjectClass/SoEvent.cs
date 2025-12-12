using System.Collections.Generic;
using System.Event;
using UnityEngine;

namespace System.ScriptableObjectClass
{
    [CreateAssetMenu(fileName = "SO_Event", menuName = "ScriptableObjects/Event", order = 0)]
    public class SoEvent : ScriptableObject
    {
        public  List<GameEvent> Events;
    }
}