using Hikawa.Data;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;
using System.Collections.Generic;

namespace Hikawa.Objects.Items.Data;

public class BowItemDataDefinition : BaseItemDataDefinition
{
    public static string TypeDefinitionId => ModEntry.BowsData.Value.Identifier;
    public override string Identifier => TypeDefinitionId;

    public override Item CreateItem(ParsedItemData data)
    {
        return new Bow(data.ItemId);
    }

    public override bool Exists(string itemId)
    {
        return ModEntry.BowsData.Value.Bows.ContainsKey(itemId);
    }

    public override IEnumerable<string> GetAllIds()
    {
        return ModEntry.BowsData.Value.Bows.Keys;
    }

    public override ParsedItemData GetData(string itemId)
    {
        BowsDataAsset generic = ModEntry.BowsData.Value;
        if (ModEntry.BowsData.Value.Bows.TryGetValue(itemId, out BowsDataEntry bowData))
        {
            return new ParsedItemData(
                itemType: this,
                itemId: itemId,
                spriteIndex: 0,
                textureName: bowData.Texture,
                internalName: itemId,
                displayName: TokenParser.ParseText(bowData.DisplayName),
                description: TokenParser.ParseText(bowData.Description),
                category: generic.Category,
                objectType: generic.Type,
                rawData: bowData,
                isErrorItem: false,
                excludeFromRandomSale: true);
        }
        return null;
    }

    public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
    {
        if (data.RawData is BowsDataEntry bowData)
            return bowData.SourceRect;

        return Rectangle.Empty;
    }
}
