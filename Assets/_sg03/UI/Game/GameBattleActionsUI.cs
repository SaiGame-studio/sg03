using System;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Handles all battle-action buttons: Init Card, Check Status, Start Battle, End Battle,
    // End Turn, Draw Card, and Attack.
    // Receives the selected preset via HandlePresetTabSelected / HandlePresetSlotsLoaded
    // so it can validate and initiate StartBattle.
    public class GameBattleActionsUI
    {
        private const string BattleModeNormal = "normal";

        private readonly Func<BattleScripts> getBattleScripts;
        private readonly Func<BattleStateCtrl> getBattleStateCtrl;

        private PresetData selectedPreset;

        private Button btnEndBattle;
        private Button btnCheckStatus;
        private Button btnInitCard;
        private Button btnEndTurn;
        private Button btnDrawCard;
        private Button btnAttack;
        private Button btnStartBattle;
        private TextField enemyCodeNameInput;

        private bool battleStatusFirstCallDone;

        public GameBattleActionsUI(
            Func<BattleScripts> getBattleScripts,
            Func<BattleStateCtrl> getBattleStateCtrl)
        {
            this.getBattleScripts = getBattleScripts;
            this.getBattleStateCtrl = getBattleStateCtrl;
        }

        public void Bind(VisualElement root)
        {
            this.BindButtons(root);
            this.RegisterCallbacks();
        }

        private void BindButtons(VisualElement root)
        {
            this.btnEndBattle = root.Q<Button>("BtnEndBattle");
            this.btnCheckStatus = root.Q<Button>("BtnCheckStatus");
            this.btnInitCard = root.Q<Button>("BtnInitCard");
            this.btnEndTurn = root.Q<Button>("BtnEndTurn");
            this.btnDrawCard = root.Q<Button>("BtnDrawCard");
            this.btnAttack = root.Q<Button>("BtnAttack");
            this.btnStartBattle = root.Q<Button>("BtnStartBattle");
            this.enemyCodeNameInput = root.Q<TextField>("EnemyCodeNameInput");
        }

        private void RegisterCallbacks()
        {
            this.btnEndBattle?.RegisterCallback<ClickEvent>(_ => this.OnEndBattleClicked());
            this.btnCheckStatus?.RegisterCallback<ClickEvent>(_ => this.OnCheckStatusClicked());
            this.btnInitCard?.RegisterCallback<ClickEvent>(_ => this.OnInitCardClicked());
            this.btnEndTurn?.RegisterCallback<ClickEvent>(_ => this.OnEndTurnClicked());
            this.btnDrawCard?.RegisterCallback<ClickEvent>(_ => this.OnDrawCardClicked());
            this.btnAttack?.RegisterCallback<ClickEvent>(_ => this.OnAttackClicked());
            this.btnStartBattle?.RegisterCallback<ClickEvent>(_ => this.OnStartBattleClicked());
            this.enemyCodeNameInput?.RegisterValueChangedCallback(_ => this.ResetStartBattleButtonText());
        }

        // Called by GamePanelUI when a desk tab is tapped (before full slot load).
        public void HandlePresetTabSelected(PresetData preset)
        {
            this.selectedPreset = preset;
            this.ResetStartBattleButtonText();
        }

        // Called by GamePanelUI when full slot data has been fetched.
        public void HandlePresetSlotsLoaded(PresetData preset)
        {
            this.selectedPreset = preset;
        }

        protected virtual void OnInitCardClicked()
        {
            this.TriggerInitCard();
        }

        protected virtual void OnEndBattleClicked()
        {
            this.TriggerEndBattle();
        }

        protected virtual void OnCheckStatusClicked()
        {
            this.TriggerCheckStatus();
        }

        protected virtual void OnStartBattleClicked()
        {
            this.TriggerStartBattle();
        }

        protected virtual void OnEndTurnClicked() { }

        protected virtual void OnDrawCardClicked() { }

        protected virtual void OnAttackClicked() { }

        private void TriggerInitCard()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (!this.CanInitCard(scripts)) return;
            this.SetInitCardLoading(true);
            scripts.RunInitCards(this.OnInitCardSucceeded, this.OnInitCardFailed);
        }

        private bool CanInitCard(BattleScripts scripts)
        {
            if (scripts != null) return true;
            this.SetInitCardButtonText("No Script");
            return false;
        }

        private void SetInitCardLoading(bool isLoading)
        {
            if (this.btnInitCard == null) return;
            this.btnInitCard.SetEnabled(!isLoading);
            this.btnInitCard.text = isLoading ? "Initing..." : "Init Card";
        }

        private void SetInitCardButtonText(string text)
        {
            if (this.btnInitCard == null) return;
            this.btnInitCard.text = text;
        }

        private void OnInitCardSucceeded(string response)
        {
            this.SetInitCardLoading(false);
            this.SetInitCardButtonText("Init OK");
            this.ApplyBattleStatusResponse(response);
        }

        private void OnInitCardFailed(string error)
        {
            this.SetInitCardLoading(false);
            this.SetInitCardButtonText("Init Failed");
            Debug.LogWarning("GameBattleActionsUI: Init card failed: " + error);
        }

        private void TriggerCheckStatus()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (!this.CanCheckBattleStatus(scripts)) return;
            this.SetCheckStatusLoading(true);
            scripts.RunBattleStatus(this.OnBattleStatusSucceeded, this.OnBattleStatusFailed);
        }

        private void TriggerGetAllCardDefinitionsOnFirstStatus()
        {
            if (this.battleStatusFirstCallDone) return;
            this.battleStatusFirstCallDone = true;
            this.GetAllCardDefinitions();
        }

        private bool CanCheckBattleStatus(BattleScripts scripts)
        {
            if (scripts != null) return true;
            this.SetCheckStatusButtonText("No Script");
            return false;
        }

        private void SetCheckStatusLoading(bool isLoading)
        {
            if (this.btnCheckStatus == null) return;
            this.btnCheckStatus.SetEnabled(!isLoading);
            this.btnCheckStatus.text = isLoading ? "Checking..." : "Check Status";
        }

        private void SetCheckStatusButtonText(string text)
        {
            if (this.btnCheckStatus == null) return;
            this.btnCheckStatus.text = text;
        }

        private void OnBattleStatusSucceeded(string response)
        {
            this.SetCheckStatusLoading(false);
            this.SetCheckStatusButtonText("Status OK");
            this.TriggerGetAllCardDefinitionsOnFirstStatus();
            this.ApplyBattleStatusResponse(response);
        }

        private void OnBattleStatusFailed(string error)
        {
            this.SetCheckStatusLoading(false);
            this.SetCheckStatusButtonText("Status Failed");
            Debug.LogWarning("GameBattleActionsUI: Battle status failed: " + error);
        }

        private void TriggerEndBattle()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (!this.CanEndBattle(scripts)) return;
            this.SetEndBattleLoading(true);
            scripts.RunBattleEnd(this.OnBattleEndSucceeded, this.OnBattleEndFailed);
        }

        private bool CanEndBattle(BattleScripts scripts)
        {
            if (scripts != null) return true;
            this.SetEndBattleButtonText("No Script");
            return false;
        }

        private void SetEndBattleLoading(bool isLoading)
        {
            if (this.btnEndBattle == null) return;
            this.btnEndBattle.SetEnabled(!isLoading);
            this.btnEndBattle.text = isLoading ? "Ending..." : "End Battle";
        }

        private void SetEndBattleButtonText(string text)
        {
            if (this.btnEndBattle == null) return;
            this.btnEndBattle.text = text;
        }

        private void OnBattleEndSucceeded(string response)
        {
            this.SetEndBattleLoading(false);
            this.SetEndBattleButtonText("Battle Ended");
            this.getBattleStateCtrl()?.BattleState?.ClearData();
        }

        private void OnBattleEndFailed(string error)
        {
            this.SetEndBattleLoading(false);
            this.SetEndBattleButtonText("End Failed");
            Debug.LogWarning("GameBattleActionsUI: Battle end failed: " + error);
        }

        private void TriggerStartBattle()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (!this.CanStartBattle(scripts)) return;
            string enemyCodeName = this.GetEnemyCodeName();
            string presetInstanceId = this.selectedPreset.id;
            string requestBody = this.BuildBattleStartRequestBody(enemyCodeName, presetInstanceId);
            this.SetStartBattleLoading(true);
            scripts.RunBattleStart(requestBody, this.OnBattleStartSucceeded, this.OnBattleStartFailed);
        }

        private bool CanStartBattle(BattleScripts scripts)
        {
            if (this.selectedPreset == null)
            {
                this.SetStartBattleButtonText("Select Desk");
                return false;
            }
            if (string.IsNullOrWhiteSpace(this.selectedPreset.id))
            {
                this.SetStartBattleButtonText("Select Desk");
                return false;
            }
            if (string.IsNullOrWhiteSpace(this.GetEnemyCodeName()))
            {
                this.SetStartBattleButtonText("Enter Enemy");
                return false;
            }
            if (scripts != null) return true;
            this.SetStartBattleButtonText("No Script");
            return false;
        }

        private string GetEnemyCodeName()
        {
            if (this.enemyCodeNameInput == null) return string.Empty;
            if (this.enemyCodeNameInput.value == null) return string.Empty;
            return this.enemyCodeNameInput.value.Trim();
        }

        private string BuildBattleStartRequestBody(string enemyCodeName, string presetInstanceId)
        {
            BattleStartScriptRequest request = new BattleStartScriptRequest();
            request.payload = new BattleStartPayload();
            request.payload.battle_mode = BattleModeNormal;
            request.payload.enemy_entity_key = enemyCodeName;
            request.payload.preset_instance_id = presetInstanceId;
            return JsonUtility.ToJson(request);
        }

        private void SetStartBattleLoading(bool isLoading)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(!isLoading);
            this.btnStartBattle.text = isLoading ? "Starting..." : "Start Battle";
        }

        private void SetStartBattleButtonText(string text)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.text = text;
        }

        private void ResetStartBattleButtonText()
        {
            this.SetStartBattleButtonText("Start Battle");
        }

        private void OnBattleStartSucceeded(string response)
        {
            this.SetStartBattleLoading(false);
            this.SetStartBattleButtonText("Battle Started");
            this.GetAllCardDefinitions();
            this.ApplyBattleStatusResponse(response);
        }

        private void OnBattleStartFailed(string error)
        {
            this.SetStartBattleLoading(false);
            this.SetStartBattleButtonText("Battle Failed");
            Debug.LogWarning("GameBattleActionsUI: Battle start failed: " + error);
        }

        private void ApplyBattleStatusResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return;
            this.getBattleStateCtrl()?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void GetAllCardDefinitions()
        {
            BattleStateCtrl ctrl = this.getBattleStateCtrl();
            if (ctrl?.BattleCardDefinitions == null) return;
            ctrl.BattleCardDefinitions.GetAll();
        }
    }
}
