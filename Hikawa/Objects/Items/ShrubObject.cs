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
            : this()
        {
            this.ItemId = id;

            var data = ItemRegistry.GetDataOrErrorItem(this.TypeDefinitionId + this.ItemId);

            this.Name = data.InternalName;
            this.Stack = stack;
        }

        protected override Item GetOneNew()
        {
            return new ShrubObject(this.ItemId, 1);
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
            if (!Shrub.CanBePlacedHere(l, tile, out string error))
            {
                if (showError && error is not null)
                    Game1.showRedMessage(error);
                return false;
            }
            return true;
        }

        public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
        {
            var tile = Vector2.Floor(new Vector2(x, y) / Game1.tileSize);
            location.terrainFeatures[tile] = new Shrub(location, tile, this.ItemId);
            return true;
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (Shrub.GetData(this.ItemId) is ShrubDataEntry shrubData)
            {
                scaleSize = System.Math.Clamp((new Vector2(Object.spriteSheetTileSize) / shrubData.IconTextureRegion.Size.ToVector2()).Length() / 2, 1f / Game1.pixelZoom, 1f);
            }
            base.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }

        public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
        {
            if (Shrub.GetData(this.ItemId) is ShrubDataEntry shrubData)
            {
                objectPosition += -shrubData.IconTextureRegion.Size.ToVector2() / 2 * Game1.pixelZoom
                    + new Vector2(1, 0) / 2 * Game1.tileSize;
            }
            base.drawWhenHeld(spriteBatch, objectPosition, f);
        }

        public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
        {
            //base.draw(spriteBatch, x, y, alpha);
        }

        public override void DrawShadow(SpriteBatch spriteBatch, Vector2 position, Color color, float layerDepth)
        {
            //base.DrawShadow(spriteBatch, position, color, layerDepth);
        }
    }
}
