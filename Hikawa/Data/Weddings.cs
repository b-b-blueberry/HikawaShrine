using StardewValley.GameData.Weddings;
using System.Collections.Generic;

namespace Hikawa.Data
{
    public class WeddingData
    {
        public string EventScript;
        public Dictionary<string, WeddingLocationData> Locations;
        /// <summary>Map of wedding group IDs (as unique dialogue responses) to <see cref="WeddingAttendeeData"/>.</summary>
        public Dictionary<string, WeddingAttendeeGroupData> AttendeeGroups;
        public Dictionary<string, string> AttendeeDialogues;
        public Dictionary<string, WeddingOfficiantData> Officiants;
        public Dictionary<string, WeddingDecorationData> Decorations;
    }

    public class WeddingLocationData
    {
        public string DisplayName;
        public string Condition;
        /// <summary>Tile offset applied to <see cref="WeddingAttendeeData"/>.</summary>
        public Point WeddingOffset;
    }

    public class WeddingAttendeeGroupData
    {
        public string DisplayName;
        public string Condition;
        /// <summary>Replacement attendees at wedding. If empty, nobody will attend. If null, the default attendees are used.</summary>
        public Dictionary<string, WeddingAttendeeData> Attendees;
    }

    public class WeddingOfficiantData
    {
        public string DisplayName;
        public string Condition;
        public string EventScript;
    }

    public class WeddingDecorationData
    {
        public string DisplayName;
        public string Condition;
        public string EventScript;
    }
}
