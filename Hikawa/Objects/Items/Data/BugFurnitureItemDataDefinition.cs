using Hikawa.Data;
using StardewValley.ItemTypeDefinitions;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Objects.Items.Data;

public class BugFurnitureItemDataDefinition : BaseItemDataDefinition
{
	public static string TypeDefinitionId => ModEntry.BugsData.Value.BugFurnitureData.Identifier;
	public override string Identifier => TypeDefinitionId;

    public static string UnqualifiedGlobalToLocalId(string itemId)
    {
        return ModEntry.BugsData.Value.BugFurniture.Keys.FirstOrDefault(localId => itemId == $"{ModEntry.ModData.ItemBugFurniture}_{localId}");
    }

    public override Item CreateItem(ParsedItemData data)
	{
		return new BugFurniture(data);
	}

	public override bool Exists(string itemId)
	{
		return BugFurnitureItemDataDefinition.UnqualifiedGlobalToLocalId(itemId) is not null;
	}

	public override IEnumerable<string> GetAllIds()
	{
		return ModEntry.BugsData.Value.BugFurniture.Keys.Select(localId => $"{ModEntry.ModData.ItemBugFurniture}_{localId}");
	}

	public override ParsedItemData GetData(string itemId)
	{
		if (BugFurnitureItemDataDefinition.UnqualifiedGlobalToLocalId(itemId) is string localId)
		{
			BugFurnitureDataEntry d = ModEntry.BugsData.Value.BugFurniture[localId];
            BugFurnitureData generic = ModEntry.BugsData.Value.BugFurnitureData;
			return new ParsedItemData(
				itemType: this,
				itemId: itemId,
				spriteIndex: d.SpriteIndex,
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
        if (BugFurnitureItemDataDefinition.UnqualifiedGlobalToLocalId(data.ItemId) is string localId)
        {
            BugFurnitureDataEntry d = ModEntry.BugsData.Value.BugFurniture[localId];
            BugFurnitureData generic = ModEntry.BugsData.Value.BugFurnitureData;
			int size = Game1.smallestTileSize;
			Rectangle r = Game1.getSourceRectForStandardTileSheet(
				tileSheet: texture,
				tilePosition: spriteIndex,
				width: size,
				height: size);
			r.Width = d.SpriteSize.X * size;
			r.Height = d.SpriteSize.Y * size;
			return r;
		}
		return default;
	}
}
