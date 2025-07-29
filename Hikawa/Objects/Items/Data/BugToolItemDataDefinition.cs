using Hikawa.Data;
using StardewValley.ItemTypeDefinitions;
using System.Collections.Generic;

namespace Hikawa.Objects.Items.Data;

public class BugToolItemDataDefinition : BaseItemDataDefinition
{
	public static string TypeDefinitionId => ModEntry.BugsData.Value.BugToolData.Identifier;
        public override string Identifier => TypeDefinitionId;
        public static string ItemName => ModEntry.BugsData.Value.BugToolData.Name;

	public override Item CreateItem(ParsedItemData data)
	{
		return new BugTool();
	}

	public override bool Exists(string itemId)
	{
		return itemId == ItemName;
	}

	public override IEnumerable<string> GetAllIds()
	{
		return [ItemName];
	}

	public override ParsedItemData GetData(string itemId)
	{
		BugToolData d = ModEntry.BugsData.Value.BugToolData;
		return new ParsedItemData(
			itemType: this,
			itemId: itemId,
			spriteIndex: d.SpriteIndex,
			textureName: d.TextureName,
			internalName: d.Name,
			displayName: d.DisplayName,
			description: d.Description,
			category: d.Category,
			objectType: null,
			rawData: null,
			isErrorItem: false,
			excludeFromRandomSale: true);
	}

	public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
	{
		BugToolData d = ModEntry.BugsData.Value.BugToolData;
		Rectangle r = Utility.getSourceRectWithinRectangularRegion(
			regionX: 0,
			regionY: 0,
			regionWidth: texture.Width,
			sourceIndex: d.SpriteIndex,
			sourceWidth: d.SpriteSize.X,
			sourceHeight: d.SpriteSize.Y);
		return r;
	}
}
