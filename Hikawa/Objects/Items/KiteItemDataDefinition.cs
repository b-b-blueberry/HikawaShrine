using Hikawa.Objects.Locations;
using StardewValley.ItemTypeDefinitions;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Objects.Items
{
    public class KiteItemDataDefinition : BaseItemDataDefinition
    {
        // I REALLY HATE ITEM DATA DEFINITIONS

        // I REALLY REALLY HATE THEM

        public static string TypeDefinitionId => ModEntry.KiteData.Value.Identifier;
        public override string Identifier => TypeDefinitionId;

        public override Item CreateItem(ParsedItemData data)
        {
            return new Kite(data.ItemId, Vector2.Zero);
        }

        public override bool Exists(string itemId)
        {
            return Kite.GetData(itemId) is not null;
        }

        public override IEnumerable<string> GetAllIds()
        {
            return ModEntry.KiteData.Value.Kites.Keys.Select(localId => $"{ModEntry.KiteData.Value.ItemId}_{localId}");
        }

        public override ParsedItemData GetData(string itemId)
        {
            if (Kite.GetData(itemId) is KiteDataEntry kiteData)
            {
                KiteData generic = ModEntry.KiteData.Value;
                ParsedItemData data = new ParsedItemData(
                    itemType: this,
                    itemId: itemId,
                    spriteIndex: kiteData.SpriteIndex,
                    textureName: kiteData.Texture,
                    internalName: itemId,
                    displayName: kiteData.DisplayName,
                    description: kiteData.Description,
                    category: generic.Category,
                    objectType: generic.Type,
                    rawData: kiteData,
                    isErrorItem: false,
                    excludeFromRandomSale: true);
                return data;
            }
            return null;
        }

        public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
        {
            return Game1.getSourceRectForStandardTileSheet(texture, data.SpriteIndex, 16, 16);
        }
    }
}
