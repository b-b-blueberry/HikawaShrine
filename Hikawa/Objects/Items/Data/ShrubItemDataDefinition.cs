using Hikawa.Data;
using StardewValley.ItemTypeDefinitions;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Objects.Items.Data;

public class ShrubItemDataDefinition : BaseItemDataDefinition
{
    // this one isnt so bad but i still hate

    public static string TypeDefinitionId => ModEntry.ShrubsData.Value.ShrubObjectData.Identifier;
    public override string Identifier => TypeDefinitionId;

    public override Item CreateItem(ParsedItemData data)
    {
        return new ShrubObject(data.ItemId, 1);
    }

    public override bool Exists(string itemId)
    {
        return ModEntry.ShrubsData.Value.Shrubs.ContainsKey(itemId);
    }

    public override IEnumerable<string> GetAllIds()
    {
        return ModEntry.ShrubsData.Value.Shrubs.Keys;
    }

    public override ParsedItemData GetData(string itemId)
    {
        if (ModEntry.ShrubsData.Value.Shrubs.TryGetValue(itemId, out ShrubDataEntry shrubData))
        {
            ParsedItemData data = new ParsedItemData(
                itemType: this,
                itemId: itemId,
                spriteIndex: -1,
                textureName: shrubData.TextureId,
                internalName: itemId,
                displayName: shrubData.DisplayName,
                description: shrubData.Description,
                category: 0,
                objectType: null,
                rawData: shrubData,
                isErrorItem: false,
                excludeFromRandomSale: true);
            return data;
        }
        return null;
    }

    public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
    {
        if (data.RawData is ShrubDataEntry shrubData)
        {
            return shrubData.IconTextureRegion;
        }
        return Rectangle.Empty;
    }

    public override IEnumerable<string> GetContextTags(ParsedItemData itemData)
    {
        return base.GetContextTags(itemData)
            .Concat(ModEntry.ShrubsData.Value.ShrubObjectData.ContextTags);
    }
}
