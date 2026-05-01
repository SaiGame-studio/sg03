using SaiGame.Services;
using UnityEngine;

namespace SG03.UI
{
    /// <summary>
    /// Singleton cache that stores the latest battle state.
    /// Any script that receives a battle_status response should call UpdateFromBattleStatus
    /// to keep this cache up to date. All data is exposed as readonly properties.
    /// </summary>
    public class BattleState : SaiSingleton<BattleState>
    {
        [Header("Battle Status Cache — Read Only")]
        [SerializeField][TextArea(5, 20)] private string battleStatusJson;
        [SerializeField] private int    alphaHp;
        [SerializeField] private int    omegaHp;
        [SerializeField] private int    alphaTheSourceCount;
        [SerializeField] private int    omegaTheSourceCount;
        [SerializeField] private int    alphaTheVoidCount;
        [SerializeField] private int    omegaTheVoidCount;
        [SerializeField] private string[] alphaTheSource;
        [SerializeField] private BattleCardSlot[] alphaHand;
        [SerializeField] private BattleCardSlot[] alphaBackLine;
        [SerializeField] private BattleCardSlot[] alphaFrontLine;

        public string   BattleStatusJson      => this.battleStatusJson;
        public int      AlphaHp               => this.alphaHp;
        public int      OmegaHp               => this.omegaHp;
        public int      AlphaTheSourceCount   => this.alphaTheSourceCount;
        public int      OmegaTheSourceCount   => this.omegaTheSourceCount;
        public int      AlphaTheVoidCount     => this.alphaTheVoidCount;
        public int      OmegaTheVoidCount     => this.omegaTheVoidCount;
        public string[] AlphaTheSource        => this.alphaTheSource;
        public BattleCardSlot[] AlphaHand      => this.alphaHand;
        public BattleCardSlot[] AlphaBackLine  => this.alphaBackLine;
        public BattleCardSlot[] AlphaFrontLine => this.alphaFrontLine;

        /// <summary>
        /// Called by any script that receives a raw battle_status JSON response.
        /// Stores the raw JSON and parses all fields into the cache.
        /// </summary>
        public void UpdateFromBattleStatus(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return;
            this.battleStatusJson = BeautifyJson(rawJson);
            this.ParseAndApplyBattleStatus(rawJson);
        }

        private static string BeautifyJson(string json)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int  indent   = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;

                if (inString)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.Append('\n');
                        indent++;
                        sb.Append(new string(' ', indent * 4));
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent--;
                        sb.Append(new string(' ', indent * 4));
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 4));
                        break;
                    case ':':
                        sb.Append(c);
                        sb.Append(' ');
                        break;
                    default:
                        if (!char.IsWhiteSpace(c))
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private void ParseAndApplyBattleStatus(string rawJson)
        {
            BattleStatusScriptResponse response = JsonUtility.FromJson<BattleStatusScriptResponse>(rawJson);
            if (response == null) return;
            if (response.output == null) return;
            this.ApplyOutput(response.output);
        }

        private void ApplyOutput(BattleStatusOutput output)
        {
            this.alphaHp             = output.alpha_hp;
            this.omegaHp             = output.omega_hp;
            this.alphaTheSourceCount = output.alpha_the_source_count;
            this.omegaTheSourceCount = output.omega_the_source_count;
            this.alphaTheVoidCount   = output.alpha_the_void_count;
            this.omegaTheVoidCount   = output.omega_the_void_count;
            this.alphaTheSource      = output.alpha_the_source;
            this.alphaHand           = output.alpha_hand;
            this.alphaBackLine       = output.alpha_back_line;
            this.alphaFrontLine      = output.alpha_front_line;
        }
    }
}
