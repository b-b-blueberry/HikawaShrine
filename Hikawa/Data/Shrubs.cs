using StardewValley.GameData;
using System.Collections.Generic;

namespace Hikawa.Data
{
    public class ShrubsDataAsset
    {
        public ShrubToolData ShrubToolData;
        public ShrubObjectData ShrubObjectData;
        /// <summary>
        /// Keyed by globally-unique shrub ID
        /// </summary>
        public Dictionary<string, ShrubDataEntry> Shrubs;
    }

    public record class ShrubToolData
    {
        public string Identifier;
        public string ItemId;

        public string DisplayName;
        public string Description;

        public int Category;
        public string Type;

        public string TextureId;
        public Rectangle IconTextureRegion;
    }

    public record class ShrubObjectData
    {
        public string Identifier;
        public List<string> ContextTags;
    }

    public record class ShrubDataEntry
    {
        public string DisplayName;
        public string Description;

        public List<PlantableRule> PlantableLocationRules;

        public int MaxGrowthStage;
        public int GrowthDays;

        public string TextureId;
        public Rectangle IconTextureRegion;
        public List<ShrubAppearanceData> Appearances;
    }

    public record class ShrubAppearanceData
    {
        public string Id;
        public int GrowthStage;

        public string DebrisColour;

        public Season Season;
        public string Condition;

        public List<ShrubDrawLayerData> DrawLayers;
    }

    public record class ShrubDrawLayerData
    {
        public string Id;

        public Rectangle TextureRegion;
        public Vector2 TextureOrigin;
        public Vector2 TextureOffset;

        public bool Shake = true;
    }
}
