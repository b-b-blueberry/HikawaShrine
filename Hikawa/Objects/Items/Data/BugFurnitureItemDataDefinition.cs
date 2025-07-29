using Hikawa.Data;
using StardewValley.ItemTypeDefinitions;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Objects.Items.Data
{
	public class BugFurnitureItemDataDefinition : BaseItemDataDefinition
	{
		public static string TypeDefinitionId => ModEntry.BugsData.Value.BugFurnitureData.Identifier;
		public override string Identifier => TypeDefinitionId;

		public override Item CreateItem(ParsedItemData data)
		{
			return new BugFurniture(data);
		}

		public override bool Exists(string itemId)
		{
			return ModEntry.BugsData.Value.BugFurniture.Any(localId => itemId == $"{ModEntry.ModData.ItemBugFurniture}_{localId}");
		}

		public override IEnumerable<string> GetAllIds()
		{
			return ModEntry.BugsData.Value.BugFurniture.Keys.Select(localId => $"{ModEntry.ModData.ItemBugFurniture}_{localId}");
		}

		public override ParsedItemData GetData(string itemId)
		{
			if (ModEntry.BugsData.Value.BugFurniture.Keys.FirstOrDefault(localId => itemId == $"{ModEntry.ModData.ItemBugFurniture}_{localId}") is string localId)
			{
				BugFurnitureDataEntry d = ModEntry.BugsData.Value.BugFurniture[localId];
				var generic = ModEntry.BugsData.Value.BugFurnitureData;
				return new ParsedItemData(
					itemType: this,
					itemId: itemId,
					spriteIndex: d.TileIndex,
					textureName: d.TextureId,
					internalName: itemId,
					displayName: d.DisplayName,
					description: d.Description,
					category: generic.Category,
					objectType: null,
					rawData: d,
					isErrorItem: false,
					excludeFromRandomSale: true);
			}
			return null;
		}

		public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
		{
			if (ModEntry.BugsData.Value.BugFurniture.TryGetValue(data.ItemId, out BugFurnitureDataEntry d))
			{
				var generic = ModEntry.BugsData.Value.BugFurnitureData;
				int size = Game1.smallestTileSize;
				Rectangle r = Game1.getSourceRectForStandardTileSheet(
					tileSheet: texture,
					tilePosition: spriteIndex,
					width: size,
					height: size);
				r.Width = d.TileSize.X * size;
				r.Height = d.TileSize.Y * size;
				return r;
			}
			return default;
		}
	}
}
