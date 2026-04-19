using System.Collections.Generic;

namespace Hikawa
{
	public record class SaveData
    {
        /// <summary>Map of bug IDs to collection data.</summary>
        public Dictionary<string, BugCollectionEntry> BugCollection;
        public Vector2 LostGlassesQuestTile;
        public Vector2 LostJewelryQuestTile;
        public Match3SaveData Match3 = new();

        /// <summary>Collection tracking for Ami's bugs quest.</summary>
        public record class BugCollectionEntry
        {
            /// <summary>Number of this bug caught.</summary>
            public int Count;
            /// <summary>Days played when first caught.</summary>
            public int DaysPlayed;
        }

        public record class Match3SaveData
        {
            // progress

            public HashSet<string> Characters = [];
            public Dictionary<string, HashSet<string>> StoryStageComplete = [];

            // stats

            public long TotalTime;
            public long TotalScore;
            public int TotalMoves;
            public int TotalPowers;
            public int TotalSuperPowers;
            public int TotalMatches;
            public int TotalPowerMatches;
            public int TotalSuperPowerMatches;
        }

        public SaveData()
        {
            this.BugCollection = [];
        }
	}
}
