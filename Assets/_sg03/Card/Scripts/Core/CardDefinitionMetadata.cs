using System;

namespace SG03
{
    /// <summary>
    /// All possible string fields returned by the server in a card definition's
    /// <c>metadata</c> object. Missing fields default to an empty string.
    /// Field names match the server JSON keys exactly for use with <c>JsonUtility</c>.
    /// </summary>
    [Serializable]
    public class CardDefinitionMetadata
    {
        public string description;
        public string race;
        public string type;
        public string gender;
        public string char_code;
        public string abilities;
        public string summon;
    }
}
