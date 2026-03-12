using Hikawa.Data;
using Hikawa.Objects.Decor;
using StardewValley.ItemTypeDefinitions;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Objects.Items.Data;

public class ShrubObjectDataDefinition : BaseItemDataDefinition
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
        return Shrub.GetData(itemId) is not null;
    }

    public override IEnumerable<string> GetAllIds()
    {
        return ModEntry.ShrubsData.Value.Shrubs.Keys;
    }

    public override ParsedItemData GetData(string itemId)
    {
        if (ModEntry.ShrubsData.Value.Shrubs.TryGetValue(itemId, out ShrubDataEntry shrubData))
        {
            ShrubsDataAsset generic = ModEntry.ShrubsData.Value;
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
