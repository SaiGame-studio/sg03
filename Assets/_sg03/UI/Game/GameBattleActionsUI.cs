using System;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private const string DefaultEnemyCodeName = "goblin_shaman";
        private const string NewGameButtonText = "New Game";
        private const string ResumeButtonText = "Resume";

        private readonly Func<BattleScripts> getBattleScripts;
        private readonly Func<BattleStateCtrl> getBattleStateCtrl;

        private PresetData selectedPreset;

        private Button btnEndBattle;
        private Button btnStartBattle;
        private DropdownField enemyCodeNameInput;

        private bool hasActiveBattleSession;

        public event Action OnBattleStartedOrResumed;

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
            this.btnStartBattle = root.Q<Button>("BtnStartBattle");
            this.enemyCodeNameInput = root.Q<DropdownField>("EnemyCodeNameInput");
            this.enemyCodeNameInput?.SetValueWithoutNotify(DefaultEnemyCodeName);
        }

        private void RegisterCallbacks()
        {
            this.btnEndBattle?.RegisterCallback<ClickEvent>(_ => this.OnEndBattleClicked());
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

        public void SetBattleSessionAvailabilityLoading()
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(false);
            this.btnStartBattle.text = "Checking...";
        }

        public void SetBattleSessionAvailability(bool hasActiveBattleSession)
        {
            this.hasActiveBattleSession = hasActiveBattleSession;
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(true);
            this.btnStartBattle.text = this.hasActiveBattleSession ? ResumeButtonText : NewGameButtonText;
        }



        protected virtual void OnEndBattleClicked()
        {
            this.TriggerEndBattle();
        }

        protected virtual void OnStartBattleClicked()
        {
            if (this.hasActiveBattleSession)
            {
                this.TriggerResumeBattle();
                return;
            }
            this.TriggerStartBattle();
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
            this.btnEndBattle.text = isLoading ? "Ending..." : "End Game";
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
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()) return;
            if (string.IsNullOrWhiteSpace(activeScene.name)) return;
            SceneManager.LoadScene(activeScene.name);
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

        private void TriggerResumeBattle()
        {
            BattleScripts scripts = this.getBattleScripts();
            if (scripts == null)
            {
                this.SetStartBattleButtonText("No Script");
                return;
            }

            this.SetResumeBattleLoading(true);
            scripts.RunBattleStatus(this.OnResumeBattleSucceeded, this.OnResumeBattleFailed);
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

        private void SetResumeBattleLoading(bool isLoading)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(!isLoading);
            this.btnStartBattle.text = isLoading ? "Resuming..." : ResumeButtonText;
        }

        private void SetStartBattleButtonText(string text)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.text = text;
        }

        private void ResetStartBattleButtonText()
        {
            this.SetStartBattleButtonText(this.hasActiveBattleSession ? ResumeButtonText : NewGameButtonText);
        }

        private void OnResumeBattleSucceeded(string response)
        {
            this.SetResumeBattleLoading(false);
            this.GetAllCardDefinitions();
            this.ApplyBattleStatusResponse(response);
            this.OnBattleStartedOrResumed?.Invoke();
        }

        private void OnResumeBattleFailed(string error)
        {
            this.SetResumeBattleLoading(false);
            Debug.LogWarning("GameBattleActionsUI: Resume battle failed: " + error);
        }

        private void OnBattleStartSucceeded(string response)
        {
            this.hasActiveBattleSession = true;
            this.SetStartBattleLoading(false);
            this.SetStartBattleButtonText("Battle Started");
            this.GetAllCardDefinitions();
            this.ApplyBattleStatusResponse(response);
            this.OnBattleStartedOrResumed?.Invoke();
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
