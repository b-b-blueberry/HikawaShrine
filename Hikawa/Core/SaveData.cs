using System.Collections.Generic;

namespace Hikawa
{
	public record class SaveData
    {
        /// <summary>
        /// Map of bug IDs to days-played when caught.
        /// </summary>
        public Dictionary<string, int> BugCollection;
        public int BellRingCount;
        public Vector2 LostGlassesQuestTile;
        public Vector2 LostJewelryQuestTile;

        public SaveData()
        {
            this.BugCollection = [];
        }
	}
}
