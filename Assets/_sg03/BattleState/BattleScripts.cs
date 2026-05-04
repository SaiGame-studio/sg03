using System;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [SerializeField] private BattleScript battleScript;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleScript();
        }

        protected virtual void OnEnable()
        {
            this.SubscribeSceneLoaded();
        }

        protected virtual void OnDisable()
        {
            this.UnsubscribeSceneLoaded();
        }

        private void SubscribeSceneLoaded()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }

        private void UnsubscribeSceneLoaded()
        {
            SceneManager.sceneLoaded -= this.OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            this.ResetBattleScriptReference();
            this.LoadBattleScript();
        }

        private void ResetBattleScriptReference()
        {
            this.battleScript = null;
        }

        protected virtual void LoadBattleScript()
        {
            if (this.battleScript != null) return;
            SaiServer server = SaiServer.Instance;
            if (server == null) return;
            this.battleScript = server.BattleScript;
            if (this.battleScript == null) return;
            Debug.LogWarning(this.transform.name + ": LoadBattleScript", this.gameObject);
        }

        public void RunBattleStart(string requestBody, Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            Debug.Log("<color=#00FFCC><b>[BattleScripts] ► RunBattleStart</b></color>", this.gameObject);
            this.battleScript.RunScript(this.scriptNameBattleStart, requestBody, onSuccess, onError);
        }

        public void RunBattleEnd(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            Debug.Log("<color=#FF6B6B><b>[BattleScripts] ► RunBattleEnd</b></color>", this.gameObject);
            this.battleScript.RunScript(this.scriptNameBattleEnd, null, onSuccess, onError);
        }

        public void RunBattleStatus(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            Debug.Log("<color=#FFD700><b>[BattleScripts] ► RunBattleStatus</b></color>", this.gameObject);
            this.battleScript.RunScript(this.scriptNameBattleStatus, null, onSuccess, onError);
        }

        public void RunInitCards(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            Debug.Log("<color=#88DDFF><b>[BattleScripts] ► RunInitCards</b></color>", this.gameObject);
            this.battleScript.RunScript(this.scriptNameInitCards, null, onSuccess, onError);
        }

        public void RunGetCardDefinitions(Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            Debug.Log("<color=#AAFFAA><b>[BattleScripts] ► RunGetCardDefinitions</b></color>", this.gameObject);
            this.battleScript.RunScript(this.scriptNameGetCardDefinitions, null, onSuccess, onError);
        }

        public void RunCardDeploy(string[] frontLine, string[] backLine, Action<string> onSuccess, Action<string> onError)
        {
            if (this.battleScript == null) return;
            string requestBody = this.BuildCardDeployRequestBody(frontLine, backLine);
            Debug.Log("<color=#FF88FF><b>[BattleScripts] ► RunCardDeploy</b></color>", this.gameObject);
            this.battleScript.RunScript(this.scriptNameCardDeploy, requestBody, onSuccess, onError);
        }

        private string BuildCardDeployRequestBody(string[] frontLine, string[] backLine)
        {
            string frontJson = this.ToJsonStringArray(frontLine);
            string backJson  = this.ToJsonStringArray(backLine);
            return $"{{\"payload\":{{\"front_line\":{frontJson},\"back_line\":{backJson}}}}}";
        }

        private string ToJsonStringArray(string[] items)
        {
            if (items == null || items.Length == 0) return "[]";
            string joined = string.Join(",", System.Array.ConvertAll(items, id => $"\"{id}\""));
            return $"[{joined}]";
        }
    }
}
