using System;

namespace SG03.UI
{
    [Serializable]
    public class BattleSessionExistsScriptResponse
    {
        public BattleSessionExistsOutput output;
    }

    [Serializable]
    public class BattleSessionExistsOutput
    {
        public bool exists;
        public string session_id;
        public string error;
    }
}
