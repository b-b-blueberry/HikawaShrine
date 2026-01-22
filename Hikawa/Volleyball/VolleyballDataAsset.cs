using System.Collections.Generic;

namespace Hikawa.Volleyball
{
    public class VolleyballDataAsset
    {
        /// <summary>Map of volleyball IDs and data.</summary>
        public Dictionary<string, VolleyballBallData> Balls;
        /// <summary>Map of character internal names and volleyball data.</summary>
        public Dictionary<string, VolleyballCharacterData> Characters;
    }
}
