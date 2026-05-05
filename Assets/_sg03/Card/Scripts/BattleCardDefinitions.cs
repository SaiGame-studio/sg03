using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    public class BattleCardDefinitions : SaiBehaviour
    {
        public static event Action OnDefinitionsLoaded;

        [Header("Battle Script")]
        [SerializeField] private BattleScripts battleScripts;

        [Header("Card Definitions Cache")]
        [SerializeField] private List<string> codes = new List<string>();
        [SerializeField] private List<CardDefinitionData> definitions = new List<CardDefinitionData>();

        public IReadOnlyList<string> Codes => this.codes;
        public IReadOnlyList<CardDefinitionData> Definitions => this.definitions;
        public bool IsLoaded => this.definitions.Count > 0;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleScripts();
        }

        protected virtual void LoadBattleScripts()
        {
            if (this.battleScripts != null) return;
            this.battleScripts = GameObject.FindAnyObjectByType<BattleScripts>();
            if (this.battleScripts == null) return;
            Debug.LogWarning(this.transform.name + ": LoadBattleScripts", this.gameObject);
        }

        public void GetAll()
        {
            Debug.Log("<color=#FFD700>[BattleCardDefinitions] GetAll — calling RunScript</color>", this);
            if (this.battleScripts == null) return;
            this.battleScripts.RunGetCardDefinitions(this.ParseResponse, null);
        }

        public CardDefinitionData GetDefinitionByCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            CardDefinitionData result = this.definitions.Find(d => d.item_code == code);
            if (result == null) Debug.LogWarning($"<color=#FF4444>[BattleCardDefinitions] Definition NOT FOUND for code '<b>{code}</b>'. Total definitions loaded: {this.definitions.Count}</color>", this);
            return result;
        }

        private void ParseResponse(string rawJson)
        {
            CardDefinitionsResponse response = JsonUtility.FromJson<CardDefinitionsResponse>(rawJson);
            if (response == null) return;
            if (response.output == null) return;
            this.ApplyCodes(response.output.codes);
            this.ApplyDefinitions(response.output.definitions);
        }

        private void ApplyCodes(string[] rawCodes)
        {
            this.codes.Clear();
            if (rawCodes == null) return;
            this.codes.AddRange(rawCodes);
        }

        private void ApplyDefinitions(CardDefinitionData[] rawDefinitions)
        {
            this.definitions.Clear();
            if (rawDefinitions == null)
            {
                Debug.LogWarning("<color=#FF4444>[BattleCardDefinitions] ApplyDefinitions received null array</color>", this);
                return;
            }
            this.definitions.AddRange(rawDefinitions);
            Debug.Log($"<color=#FFD700>[BattleCardDefinitions] Loaded <b>{this.definitions.Count}</b> definitions.</color>", this);
            OnDefinitionsLoaded?.Invoke();
        }
    }
}
