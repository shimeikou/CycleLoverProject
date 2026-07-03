
using System;
using System.Event;
using System.GameSession;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace Scene.MainGrand
{
    public class MainScenePresenter : MonoBehaviour
    {
        private MainSceneModel _model = null;
        [SerializeField] private int stepIntervalMs = 120;

        private MainSceneStepResult _lastStepResult;
        private int _stepCount;
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

            GameLogger.LogInfo("MainScene initialized currentDate => " + GameSession.CurrentDay, this);
            Debug.Log($"[MainScene] Start progression from {GameSession.CurrentDay}");
            _progressState = $"Running from {GameSession.CurrentDay}";

            const int maxSteps = 32;
            _stepCount = 0;

            while (_model.HasScheduleForCurrentDay() && _stepCount < maxSteps)
            {
                _stepCount++;
                var result = _model.ProcessCurrentTiming();
                _lastStepResult = result;

                if (result.WasEventResolved)
                {
                    Debug.Log(
                        $"[MainScene] Step {_stepCount}: {result.BeforeDateText} " +
                        $"[{result.Timing}] -> {result.EventKey} ({result.DisplayName}) => {result.AfterDateText}");

                    Debug.Log(
                        $"[MainScene] Rewards {result.EventKey}: {result.RewardDebugText}");
                }
                else
                {
                    Debug.Log(
                        $"[MainScene] Step {_stepCount}: {result.BeforeDateText} " +
                        $"[{result.Timing}] -> no event => {result.AfterDateText}");
                }

                _progressState = $"Running step {_stepCount}";
                await UniTask.Delay(stepIntervalMs);
            }

            if (_stepCount >= maxSteps)
            {
                Debug.LogWarning("[MainScene] Progression stopped due to max step safety guard.");
                _progressState = "Stopped by safety guard";
            }
            else
            {
                Debug.Log($"[MainScene] Progression finished at {GameSession.CurrentDay}");
                _progressState = $"Finished at {GameSession.CurrentDay}";
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(new Rect(12, 12, 520, 260), _boxStyle);
            GUILayout.Label("MainScene Debug", _labelStyle);
            GUILayout.Label($"State: {_progressState}");
            GUILayout.Label($"Current: {GameSession.CurrentDay}");
            GUILayout.Label($"Steps: {_stepCount}");

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
                GUILayout.Label(
                    $"{_lastStepResult.BeforeDateText} [{_lastStepResult.Timing}] -> " +
                    $"{_lastStepResult.EventKey ?? "no event"}");
                GUILayout.Label($"Display: {_lastStepResult.DisplayName ?? "-"}");
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
    }
}
