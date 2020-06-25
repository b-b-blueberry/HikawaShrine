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
		internal const string ModName = "blueberry.Hikawa";
		internal const string ContentPrefix = ModName + ".";
		internal const string SaveDataKey = ModName;

		internal const string AssetsPath = "assets";
		internal static readonly string SpritesPath = Path.Combine(AssetsPath, "LooseSprites");
		internal static readonly string EventsPath = Path.Combine("Data", "events.json");
		internal static readonly string JaContentPackPath = Path.Combine(AssetsPath, "ContentPack");

		internal const string TilesheetPrefix = "z_hikawa";
		internal const string ExtraSpritesFile = TilesheetPrefix + "_extras";
		internal const string BuffIconSpritesFile = TilesheetPrefix + "_bufficons";
		internal const string ArcadeSpritesFile = TilesheetPrefix + "_arcade";
		internal const string IndoorsSpritesFile = TilesheetPrefix + "_indoors";
		internal const string CrowSpritesFile = TilesheetPrefix + "_crows";
		internal const string CatSpritesFile = TilesheetPrefix + "_cats";


		/* Game objects */
		// Objects
		private const string ArcadeMinigameName = "LightGun";
		internal const string ArcadeMinigameId = ModName + ArcadeMinigameName;
		internal const string ArcadeObjectName = "Sailor V Arcade System";
		// NPCs
		internal const string ReiNpcId = ContentPrefix + "Rei";
		internal const string AmiNpcId = ContentPrefix + "Ami";
		internal const string UsaNpcId = ContentPrefix + "Usagi";
		internal const string GrampsNpcId = ContentPrefix + "Grandpa";
		// Maps
		internal const string ShrineMapId = ContentPrefix + "Shrine";
		internal const string HouseMapId = ContentPrefix + "House";
		internal const string TownSnippetId = ContentPrefix + "Town";
		internal const string TownJojaSnippetId = TownSnippetId + "." + "Joja";
		internal const string CorridorMapId = ContentPrefix + "Corridor";
		internal const string NegativeMapId = ContentPrefix + "Negative";
		// Tile actions
		internal const string ActionEma = ContentPrefix + "Ema";
		internal const string ActionShrineHall = ContentPrefix + "HallDoor";
		internal const string ActionShrineShop = ContentPrefix + "OmiyageyaShop";
		internal const string ActionShrineOffering = ContentPrefix + "Offering";
		internal const string ActionLockbox = ContentPrefix + "Lockbox";
		internal const string ActionSit = ContentPrefix + "Sit";
		internal const string ActionArcade = ContentPrefix + ArcadeMinigameName;
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
		internal const int BuffId = 870084643;
		internal const int BananaBegins = 3;
		internal const int BigBananaBonanza = 7;
	}
}
