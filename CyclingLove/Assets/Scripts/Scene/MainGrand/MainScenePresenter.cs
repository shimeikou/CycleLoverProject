
using System;
using System.Event;
using System.GameSession;
using System.Manager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

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
                    await _model.Initialize();

                    var data = GameScheduleManager.Instance.GetData();
                    Debug.LogWarning(data);
                    
                    GameLogger.LogInfo("Initialize finished currentDate => " + GameSession.CurrentDay);
                }
            }
        }

        private void Update()
        {
            return;
            if(_model?.GetCurrentEvent() == null ) return;
            var currentEvent = _model?.GetCurrentEvent();
            if (currentEvent == null) return;
            switch (currentEvent.State)
            {
                case GameEventState.None:
                    currentEvent.State = GameEventState.Start;
                    break;
                case GameEventState.Start:
                    currentEvent.Start();
                    currentEvent.State = GameEventState.Running;
                    break;
                case GameEventState.Running:
                    var res = currentEvent.Running();
                    if (res)
                    {
                        currentEvent.State = GameEventState.Finish;
                    }
                    break;
                case GameEventState.Finish:
                    currentEvent.Finish();
                    var isTodayEnd = GameSession.CurrentDay.ToNextActionTiming();
                    if (isTodayEnd)
                    {
                        GameSession.CurrentDay.IncrementDay();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}