using System;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Central facade for all Lua script calls in SG03.
    /// Defines script name constants and typed entry-point methods.
    /// Delegates every call to BattleScript (SaiGame) which sends the
    /// request to the backend Lua runner endpoint.
    /// </summary>
    public class BattleScripts : SaiBehaviour
    {
        [Header("Script Names")]
        [SerializeField] private string scriptNameBattleStart        = "battle_start";
        [SerializeField] private string scriptNameBattleEnd          = "battle_end";
        [SerializeField] private string scriptNameBattleStatus       = "battle_status";
        [SerializeField] private string scriptNameInitCards          = "init_cards";
        [SerializeField] private string scriptNameGetCardDefinitions = "get_card_definitions";
        [SerializeField] private string scriptNameCardDeploy         = "card_deploy";
        [SerializeField] private string scriptNameAlphaAttacking     = "alpha_attacking";

        private BattleScript battleScript => SaiServer.Instance != null ? SaiServer.Instance.BattleScript : null;

        [SerializeField] private BattleState battleState;

        private bool isRunning;

        /// <summary>True while a script request is in-flight (waiting for server response).</summary>
        public bool IsRunning => this.isRunning;

        [Header("Debug")]
        [SerializeField] private bool logPayload = true;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleState();
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            this.battleState = UnityEngine.Object.FindFirstObjectByType<BattleState>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        public void RunBattleStart(string requestBody, Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunBattleStart))) return;
            this.LogPayload("RunBattleStart", "#00FFCC", requestBody);
            this.RunWithLock(this.scriptNameBattleStart, requestBody, onSuccess, onError);
        }

        public void RunBattleEnd(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunBattleEnd))) return;
            this.LogPayload("RunBattleEnd", "#FF6B6B", null);
            this.RunWithLock(this.scriptNameBattleEnd, null, onSuccess, onError);
        }

        public void RunBattleStatus(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunBattleStatus))) return;
            this.LogPayload("RunBattleStatus", "#FFD700", null);
            this.RunWithLock(this.scriptNameBattleStatus, null, onSuccess, onError);
        }

        public void RunInitCards(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunInitCards))) return;
            this.LogPayload("RunInitCards", "#88DDFF", null);
            this.RunWithLock(this.scriptNameInitCards, null, onSuccess, onError);
        }

        public void RunGetCardDefinitions(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunGetCardDefinitions))) return;
            this.LogPayload("RunGetCardDefinitions", "#AAFFAA", null);
            this.RunWithLock(this.scriptNameGetCardDefinitions, null, onSuccess, onError);
        }

        public void RunCardDeploy(Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunCardDeploy))) return;
            string requestBody = this.BuildCardDeployRequestBody();
            this.LogPayload("RunCardDeploy", "#FF88FF", requestBody);
            this.RunWithLock(this.scriptNameCardDeploy, requestBody, onSuccess, onError);
        }

        public void RunAlphaAttacking(string attackerInventoryItemId, string defenderInventoryItemId, Action<string> onSuccess, Action<string> onError)
        {
            if (this.IsBattleScriptMissing(nameof(this.RunAlphaAttacking))) return;
            string requestBody = this.BuildAlphaAttackingRequestBody(attackerInventoryItemId, defenderInventoryItemId);
            this.LogPayload("RunAlphaAttacking", "#FF4444", requestBody);
            this.RunWithLock(this.scriptNameAlphaAttacking, requestBody, onSuccess, onError);
        }

        /// <summary>Guards against concurrent requests; acquires the lock and dispatches the script call.</summary>
        private void RunWithLock(string scriptName, string requestBody, Action<string> onSuccess, Action<string> onError)
        {
            if (this.isRunning)
            {
                Debug.LogWarning($"[BattleScripts] '{scriptName}' blocked — a script is already in-flight.", this.gameObject);
                return;
            }

            this.isRunning = true;
            this.battleScript.RunScript(scriptName, requestBody, this.ReleaseLockThen(onSuccess), this.ReleaseLockThen(onError));
        }

        /// <summary>Returns a wrapper that releases the in-flight lock and then invokes the original callback.</summary>
        private Action<string> ReleaseLockThen(Action<string> callback)
        {
            return result =>
            {
                this.isRunning = false;
                callback?.Invoke(result);
            };
        }

        private bool IsBattleScriptMissing(string callerName)
        {
            if (this.battleScript != null) return false;
            string serverState = SaiServer.Instance == null ? "SaiServer=null" : "SaiServer=ok, BattleScript=null";
            Debug.LogWarning($"[BattleScripts] {callerName}: battleScript is null ({serverState})", this.gameObject);
            return true;
        }

        private void LogPayload(string methodName, string color, string payload)
        {
            if (!this.logPayload) return;
            string payloadPart = string.IsNullOrEmpty(payload) ? string.Empty : "\n" + payload;
            Debug.Log($"<color={color}><b>[BattleScripts] \u25ba {methodName}</b></color>{payloadPart}", this.gameObject);
        }

        private string BuildAlphaAttackingRequestBody(string attackerInventoryItemId, string defenderInventoryItemId)
        {
            return $"{{\"payload\":{{\"attacker_inventory_item_id\":\"{attackerInventoryItemId}\",\"defender_inventory_item_id\":\"{defenderInventoryItemId}\"}}}}";
        }

        private string BuildCardDeployRequestBody()
        {
            string sessionId  = this.battleState != null ? this.battleState.SessionId : string.Empty;
            string[] hand      = this.CollectInventoryIds(this.battleState?.AlphaHand);
            CardDeployLineSlot[] frontLine = this.CollectLineSlots(this.battleState?.AlphaFrontLine);
            CardDeployLineSlot[] backLine  = this.CollectLineSlots(this.battleState?.AlphaBackLine);
            string handJson  = this.ToJsonStringArray(hand);
            string frontJson = this.ToJsonLineSlotArray(frontLine);
            string backJson  = this.ToJsonLineSlotArray(backLine);
            return $"{{\"payload\":{{\"session_id\":\"{sessionId}\",\"hand\":{handJson},\"front_line\":{frontJson},\"back_line\":{backJson}}}}}";
        }

        private CardDeployLineSlot[] CollectLineSlots(BattleCardSlot[] slots)
        {
            if (slots == null) return new CardDeployLineSlot[0];
            CardDeployLineSlot[] result = new CardDeployLineSlot[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                BattleCardSlot slot = slots[i];
                result[i] = new CardDeployLineSlot
                {
                    inventory_item_id = slot?.inventory_item_id ?? string.Empty,
                    face_up           = slot?.face_up ?? false,
                    slot_index        = slot?.slot_index ?? i
                };
            }
            return result;
        }

        private string ToJsonLineSlotArray(CardDeployLineSlot[] slots)
        {
            if (slots == null || slots.Length == 0) return "[]";
            System.Text.StringBuilder sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < slots.Length; i++)
            {
                if (i > 0) sb.Append(",");
                string faceUpStr = slots[i].face_up ? "true" : "false";
                sb.Append($"{{\"inventory_item_id\":\"{slots[i].inventory_item_id}\",\"face_up\":{faceUpStr},\"slot_index\":{slots[i].slot_index}}}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private string[] CollectInventoryIds(BattleCardSlot[] slots)
        {
            if (slots == null) return new string[0];
            string[] ids = new string[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                ids[i] = slots[i]?.inventory_item_id ?? string.Empty;
            return ids;
        }

        private string ToJsonStringArray(string[] items)
        {
            if (items == null || items.Length == 0) return "[]";
            string joined = string.Join(",", System.Array.ConvertAll(items, id => $"\"{id}\""));
            return $"[{joined}]";
        }
    }
}
