using System.Collections.Generic;
using Hikawa.Objects.Locations;
using StardewValley.GameData;

namespace Hikawa
{
	public record class ModData
	{
		// Tokens
		// ids
		public string ContentPrefix;
		public string ConsoleCommandPrefix;
		public string ModDataKey;
		public string SaveDataKey;
		// characters
		public string NpcRei;
		public string NpcAmi;
		public string NpcUsagi;
		public string NpcMako;
		public string NpcMina;
		public string NpcGramps;
		public string NpcGuy;
		public string NpcCat;
		public string NpcVolleyballSuffix;
		// locations
		public string MapShrine;
		public string MapHouse;
		public string MapHall;
		public string MapVortex;
		public string MapRoof;
		public string MapVolleyball;
		// tile sheets
		public string TilesheetOutdoors;
		public string TilesheetHouse;
		// tile actions
		public string ActionShrineShop;
		public string ActionShrineHall;
		public string ActionShrineOffering;
		public string ActionBackDoor;
		public string ActionLockbox;
		public string ActionWardrobe;
		public string ActionEma;
		public string ActionVortex;
		public string ActionCrowTrade;
		// touch actions
		public string TouchActionHop;
		// trigger actions
		public string TriggerDialogueEffects;
		// event commands
		public string EventCommandCrystalBall;
		// items
		public string ItemWand;
		public string ItemMirror;
		public string ItemTotem;
		public string ItemVolleyball;
		public string ItemLostGlasses;
		public string ItemLostJewelry;
		// shops
		public string ShopShrineRei;
		// other
		public string HearthLightBaseId;
		public Point HearthLightSize;

		// Data
		public Vector2 HouseChimneyTile;
		public Vector2 CrowSpawnRadius;
		public Rectangle CrowSpawnArea;
		/// <summary>
		/// Keyed by chance to appear, chance is measured by whether key is higher than the random roll
		/// </summary>
		public Dictionary<float, CrowSpawnEntry> CrowPerches;
		public Point[] BabyCrowPerches;
		public Point[] BabyCrowRoosts;
		public Dictionary<string, ShrineTreeDefinitionsEntry> ShrineTreeDefinitions;
		public Dictionary<string, List<ShrineTreesEntry>> ShrineTrees;
		public Dictionary<string, List<HangingSpriteEntry>> HangingSprites;
		public Dictionary<string, List<LightTileEntry>> LightTiles;
		public Dictionary<string, List<LightEntry>> Lights;
		public GenericSpawnItemDataWithCondition[] CrowTradeRules;
	}
}
