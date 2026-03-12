using Hikawa.Data;
using Hikawa.Objects.Decor;
using System.Xml.Serialization;

namespace Hikawa.Objects.Items
{
    [XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(ShrubObject)}")] // SpaceCore serialisation signature
    public class ShrubObject : Object
    {
        public override string TypeDefinitionId => ModEntry.ShrubsData.Value.ShrubObjectData.Identifier;

        public ShrubObject()
            : base()
        {
        }

        public ShrubObject(string id, int stack)
            : base(id, stack)
        {
        }

        public static bool IsTilePlaceableForShrub(GameLocation location, Vector2 tile, out string error)
        {
            if (!location.CanItemBePlacedHere(tile, false, CollisionMask.None))
            {
                error = ModEntry.I18n.Get("shrubs.error.placement");
                return false;
            }
            foreach (var other in Utility.getSurroundingTileLocationsArray(tile))
            {
                if (location.isTerrainFeatureAt((int)other.X, (int)other.Y))
                {
                    error = ModEntry.I18n.Get("shrubs.error.close");
                    return false;
                }
            }
            error = null;
            return true;
        }

        public override string getCategoryName()
        {
            return ModEntry.I18n.Get("item.category.shrub");
        }

        public override Color getCategoryColor()
        {
            return Color.Olive;
        }

        public override void drawPlacementBounds(SpriteBatch spriteBatch, GameLocation location)
        {
            base.drawPlacementBounds(spriteBatch, location);
        }

        public override bool canBePlacedHere(GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false)
        {
            return ShrubObject.IsTilePlaceableForShrub(l, tile, out _);
        }

        public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
        {
            var tile = Vector2.Floor(new Vector2(x, y) / Game1.tileSize);
            if (!ShrubObject.IsTilePlaceableForShrub(location, tile, out string error))
            {
                if (error is not null)
                    Game1.showRedMessage(error);
                return false;
            }

            location.terrainFeatures[tile] = new Shrub(location, tile, this.ItemId);
            return true;
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (Shrub.GetData(this.ItemId) is ShrubDataEntry shrubData)
            {
                scaleSize = (new Vector2(Object.spriteSheetTileSize) / shrubData.IconTextureRegion.Size.ToVector2()).Length();
            }
            base.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }

        public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
        {
            if (Shrub.GetData(this.ItemId) is ShrubDataEntry shrubData)
            {
                objectPosition += -shrubData.IconTextureRegion.Size.ToVector2() / 2 * Game1.pixelZoom
                    + new Vector2(1, -1) / 2 * Game1.tileSize;
            }
            base.drawWhenHeld(spriteBatch, objectPosition, f);
        }

        public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
        {
            //base.draw(spriteBatch, x, y, alpha);
        }

        public override void DrawShadow(SpriteBatch spriteBatch, Vector2 position, Color color, float layerDepth)
        {
            base.DrawShadow(spriteBatch, position, color, layerDepth);
        }
    }
}
