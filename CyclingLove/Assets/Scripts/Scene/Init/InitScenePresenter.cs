using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace Scene.Init
{
    public class InitScenePresenter : MonoBehaviour
    {
        private InitSceneModel _model;
       
        [SerializeField]
        private InitSceneViewer view;
        public void Start()
        {
            Init().Forget();
        }

        private async UniTask Init()
        {
            _model = new InitSceneModel();
            await _model.Init();
            
            view.Init(OnClickedStart, OnClickedLoad, OnClickedConfig, OnClickedExit);
            view.Show();
        }

        private void OnClickedExit()
        {
            if(!view.IsReady)return;
            GameLogger.LogInfo("[Init] OnClickedExit"); 
            
            Application.Quit();
            
        }

        private void OnClickedConfig()
        {
            if(!view.IsReady)return;
            
            GameLogger.LogInfo("[Init] OnClickedConfig");
        }

        private void OnClickedLoad()
        {
            if(!view.IsReady)return;
            GameLogger.LogInfo("[Init] OnClickedLoad");  
        }

        private void OnClickedStart()
        {
            if(!view.IsReady)return;
            GameLogger.LogInfo("[Init] OnClickedStart");  
        }
        
    }
}
