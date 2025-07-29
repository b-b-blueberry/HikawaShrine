using System.Collections.Generic;

namespace Hikawa.Data
{
    public class ShrubsDataAsset
    {
        /// <summary>
        /// Keyed by shrub ID
        /// </summary>
        public Dictionary<string, ShrubDataEntry> Shrubs;
    }

    public record class ShrubDataEntry
    {
        public string Texture;
        public Rectangle SourceRect;
        public Vector2 Origin;
        /// <summary>
        /// Translated display name.
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// Translated scientific binomial.
        /// </summary>
        public string ScientificName;
        /// <summary>
        /// Translated description.
        /// </summary>
        public string Description;
        public int MaxSize;
    }
}
