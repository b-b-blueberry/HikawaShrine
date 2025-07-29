using Hikawa.Data;
using Hikawa.Objects.Items.Data;
using Hikawa.Objects.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Hikawa.Objects.Items;

[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(Kite)}")] // SpaceCore serialisation signature
public class Kite : StardewValley.Object
{
    /// <summary>
    /// this one's for the long name kite class kites as instances
    /// </summary>
    public class KiteButItsNotTheDataClassesOrTheInstanceOrEvenTheOtherKiteClassWithALongName
    {
        public float Drift;
        public float Altitude;
        public Vector2 Position;
        /// <summary>
        /// jesus christ
        /// </summary>
        public KiteDataOnTheKiteItsASmallerKiteNotTheMainDataOrTheDataEntryClassesThisIsDifferent Data;
    }

    public override string TypeDefinitionId => KiteItemDataDefinition.TypeDefinitionId;

    public readonly KiteDataEntry KiteData;
    public readonly List<KiteButItsNotTheDataClassesOrTheInstanceOrEvenTheOtherKiteClassWithALongName> Kites;

    public Vector2 StakePosition;

    protected float Rando => this.TileLocation.X + this.TileLocation.Y % 50;

    public Kite() : this(itemId: null, tile: Vector2.Zero) { }

    public Kite(string itemId, Vector2 tile)
    {
        this.initNetFields();

        KitesDataAsset generic = ModEntry.KitesData.Value;

        // Object
        itemId = this.ValidateUnqualifiedItemId(itemId);
        this.ItemId = itemId;
        this.Name = itemId;
        this.TileLocation = tile;

        this.IsRecipe = false;
        this.bigCraftable.Value = false;
        this.CanBeSetDown = true;
        this.setOutdoors.Value = true;
        this.setIndoors.Value = false;
        this.Type = generic.Type;
        this.Category = generic.Category;
        this.Fragility = generic.Fragility;
        this.Edibility = StardewValley.Object.inedible;

        // Kite
        if (Kite.GetData(itemId) is KiteDataEntry kiteData)
        {
            this.KiteData = kiteData;
            this.Kites = kiteData.Kites.Select(data => new KiteButItsNotTheDataClassesOrTheInstanceOrEvenTheOtherKiteClassWithALongName() { Data = data }).ToList();
        }
    }

    public static KiteDataEntry GetData(string itemId)
    {
        foreach ((string localId, KiteDataEntry kiteData) in ModEntry.KitesData.Value.Kites)
        {
            if (itemId == $"{ModEntry.KitesData.Value.ItemId}_{localId}")
                return kiteData;
        }
        return null;
    }

    protected override Item GetOneNew()
    {
        return new Kite(itemId: this.ItemId, tile: this.TileLocation);
    }

    protected override void GetOneCopyFrom(Item source)
    {
        base.GetOneCopyFrom(source);

        if (source is Kite kite)
        {
            //
        }
    }

    public override bool isPlaceable()
    {
        return base.isPlaceable();
    }

    public override bool isPassable()
    {
        if (Kite.GetData(this.ItemId) is KiteDataEntry kiteData)
        {
            return kiteData.Passable;
        }
        return false;
    }

    public override bool canBePlacedHere(GameLocation l, Vector2 tile, CollisionMask collisionMask = CollisionMask.All, bool showError = false)
    {
        return base.canBePlacedHere(l, tile, collisionMask, showError);
    }

    public override int maximumStackSize()
    {
        //return base.maximumStackSize();
        return 1;
    }

    public override Color getCategoryColor()
    {
        return base.getCategoryColor();
        //return new Color(100, 25, 190);
    }

    public override string getCategoryName()
    {
        return base.getCategoryName();
        //return Game1.content.LoadString("Strings/StringsFromCSFiles:Object.cs.12859");
    }

    public override void updateWhenCurrentLocation(GameTime time)
    {
        base.updateWhenCurrentLocation(time);

        if (this.KiteData is KiteDataEntry kiteData && this.Kites is not null)
        {
            for (int i = 0; i < this.Kites.Count; ++i)
            {
                var kite = this.Kites[i];
                float rando = this.Rando + i / 5f;
                float dt = (float)time.ElapsedGameTime.TotalMilliseconds;
                float scale = 1f + rando / 500f;
                {
                    // wind drift
                    float wind = dt * scale * -Game1.windGust / 30000f; // Current scaled wind force
                    float resistance = dt * scale * 0.00003f;
                    kite.Drift = Math.Clamp(kite.Drift + wind - resistance, 0, 1);
                    // gravity/buoyancy
                    kite.Altitude = Math.Clamp(kite.Altitude + kite.Data.Gravity / dt, -kite.Data.Length, 0);
                }
            }
        }
    }

