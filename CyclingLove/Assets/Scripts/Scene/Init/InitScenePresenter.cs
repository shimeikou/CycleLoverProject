using System;
using System.GameSession;
using System.Manager;
using System.Service;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene.Init
{
    public sealed class InitScenePresenter : MonoBehaviour
    {
        [SerializeField] private InitSceneViewer view;
        private bool _busy;
        private async void Start()
        {
            try
            {
                await new InitSceneModel().Init();
                StandardUIManager.Instance?.HideLoading();
                view.Init(() => Open(false).Forget(), () => Open(true).Forget(),
                    () => view.SetMessage("設定画面は準備中です。"), () => Application.Quit());
                view.SetLoadAvailable(SaveDataService.Exists(GameSession.CurrentSaveSlot));
                view.Show();
            }
            catch (Exception ex) { Debug.LogException(ex); view.SetMessage("タイトルの初期化に失敗しました。"); }
        }
        private async UniTask Open(bool load)
        {
            if (_busy || !view.IsInit || view.IsTransition) return;
            _busy = true;
            view.SetBusy(true);
            try
            {
                if (load) await GameFlowManager.Instance.LoadGameFlow();
                else await GameFlowManager.Instance.StartNewGameFlow();
                StandardUIManager.Instance?.ShowLoading();
                await SceneManager.LoadSceneAsync("MainGameScene");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Start/load failed: {ex.GetType().Name}");
                view.SetMessage(load ? "読み込めませんでした。セーブが破損しているか、この試作に対応していません。新しく始めることはできます。" : "開始できませんでした。もう一度お試しください。");
                StandardUIManager.Instance?.HideLoading();
                _busy = false; view.SetBusy(false);
            }
        }
    }
}
