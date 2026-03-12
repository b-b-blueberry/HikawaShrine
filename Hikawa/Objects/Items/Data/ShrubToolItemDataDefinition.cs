using Hikawa.Data;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;
using System.Collections.Generic;

namespace Hikawa.Objects.Items.Data;

public class ShrubToolItemDataDefinition : BaseItemDataDefinition
{
    // the developers forced me to do this i absolutely hate

    public static string TypeDefinitionId => ModEntry.ShrubsData.Value.ShrubToolData.Identifier;
    public override string Identifier => TypeDefinitionId;

    public override Item CreateItem(ParsedItemData data)
    {
        return new ShrubTool(data.ItemId);
    }

    public override bool Exists(string itemId)
    {
        return ModEntry.ShrubsData.Value.ShrubToolData.ItemId == itemId;
    }

    public override IEnumerable<string> GetAllIds()
    {
        yield return ModEntry.ShrubsData.Value.ShrubToolData.ItemId;
    }

    public override ParsedItemData GetData(string itemId)
    {
        ShrubToolData generic = ModEntry.ShrubsData.Value.ShrubToolData;
        return new ParsedItemData(
            itemType: this,
            itemId: itemId,
            spriteIndex: 0,
            textureName: generic.TextureId,
            internalName: itemId,
            displayName: TokenParser.ParseText(generic.DisplayName),
            description: TokenParser.ParseText(generic.Description),
            category: generic.Category,
            objectType: generic.Type,
            rawData: generic,
            isErrorItem: false,
            excludeFromRandomSale: true);
    }

    public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
    {
        if (data.RawData is ShrubToolData shrubToolData)
            return shrubToolData.IconTextureRegion;

        return Rectangle.Empty;
    }
}
