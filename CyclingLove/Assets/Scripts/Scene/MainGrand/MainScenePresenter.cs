using System;
using System.Flow;
using System.GameSession;
using System.Manager;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene.MainGrand
{
    public sealed class MainScenePresenter : MonoBehaviour
    {
        [SerializeField] private MainSceneViewer view;
        private bool _busy;
        private int _shownRevision;

        private async void Start()
        {
            try
            {
                await GameFlowManager.Instance.ResumeCurrentFlow();
                view.Init(id => Advance(GameFlowCompletionType.ActionSelected, id).Forget(),
                    () => Advance(GameFlowCompletionType.NovelClosed).Forget(),
                    () => { }, () => Advance(GameFlowCompletionType.SummaryClosed).Forget());
                view.BindSessionControls(() => Save().Forget(), () => ReturnToTitle().Forget());
                StandardUIManager.Instance?.HideLoading();
                Render();
            }
            catch (Exception ex) { Debug.LogException(ex); view?.SetMessage("画面を開始できませんでした。"); }
        }

        private void Render()
        {
            var request = GameFlowManager.Instance.PeekCurrentRequest();
            _shownRevision = request.Revision;
            view.Render(request, GameSession.PlayerData, new MainSceneModel().GetActionOptions(), "");
        }

        private async UniTask Advance(GameFlowCompletionType completion, string action = null)
        {
            if (_busy) return;
            if (GameFlowManager.Instance.CurrentPhase == GameFlowPhase.Finished)
            {
                await ReturnToTitle(); return;
            }
            _busy = true;
            view.SetBusy(true);
            try
            {
                GameFlowManager.Instance.SubmitSceneResult(new GameSceneResult
                {
                    CompletionType = completion, SelectedActionId = action, ExpectedRevision = _shownRevision
                });
                Render();
                // One click cannot cascade through newly displayed panels in the same frame.
                await UniTask.Delay(150, ignoreTimeScale: true, cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException) { }
            finally { _busy = false; if (view != null) view.SetBusy(false); }
        }

        private async UniTask Save()
        {
            if (_busy) return;
            _busy = true; view.SetBusy(true);
            try { await GameSession.Save(); view.SetMessage("保存しました。続きから再開できます。"); }
            catch (Exception ex) { Debug.LogWarning($"Save failed: {ex.GetType().Name}"); view.SetMessage("保存できませんでした。空き容量などを確認して再度お試しください。"); }
            finally { _busy = false; if (view != null) view.SetBusy(false); }
        }

        private async UniTask ReturnToTitle()
        {
            if (_busy) return;
            _busy = true; view.SetBusy(true);
            await SceneManager.LoadSceneAsync("InitScene");
        }
    }
}
