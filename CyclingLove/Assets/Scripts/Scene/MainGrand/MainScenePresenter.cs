
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene.MainGrand
{
    public class MainScenePresenter : MonoBehaviour
    {
        private MainSceneModel _model = null;
        private void Start()
        {
            Initialize().Forget();
            return;

            async UniTaskVoid Initialize()
            {
                if (_model == null)
                {
                    _model = new MainSceneModel();
                    await MainSceneModel.Initialize();
                }

                var currentEvent = MainSceneModel.GetCurrentEvent();
                currentEvent?.Execute();
            }
        }
    }
}