    public override void resetState()
    {
        base.resetState();
    }

    public override void draw(SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha = 1)
    {
        base.draw(spriteBatch, xNonTile, yNonTile, layerDepth, alpha);
    }

    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        base.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
    }

    public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
    {
        base.drawWhenHeld(spriteBatch, objectPosition, f);
    }

    public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
    {
        //base.draw(spriteBatch, x, y, alpha);

        if (this.KiteData is KiteDataEntry kiteData)
        {
            this.StakePosition = new Vector2(x + 0.5f, y + 0.5f) * Game1.tileSize;

            float layerDepth = this.GetBoundingBoxAt(x, y).Center.Y / 10000f;

            // stake on the ground
            Vector2 stakeOrigin = kiteData.StakeOrigin;
            Vector2 screenStakePosition = Game1.GlobalToLocal(Game1.viewport, this.StakePosition);

            // sprite
            spriteBatch.Draw(
                texture: Game1.content.Load<Texture2D>(kiteData.StakeTexture),
                position: screenStakePosition,
                sourceRectangle: kiteData.StakeSource,
                color: Color.White * alpha,
                rotation: 0,
                origin: stakeOrigin,
                scale: Game1.pixelZoom,
                effects: SpriteEffects.None,
                layerDepth: layerDepth);
            // shadow
            spriteBatch.Draw(
                texture: Game1.shadowTexture,
                position: screenStakePosition + new Vector2(0, 2) * Game1.pixelZoom,
                sourceRectangle: Game1.shadowTexture.Bounds,
                color: Color.White * alpha,
                rotation: 0,
                origin: Game1.shadowTexture.Bounds.Size.ToVector2() / 2,
                scale: Game1.pixelZoom / 2,
                effects: SpriteEffects.None,
                layerDepth: layerDepth - 0.000001f);

            foreach (var kite in this.Kites)
            {
                if (alpha == 1)
                {
                    Vector2 screenKitePosition = Game1.GlobalToLocal(Game1.viewport, kite.Position);

                    // string
                    Utility.drawLineWithScreenCoordinates((int)screenStakePosition.X, (int)screenStakePosition.Y - kite.Data.Height * Game1.pixelZoom, (int)screenKitePosition.X, (int)screenKitePosition.Y, spriteBatch, kite.Data.StringColor, layerDepth - 0.00001f, thickness: Game1.pixelZoom / 2);
                }
            }
        }
    }

    public override void drawAboveFrontLayer(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
    {
        //base.drawAboveFrontLayer(spriteBatch, x, y, alpha);

        // kite in the sky
        if (this.KiteData is KiteDataEntry kiteData && this.Kites is not null)
        {
            for (int i = 0; i < this.Kites.Count; ++i)
            {
                var kite = this.Kites[i];
                float rando = this.Rando + i / 5f;

                Vector2 kitePosition = this.StakePosition
                    + new Vector2(0, -kite.Data.Height) * Game1.pixelZoom;

                Vector2 kiteOrigin = kite.Data.KiteOrigin;
                float layerDepth = this.GetBoundingBoxAt(x, y).Center.Y / 10000f;
                float bounce = 1f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 750d + rando * 1000f);
                float scale = 1f + rando / 500f;
                Vector2 kiteOffset = Vector2.Zero;
                {
                    // final offsets
                    float height = 1f * kite.Altitude;
                    float wind = kite.Data.Wind * kite.Drift;
                    kiteOffset = kiteOffset
                        + new Vector2(height / 8, height / 8 * 7)
                        + new Vector2(-bounce, bounce)
                        + new Vector2(-wind, wind);

                    // flip x and y for horizontal kites (banners, streamers)
                    if (!kite.Data.Vertical)
                    {
                        float temp = kiteOffset.X;
                        kiteOffset.X = kiteOffset.Y;
                        kiteOffset.Y = 0;
                    }
                }
                kite.Position = kitePosition
                    + kiteOffset * scale * Game1.pixelZoom;

                // kite
                float rotation = bounce / MathF.PI / 5;
                spriteBatch.Draw(
                    Game1.content.Load<Texture2D>(kite.Data.KiteTexture),
                    position: Game1.GlobalToLocal(Game1.viewport, kite.Position),
                    sourceRectangle: kite.Data.KiteSource,
                    color: Color.White * alpha,
                    rotation: rotation,
                    origin: kiteOrigin,
                    scale: (Vector2.One + new Vector2(rotation) * kite.Data.Scaling) * Game1.pixelZoom,
                    effects: SpriteEffects.None,
                    layerDepth: layerDepth);
            }
        }
    }
}
