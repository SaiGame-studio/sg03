using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    public class BattleCardDefinitions : BattleScript
    {
        [Header("Card Definitions Cache")]
        [SerializeField] private List<string> codes = new List<string>();
        [SerializeField] private List<CardDefinitionData> definitions = new List<CardDefinitionData>();

        public IReadOnlyList<string> Codes => this.codes;
        public IReadOnlyList<CardDefinitionData> Definitions => this.definitions;

        protected override void ResetValue()
        {
            base.ResetValue();
            this.scriptName = "get_card_definitions";
        }

        public void GetAll()
        {
            this.RunScript(onSuccess: this.ParseResponse);
        }

        public CardDefinitionData GetDefinitionByCode(string code)
        {
            return this.definitions.Find(d => d.item_code == code);
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
            if (rawDefinitions == null) return;
            this.definitions.AddRange(rawDefinitions);
        }
    }
}
