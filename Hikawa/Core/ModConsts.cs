using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using xTile.Dimensions;

namespace Hikawa
{
	internal class ModConsts
	{
		/* Mod data */
		// Directories
		internal const string ModName = "Hikawa";
		internal const string SaveDataKey = ModName;

		internal const string AssetsDirectory = "assets";
		internal const string TilesheetPrefix = "z_hikawa";
		internal const string SpritesDirectory = "Sprites";
		internal static readonly string EventsPath = Path.Combine("Data", "events.json");
		internal const string ExtraSpritesFile = TilesheetPrefix + "_extras";
		internal const string ArcadeSpritesFile = TilesheetPrefix + "_arcade";
		internal const string IndoorsSpritesFile = TilesheetPrefix + "_indoors";
		
		internal const string ContentPackPrefix = "blueberry.Hikawa.";
		internal static readonly string JaContentPackPath = Path.Combine(AssetsDirectory, "ContentPack");


		/* Game objects */
		// Objects
		internal const string ArcadeMinigameId = ModName + "LightGun";
		internal const string ArcadeObjectName = "Sailor V Arcade System";
		// NPCs
		internal const string ReiNpcId = "HikawaRei";
		internal const string AmiNpcId = "HikawaAmi";
		internal const string UsaNpcId = "HikawaUsagi";
		internal const string GrampsNpcId = "HikawaGrandpa";
		// Maps
		internal const string MapPrefix = ModName;
		internal const string ShrineMapId = MapPrefix + "Shrine";
		internal const string HouseMapId = MapPrefix + "House";
		internal const string TownSnippetId = MapPrefix + "Town";
		internal const string TownJojaSnippetId = TownSnippetId + "Joja";
		internal const string CorridorMapId = MapPrefix + "Corridor";
		internal const string NegativeMapId = MapPrefix + "Negative";
		// Tile actions
		internal const string ActionEma = MapPrefix + "Ema";
		internal const string ActionArcade = MapPrefix + "ArcadeMinigameId";
		internal const string ActionShrineHall = MapPrefix + "HallDoor";
		internal const string ActionShrineShop = MapPrefix + "OmiyageyaShop";
		internal const string ActionShrineOffering = MapPrefix + "Offering";
		internal const int OfferingCostS = 75;
		internal const int OfferingCostM = 330;
		internal const int OfferingCostL = 825;

		// Coordinates
		internal static readonly Vector2 StoryStockPosition = new Vector2(20, 10);
		internal static readonly Vector2 ShrineSouvenirShopPosition = new Vector2(28, 42) * 64f;
		internal static readonly Location ShrineDefaultWarpPosition = new Location(71, 40);
		internal static readonly Location ArcadeMachinePosition = new Location(40, 16);
		internal static readonly List<Location> CrowTilePositions = new List<Location>
		{
			new Location()
		};
		internal static readonly List<Vector2> StoryPlantPositionsForFarmTypes = new List<Vector2>
		{
			new Vector2(42, 27), // Standard
			new Vector2(22, 31), // River
			new Vector2(38, 16), // Forest
			new Vector2(61, 30), // Hilltop
			new Vector2(47, 18), // Wilderness
			new Vector2(38, 40)  // Four Corners
		};
		// Events and story
		internal enum Progress
		{
			None,
			Started,
			ExtraStage1,
			ExtraStage2,
			ExtraStage3,
			Complete
		}

		// Values and things
		internal const int BananaBegins = 3;
		internal const int BigBananaBonanza = 7;
	}
}
