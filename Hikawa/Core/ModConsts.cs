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
		public const string SpaceCoreXmlPrefix = "Mods_Blueberry_Hikawa_";

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
        public const string ActionCrowTrade = ContentPrefix + "CrowTrade";
		// Touch actions
		public const string TouchHop = ContentPrefix + "Hop";
		// Trigger actions
		public const string TriggerDialogueEffects = "DialogueEffects";
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
		// Values and things
		public const string CommandPrefix = "bb";
        public const int GenericID = 87008;
    }
}
