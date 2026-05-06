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

        private BattleScript battleScript => SaiServer.Instance != null ? SaiServer.Instance.BattleScript : null;

        [SerializeField] private BattleState battleState;

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
            if (this.battleScript == null) return;
            this.LogPayload("RunBattleStart", "#00FFCC", requestBody);
            this.battleScript.RunScript(this.scriptNameBattleStart, requestBody, onSuccess, onError);
        }

        public void RunBattleEnd(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            this.LogPayload("RunBattleEnd", "#FF6B6B", null);
            this.battleScript.RunScript(this.scriptNameBattleEnd, null, onSuccess, onError);
        }

        public void RunBattleStatus(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            this.LogPayload("RunBattleStatus", "#FFD700", null);
            this.battleScript.RunScript(this.scriptNameBattleStatus, null, onSuccess, onError);
        }

        public void RunInitCards(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            this.LogPayload("RunInitCards", "#88DDFF", null);
            this.battleScript.RunScript(this.scriptNameInitCards, null, onSuccess, onError);
        }

        public void RunGetCardDefinitions(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            this.LogPayload("RunGetCardDefinitions", "#AAFFAA", null);
            this.battleScript.RunScript(this.scriptNameGetCardDefinitions, null, onSuccess, onError);
        }

        public void RunCardDeploy(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            string requestBody = this.BuildCardDeployRequestBody();
            this.LogPayload("RunCardDeploy", "#FF88FF", requestBody);
            this.battleScript.RunScript(this.scriptNameCardDeploy, requestBody, onSuccess, onError);
        }

        private void LogPayload(string methodName, string color, string payload)
        {
            if (!this.logPayload) return;
            string payloadPart = string.IsNullOrEmpty(payload) ? string.Empty : "\n" + payload;
            Debug.Log($"<color={color}><b>[BattleScripts] \u25ba {methodName}</b></color>{payloadPart}", this.gameObject);
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
