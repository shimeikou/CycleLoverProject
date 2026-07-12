using System.Collections.Generic;
using System.Flow;
using System.GameSession;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Scene.MainGrand
{
    public sealed class MainSceneViewer : MonoBehaviour
    {
        private UnityAction<string> _onSelectAction;

        [Header("Base HUD")]
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private TextMeshProUGUI timingText;
        [SerializeField] private TextMeshProUGUI playerStatusText;
        [SerializeField] private TextMeshProUGUI debugText;

        [Header("Panels")]
        [SerializeField] private GameObject actionSelectionPanel;
        [SerializeField] private GameObject novelPanel;
        [SerializeField] private GameObject miniGamePanel;
        [SerializeField] private GameObject summaryPanel;

        [Header("Action Selection")]
        [SerializeField] private TextMeshProUGUI actionTitleText;
        [SerializeField] private TextMeshProUGUI actionDescriptionText;
        [SerializeField] private Button actionButtonPrimary;
        [SerializeField] private TextMeshProUGUI actionButtonPrimaryLabel;
        [SerializeField] private Button actionButtonSecondary;
        [SerializeField] private TextMeshProUGUI actionButtonSecondaryLabel;
        [SerializeField] private Button actionButtonTertiary;
        [SerializeField] private TextMeshProUGUI actionButtonTertiaryLabel;

        [Header("Novel")]
        [SerializeField] private TextMeshProUGUI novelTitleText;
        [SerializeField] private TextMeshProUGUI novelBodyText;
        [SerializeField] private Button novelContinueButton;

        [Header("Mini Game")]
        [SerializeField] private TextMeshProUGUI miniGameTitleText;
        [SerializeField] private TextMeshProUGUI miniGameBodyText;
        [SerializeField] private Button miniGameContinueButton;

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI summaryTitleText;
        [SerializeField] private TextMeshProUGUI summaryBodyText;
        [SerializeField] private Button summaryContinueButton;

        public void Init(
            UnityAction<string> onSelectAction,
            UnityAction onNovelContinue,
            UnityAction onMiniGameContinue,
            UnityAction onSummaryContinue)
        {
            _onSelectAction = onSelectAction;
            BindActionButton(actionButtonPrimary, null);
            BindActionButton(actionButtonSecondary, null);
            BindActionButton(actionButtonTertiary, null);

            RebindButton(novelContinueButton, onNovelContinue);
            RebindButton(miniGameContinueButton, onMiniGameContinue);
            RebindButton(summaryContinueButton, onSummaryContinue);

            HideAllPanels();
        }

        public void Render(GameSceneRequest request, GamePlayerData playerData, IReadOnlyList<MainSceneActionOption> actionOptions, string progressState)
        {
            UpdateHud(request, playerData, progressState);
            HideAllPanels();

            if (request == null)
            {
                return;
            }

            switch (request.Phase)
            {
                case GameFlowPhase.ActionSelection:
                    ShowActionSelection(request, actionOptions);
                    break;
                case GameFlowPhase.Novel:
                    ShowNovel(request);
                    break;
                case GameFlowPhase.MiniGameAction:
                    ShowMiniGame(request);
                    break;
                case GameFlowPhase.DailySummary:
                    ShowSummary(request);
                    break;
            }
        }

        public void UpdateDebug(string debugMessage)
        {
            if (debugText != null)
            {
                debugText.text = debugMessage ?? "-";
            }
        }

        private void UpdateHud(GameSceneRequest request, GamePlayerData playerData, string progressState)
        {
            if (phaseText != null)
            {
                phaseText.text = request?.Phase.ToString() ?? "None";
            }

            if (dateText != null)
            {
                dateText.text = GameSession.CurrentDay.ToString();
            }

            if (timingText != null)
            {
                timingText.text = GameSession.CurrentDay.ActionTiming.ToString();
            }

            if (playerStatusText != null && playerData != null)
            {
                playerStatusText.text =
                    $"Money {playerData.Money} / Physical {playerData.Physical} / Toughness {playerData.Toughness}\n" +
                    $"Smart {playerData.Smart} / Empathy {playerData.Empathy} / Appeal {playerData.Appeal}";
            }

            if (debugText != null)
            {
                debugText.text =
                    $"{progressState}\n" +
                    $"Event: {request?.EventKey ?? "-"}\n" +
                    $"Scenario: {request?.ScenarioId ?? "-"}";
            }
        }

        private void ShowActionSelection(GameSceneRequest request, IReadOnlyList<MainSceneActionOption> actionOptions)
        {
            if (actionSelectionPanel != null)
            {
                actionSelectionPanel.SetActive(true);
            }

            if (actionTitleText != null)
            {
                actionTitleText.text = request.DisplayName ?? "Action Selection";
            }

            if (actionDescriptionText != null)
            {
                actionDescriptionText.text = "行動を1つ選んで次のフェイズへ進みます。今は仮選択UIです。";
            }

            ApplyActionOption(actionButtonPrimary, actionButtonPrimaryLabel, actionOptions, 0);
            ApplyActionOption(actionButtonSecondary, actionButtonSecondaryLabel, actionOptions, 1);
            ApplyActionOption(actionButtonTertiary, actionButtonTertiaryLabel, actionOptions, 2);
        }

        private void ShowNovel(GameSceneRequest request)
        {
            if (novelPanel != null)
            {
                novelPanel.SetActive(true);
            }

            if (novelTitleText != null)
            {
                novelTitleText.text = request.DisplayName ?? "Novel";
            }

            if (novelBodyText != null)
            {
                novelBodyText.text =
                    $"ScenarioId: {request.ScenarioId ?? "-"}\n\n" +
                    "ここにノベル再生UIを接続します。今は Continue ボタンで読み終わり扱いです。";
            }
        }

        private void ShowMiniGame(GameSceneRequest request)
        {
            if (miniGamePanel != null)
            {
                miniGamePanel.SetActive(true);
            }

            if (miniGameTitleText != null)
            {
                miniGameTitleText.text = request.DisplayName ?? "Mini Game";
            }

            if (miniGameBodyText != null)
            {
                miniGameBodyText.text =
                    $"ActionId: {request.ActionId ?? "-"}\n\n" +
                    "ここにミニゲームやアクション実行UIを接続します。今は Continue ボタンで完了扱いです。";
            }
        }

        private void ShowSummary(GameSceneRequest request)
        {
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(true);
            }

            if (summaryTitleText != null)
            {
                summaryTitleText.text = request.DisplayName ?? "Summary";
            }

            if (summaryBodyText != null)
            {
                summaryBodyText.text = "ここに日次まとめや結果表示を接続します。今は Continue ボタンで次へ進みます。";
            }
        }

        private void ApplyActionOption(Button button, TextMeshProUGUI label, IReadOnlyList<MainSceneActionOption> actionOptions, int index)
        {
            if (button == null)
            {
                return;
            }

            var hasOption = actionOptions != null && index < actionOptions.Count;
            button.gameObject.SetActive(hasOption);

            if (!hasOption)
            {
                return;
            }

            var option = actionOptions[index];
            if (label != null)
            {
                label.text = option.Label;
            }

            BindActionButton(button, option.ActionId);
        }

        private void HideAllPanels()
        {
            if (actionSelectionPanel != null) actionSelectionPanel.SetActive(false);
            if (novelPanel != null) novelPanel.SetActive(false);
            if (miniGamePanel != null) miniGamePanel.SetActive(false);
            if (summaryPanel != null) summaryPanel.SetActive(false);
        }

        private void RebindButton(Button button, UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (callback != null)
            {
                button.onClick.AddListener(callback);
            }
        }

        private void BindActionButton(Button button, string actionId)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (_onSelectAction != null && !string.IsNullOrWhiteSpace(actionId))
            {
                button.onClick.AddListener(() => _onSelectAction.Invoke(actionId));
            }
        }
    }
}
