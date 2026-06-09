using UnityEngine;

namespace System.AdvScriptEngine
{
    public class AdcScriptEngineController
    {
        private AdvScriptEngineModel _model;
        
        [SerializeField]
        private AdvScriptEngineViewer _viewer;


        public void Initialize(int advId)
        {
            _model.Init(advId);
        }
    }
}