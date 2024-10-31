using System.Collections.Generic;
using Microsoft.Xna.Framework;
using xTile.Dimensions;

namespace Hikawa
{
	public class ModConsts
	{
		/* Mod data */
		// IDs
		public static string CoreModID => ModEntry.Instance.ModManifest.UniqueID;
		public static string ContentModID => string.Join(".", CoreModID, "CP");
		public static string ArcadeModID => string.Join(".", CoreModID, "Arcade");
		// Directories
		public const string ContentPrefix = "Custom_Hikawa_";
		public const string SaveDataKey = ContentPrefix + "SaveData";

		/* Game objects */
		// NPCs
		public const string NpcRei = ContentPrefix + "Rei";
		public const string NpcAmi = ContentPrefix + "Ami";
		public const string NpcCat = ContentPrefix + "Cat";
		public const string NpcUsa = ContentPrefix + "Usagi";
		public const string NpcMako = ContentPrefix + "Mako";
		public const string NpcMina = ContentPrefix + "Mina";
		public const string NpcGuy = ContentPrefix + "Yuichiro";
		public const string NpcGramps = ContentPrefix + "Gramps";
		public const string NpcVolleyballSuffix = "_Volleyball";
		// Maps
		public const string MapShrine = ContentPrefix + "Shrine";
		public const string MapHouse = ContentPrefix + "House";
		public const string MapHall = ContentPrefix + "Hall";
		public const string MapTown = ContentPrefix + "Town";
		public const string MapTownJoja = MapTown + "Joja";
		public const string MapTime = ContentPrefix + "Corridor";
		public const string MapVortex = ContentPrefix + "Vortex";
		public const string MapRoof = ContentPrefix + "Roof";
		public const string MapVolleyball = ContentPrefix + "Volleyball";
		// Map properties
		public const string PropertyAnimals = ContentPrefix + "Animals";
		public const string PropertyLantern = ContentPrefix + "Lantern";
		public const string PropertyTotem = ContentPrefix + "Totem";
		public const string PropertyShop = ContentPrefix + "Shop";
		public const string PropertyChest = ContentPrefix + "Chest";
		public const string PropertyHearth = ContentPrefix + "Hearth";
		public const string PropertyLamps = ContentPrefix + "Lamps";
		// Tile actions
		public const string ActionShrineHall = ContentPrefix + "HallDoor";
		public const string ActionShrineOffering = ContentPrefix + "Offering";
		public const string ActionBackDoor = ContentPrefix + "BackDoor";
		public const string ActionLockbox = ContentPrefix + "Lockbox";
		public const string ActionWardrobe = ContentPrefix + "Wardrobe";
        public const string ActionEma = ContentPrefix + "Ema";
        public const string ActionVortex = ContentPrefix + "Vortex";
		// Tile properties
		public const string PropertyDummy = ContentPrefix + "Dummy";
		public const string TilePetalSpawner = ContentPrefix + "PetalSpawner";
		// Touch actions
		public const string TouchHop = ContentPrefix + "Hop";
		// Custom fields
		public const string FieldLights = ContentPrefix + "Lights";
		// Event commands
		public const string EventCommandCrystalBall = ContentPrefix + "CrystalBall";
		// Items
		public const string ItemWand = ContentPrefix + "Wand";
        public const string ItemMirror = ContentPrefix + "Mirror";
        public const string ItemTotem = ContentPrefix + "Totem";
        public const string ItemLostGlasses = ContentPrefix + "LostGlasses";
        public const string ItemLostJewelry = ContentPrefix + "LostJewelry";
		// Tilesheets
		public const string HouseTilesheetName = "z_Custom_Hikawa_House";
		// Coordinates
		public static readonly Vector2 HouseChimneyTile = new(x: 53, y: 22);
		public static readonly Location CrowSpawnRadius = new(x: 2, y: 2);
		public static readonly xTile.Dimensions.Rectangle CrowSpawnArea = new(x: 23, y: 31, width: 30, height: 20);
		public static readonly Vector2[] CrowTopOfStairsSpawn = [new(33.5f, 53f), new(35f, 54f)];
		public static readonly Dictionary<float, Vector2[]> CrowPerches = new()
		{   // Keyed by chance to appear, chance is measured by whether key is higher than the random roll
			// Paired by spawn positions for Phobos, Deimos
			// Third value in list is hop-range for crow critters
			{ 0.2f, new Vector2[] { new(33.8f, 29.6f), new(35.2f, 29.6f) } },		// Shrine front
			{ 0.3f, new Vector2[] { new(30f, 29f), new(32f, 29.25f) } },			// Shrine left
			{ 0.4f, new Vector2[] { new(37f, 29.25f), new(39f, 29f) } },			// Shrine right
			{ 0.5f, new Vector2[] { new(45f, 27f), new(47f, 26.75f), new(2f) } },	// House
			{ 0.65f, new Vector2[] { new(31f, 37.2f), new(38f, 37.2f) } },			// Tourou
			{ 0.8f, new Vector2[] { new(33f, 45.5f), new(36f, 45.5f), new(2f) } },	// Torii
			{ 0.9f, new Vector2[] { new(21.5f, 17.5f), new(23.5f, 18.5f) } },		// Torii back
			{ 0.95f, new Vector2[] { new(25f, 40.3f), new(27.075f, 40.125f) } },	// Omiyageya
			{ 1f, new Vector2[] { new(50f, 40f), new(52f, 41f) } },					// Hall
		};
		// Values and things
		public const string CommandPrefix = "bb";
        public const int GenericID = 87008;
    }
}
