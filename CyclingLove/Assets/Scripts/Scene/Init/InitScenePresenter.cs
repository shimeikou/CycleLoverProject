using System;
using System.GameSession;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            if(!view.IsInit)return;
            GameLogger.LogInfo("[Init] OnClickedExit"); 
            
            Application.Quit();
            
        }

        private void OnClickedConfig()
        {
            if(!view.IsInit)return;
            
            GameLogger.LogInfo("[Init] OnClickedConfig");
        }

        private void OnClickedLoad()
        {
            if(!view.IsInit)return;
            GameLogger.LogInfo("[Init] OnClickedLoad");

            OpenSaveSlotSelectPanel();
        }

        private void OpenSaveSlotSelectPanel()
        {
            
        }

        private void OnClickedStart()
        {
            if(!view.IsInit)return;
            GameLogger.LogInfo("[Init] OnClickedStart");

            GameSession.IsNewGame = true;
            view.HideAndShowLoading(Color.black, ()=>
            {
                GoToNewGame().Forget();
            });
           
        }

        private async UniTask GoToNewGame()
        {
            StandardUIManager.Instance.ShowLoading();
            await SceneManager.LoadSceneAsync("MainGameScene");
        }
    }
}
