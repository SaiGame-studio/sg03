using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using SaiGame.Services;

namespace SG03.UI
{
    [Serializable]
    public class BattleEndScriptResponse
    {
        public BattleEndOutput output;
    }

    [Serializable]
    public class BattleEndOutput
    {
        public string error;
        public string session_id;
        public string status;
        public string winner;
        public int turn;
        public int alpha_hp;
        public int omega_hp;
        public BattleEndDropItem[] drops;
    }

    [Serializable]
    public class BattleEndDropItem
    {
        public string definition_id;
        public string name;
        public int quantity;
    }

    public class GameBattleResultPopupUI
    {
        private readonly BattleState battleState;
        private readonly bool isWin;
        private readonly int turn;
        private readonly int playerHp;
        private readonly int enemyHp;
        private readonly BattleEndDropItem[] drops;
        private readonly string lobbySceneName;

        private VisualElement overlay;
        private Label titleLabel;
        private Label messageLabel;
        private Label errorLabel;
        private Button btnContinue;
        private Button btnStop;

        public GameBattleResultPopupUI(
            BattleState battleState,
            bool isWin,
            int turn,
            int playerHp,
            int enemyHp,
            BattleEndDropItem[] drops,
            string lobbySceneName)
        {
            this.battleState = battleState;
            this.isWin = isWin;
            this.turn = turn;
            this.playerHp = playerHp;
            this.enemyHp = enemyHp;
            this.drops = drops;
            this.lobbySceneName = lobbySceneName;
        }

        public void Show(VisualElement parent)
        {
            if (parent == null) return;

            // 1. Backdrop overlay
            this.overlay = new VisualElement { name = "BattleResultPopupOverlay" };
            this.overlay.AddToClassList("battle-result-popup-overlay");

            // 2. Central Card Panel
            VisualElement card = new VisualElement { name = "BattleResultPopupCard" };
            card.AddToClassList("battle-result-popup-card");
            this.overlay.Add(card);

            // 3. Title (Victory / Defeat)
            this.titleLabel = new Label { name = "BattleResultPopupTitle" };
            this.titleLabel.AddToClassList("battle-result-popup-title");
            if (this.isWin)
            {
                this.titleLabel.text = "Victory";
                this.titleLabel.AddToClassList("battle-result-popup-title--win");
            }
            else
            {
                this.titleLabel.text = "Defeat";
                this.titleLabel.AddToClassList("battle-result-popup-title--loss");
            }
            card.Add(this.titleLabel);

            // 4. Message description
            this.messageLabel = new Label { name = "BattleResultPopupMessage" };
            this.messageLabel.AddToClassList("battle-result-popup-message");
            this.messageLabel.text = $"Battle ended on turn {this.turn}.\nYour HP: {this.playerHp} | Enemy HP: {this.enemyHp}";
            card.Add(this.messageLabel);

            // 5. Rewards List
            if (this.drops != null && this.drops.Length > 0)
            {
                VisualElement rewardsContainer = new VisualElement { name = "BattleResultPopupRewards" };
                rewardsContainer.style.marginTop = 10;
                rewardsContainer.style.marginBottom = 20;
                rewardsContainer.style.alignItems = Align.Center;

                Label rewardsTitle = new Label { name = "BattleResultPopupRewardsTitle" };
                rewardsTitle.text = "REWARDS OBTAINED";
                rewardsTitle.style.color = new Color(0.6f, 0.7f, 0.9f);
                rewardsTitle.style.fontSize = 12;
                rewardsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                rewardsTitle.style.letterSpacing = 1.5f;
                rewardsTitle.style.marginBottom = 8;
                rewardsContainer.Add(rewardsTitle);

                foreach (var drop in this.drops)
                {
                    Label dropLabel = new Label { name = $"BattleResultPopupDrop_{drop.definition_id}" };
                    string displayName = !string.IsNullOrEmpty(drop.name) ? drop.name : drop.definition_id;
                    dropLabel.text = $"{displayName} x{drop.quantity}";
                    dropLabel.style.color = new Color(0.8f, 0.9f, 1f);
                    dropLabel.style.fontSize = 14;
                    dropLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    dropLabel.style.marginBottom = 4;
                    rewardsContainer.Add(dropLabel);
                }

                card.Add(rewardsContainer);
            }

            // 6. Error Label (hidden by default)
            this.errorLabel = new Label { name = "BattleResultPopupErrorLabel" };
            this.errorLabel.style.color = new Color(1f, 0.3f, 0.3f);
            this.errorLabel.style.fontSize = 12;
            this.errorLabel.style.marginBottom = 16;
            this.errorLabel.style.display = DisplayStyle.None;
            this.errorLabel.style.whiteSpace = WhiteSpace.Normal;
            card.Add(this.errorLabel);

            // 7. Buttons Row
            VisualElement btnRow = new VisualElement { name = "BattleResultPopupBtnRow" };
            btnRow.AddToClassList("battle-result-popup-btn-row");

            this.btnContinue = new Button { name = "BtnContinuePlay" };
            this.btnContinue.text = "Continue Play";
            this.btnContinue.AddToClassList("battle-result-popup-btn");
            this.btnContinue.AddToClassList("battle-result-popup-btn--continue");
            this.btnContinue.RegisterCallback<ClickEvent>(_ => this.OnContinuePlayClicked());
            btnRow.Add(this.btnContinue);

            this.btnStop = new Button { name = "BtnStopPlay" };
            this.btnStop.text = "Stop Play";
            this.btnStop.AddToClassList("battle-result-popup-btn");
            this.btnStop.AddToClassList("battle-result-popup-btn--stop");
            this.btnStop.RegisterCallback<ClickEvent>(_ => this.OnStopPlayClicked());
            btnRow.Add(this.btnStop);

            card.Add(btnRow);

            parent.Add(this.overlay);
        }

        private void OnContinuePlayClicked()
        {
            this.SetButtonsEnabled(false);
            this.btnContinue.text = "Loading...";
            this.HandleImmediateContinue();
        }

        private void OnStopPlayClicked()
        {
            this.SetButtonsEnabled(false);
            this.btnStop.text = "Loading...";
            this.HandleImmediateStop();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            this.btnContinue.SetEnabled(enabled);
            this.btnStop.SetEnabled(enabled);
        }

        private void HandleImmediateContinue()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && !string.IsNullOrWhiteSpace(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }

        private void HandleImmediateStop()
        {
            if (!string.IsNullOrWhiteSpace(this.lobbySceneName))
            {
                SceneManager.LoadScene(this.lobbySceneName);
            }
        }
    }
}
