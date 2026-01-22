using System.Collections.Generic;

namespace Hikawa
{
	public record class SaveData
    {
        /// <summary>Map of bug IDs to collection data.</summary>
        public Dictionary<string, BugCollectionEntry> BugCollection;
        public Vector2 LostGlassesQuestTile;
        public Vector2 LostJewelryQuestTile;

        /// <summary>Collection tracking for Ami's bugs quest.</summary>
        public record class BugCollectionEntry
        {
            /// <summary>Number of this bug caught.</summary>
            public int Count;
            /// <summary>Days played when first caught.</summary>
            public int DaysPlayed;
        }

        public SaveData()
        {
            this.BugCollection = [];
        }
	}
}
