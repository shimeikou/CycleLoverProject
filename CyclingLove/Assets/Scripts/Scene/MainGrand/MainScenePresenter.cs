
using System;
using System.Flow;
using System.GameSession;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;

namespace Scene.MainGrand
{
    public class MainScenePresenter : MonoBehaviour
    {
        private MainSceneModel _model = null;
        [SerializeField] private MainSceneViewer view;

        private MainSceneStepResult _lastStepResult;
        private string _progressState = "Not Started";
        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;

        private void Start()
        {
            InitializeAndRun().Forget();
        }

        private async UniTaskVoid InitializeAndRun()
        {
            if (_model != null)
            {
                return;
            }

            _model = new MainSceneModel();
            await _model.Initialize();
            view?.Init(OnSelectAction, OnNovelContinue, OnMiniGameContinue, OnSummaryContinue);

            var initialRequest = _model.GetCurrentRequest();
            if (!string.IsNullOrWhiteSpace(initialRequest?.UnitySceneName) &&
                initialRequest.UnitySceneName != SceneManager.GetActiveScene().name)
            {
                await SceneManager.LoadSceneAsync(initialRequest.UnitySceneName);
                return;
            }

            GameLogger.LogInfo("MainScene initialized currentDate => " + GameSession.CurrentDay, this);
            Debug.Log($"[MainScene] Start progression from {GameSession.CurrentDay}");
            _progressState = $"Ready at {GameSession.CurrentDay}";
            PresentCurrentRequest();
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(12, 12, 520, 260), _boxStyle);
            GUILayout.Label("MainScene Debug", _labelStyle);
            GUILayout.Label($"State: {_progressState}");
            GUILayout.Label($"Phase: {_model?.GetCurrentPhase().ToString() ?? "-"}");
            GUILayout.Label($"Current: {GameSession.CurrentDay}");

            if (GameSession.PlayerData != null)
            {
                GUILayout.Space(6);
                GUILayout.Label("Player");
                GUILayout.Label(
                    $"Money {GameSession.PlayerData.Money} | " +
                    $"Physical {GameSession.PlayerData.Physical} | " +
                    $"Toughness {GameSession.PlayerData.Toughness} | " +
                    $"Smart {GameSession.PlayerData.Smart} | " +
                    $"Empathy {GameSession.PlayerData.Empathy} | " +
                    $"Appeal {GameSession.PlayerData.Appeal}");
                GUILayout.Label($"Cleared: {JoinOrDash(GameSession.PlayerData.ClearedEventKeys)}");
                GUILayout.Label($"RouteFlags: {JoinOrDash(GameSession.PlayerData.ActiveRouteFlags)}");
                GUILayout.Label($"MetHeroines: {JoinOrDash(GameSession.PlayerData.MetHeroineIds)}");
            }

            if (_lastStepResult != null)
            {
                GUILayout.Space(6);
                GUILayout.Label("Last Step");
                GUILayout.Label($"Phase: {_lastStepResult.Phase} -> {_lastStepResult.NextPhase}");
                GUILayout.Label(
                    $"{_lastStepResult.BeforeDateText} [{_lastStepResult.Timing}] -> " +
                    $"{_lastStepResult.EventKey ?? "no event"}");
                GUILayout.Label($"Display: {_lastStepResult.DisplayName ?? "-"}");
                GUILayout.Label($"Completion: {_lastStepResult.CompletionType}");
                GUILayout.Label($"Scene: {_lastStepResult.RequestedSceneName} -> {_lastStepResult.NextSceneName}");
                GUILayout.Label($"Rewards: {_lastStepResult.RewardDebugText ?? "no rewards"}");
                GUILayout.Label($"After: {_lastStepResult.AfterDateText}");
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(12, 12, 12, 12),
                    alignment = TextAnchor.UpperLeft
                };
            }
        }

        private static string JoinOrDash(System.Collections.Generic.IEnumerable<string> values)
        {
            if (values == null)
            {
                return "-";
            }

            var text = string.Join(", ", values);
            return string.IsNullOrWhiteSpace(text) ? "-" : text;
        }

        private void OnSelectAction(string actionId)
        {
            AdvanceFlow(new GameSceneResult
            {
                CompletionType = GameFlowCompletionType.ActionSelected,
                SelectedActionId = actionId,
                Notes = $"selected action: {actionId}"
            }).Forget();
        }

        private void OnNovelContinue()
        {
            AdvanceFlow(new GameSceneResult
            {
                CompletionType = GameFlowCompletionType.NovelClosed,
                Notes = "novel continue"
            }).Forget();
        }

        private void OnMiniGameContinue()
        {
            AdvanceFlow(new GameSceneResult
            {
                CompletionType = GameFlowCompletionType.MiniGameFinished,
                MiniGameScore = 0,
                Notes = "minigame continue"
            }).Forget();
        }

        private void OnSummaryContinue()
        {
            AdvanceFlow(new GameSceneResult
            {
                CompletionType = GameFlowCompletionType.SummaryClosed,
                Notes = "summary continue"
            }).Forget();
        }

        private async UniTaskVoid AdvanceFlow(GameSceneResult result)
        {
            if (_model == null)
            {
                return;
            }

            var stepResult = _model.CompleteCurrentPhase(result);
            _lastStepResult = stepResult;
            LogStep(stepResult);

            if (ShouldChangeScene(stepResult))
            {
                _progressState = $"Scene change to {stepResult.NextSceneName}";
                await SceneManager.LoadSceneAsync(stepResult.NextSceneName);
                return;
            }

            if (!_model.IsCurrentScenePlayable())
            {
                _progressState = $"Finished at {GameSession.CurrentDay}";
                view?.UpdateDebug("Flow finished.");
                return;
            }

            PresentCurrentRequest();
        }

        private void PresentCurrentRequest()
        {
            var request = _model?.GetCurrentRequest();
            _progressState = request == null
                ? "No request"
                : $"Waiting {request.Phase}";

            view?.Render(request, GameSession.PlayerData, _model?.GetActionOptions(), _progressState);
        }

        private void LogStep(MainSceneStepResult result)
        {
            if (result == null)
            {
                return;
            }

            if (result.WasEventResolved)
            {
                Debug.Log(
                    $"[MainScene] {result.Phase} -> {result.NextPhase} | {result.BeforeDateText} " +
                    $"[{result.Timing}] -> {result.EventKey} ({result.DisplayName}) => {result.AfterDateText}");
                Debug.Log($"[MainScene] Rewards {result.EventKey}: {result.RewardDebugText}");
            }
            else
            {
                Debug.Log(
                    $"[MainScene] {result.Phase} -> {result.NextPhase} | {result.BeforeDateText} " +
                    $"[{result.Timing}] -> {result.DisplayName ?? "no event"} => {result.AfterDateText}");
            }
        }

        private static bool ShouldChangeScene(MainSceneStepResult result)
        {
            return !string.IsNullOrWhiteSpace(result?.NextSceneName) &&
                   result.NextSceneName != SceneManager.GetActiveScene().name;
        }
    }
}
