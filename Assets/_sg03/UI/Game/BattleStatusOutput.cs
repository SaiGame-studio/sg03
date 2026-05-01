using System;

namespace SG03.UI
{
    [Serializable]
    public class BattleStatusOutput
    {
        public int    alpha_hp;
        public int    omega_hp;
        public string[] alpha_the_source;
        public string[] omera_the_source;
        public int    alpha_the_source_count;
        public int    omega_the_source_count;
        public int    alpha_the_void_count;
        public int    omega_the_void_count;
    }
}