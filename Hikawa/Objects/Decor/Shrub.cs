using Hikawa.Data;
using Hikawa.Objects.Items;
using StardewValley.Delegates;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Xml.Serialization;

namespace Hikawa.Objects.Decor
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(Shrub)}")] // SpaceCore serialisation signature
	public class Shrub : LargeTerrainFeature
	{
		[XmlIgnore]
		public string Id
		{
			get => this._id;
			set
			{
				var id = this._id;
				this._id = value;
				if ((this._id != id || this.Data is null) && ModEntry.ShrubsData.Value.Shrubs.TryGetValue(value, out ShrubDataEntry data))
				{
					this.Data = data;

                    this.loadSprite();
                }
                else
                {
                    Log.E($"No data found for {nameof(Shrub)} type {value} at {this.Location?.NameOrUniqueName ?? "null"} {this.Tile}");
                }
			}
        }
        [XmlIgnore]
        public int GrowthStage
        {
            get => this._growthStage;
            set
            {
                var growthStage = this._growthStage;
                this._growthStage = Math.Clamp(value, 0, this.Data.MaxGrowthStage);

                if (this._growthStage != growthStage)
                {
                    this._growthDays = 0;
                    this._hits = 0;
                    this._fertilised = false;
                    this.loadSprite();
                }
            }
        }
        [XmlIgnore]
		public ShrubDataEntry Data;

        public bool PreventGrowth;
        public bool PreventInteractions;

        [XmlIgnore]
        private Texture2D _texture;
        [XmlIgnore]
        private ShrubAppearanceData _appearance;

        [XmlIgnore]
        private int _hits;
        [XmlIgnore]
        private float _shakeTimer;

        [XmlElement("id")]
        private string _id;
        [XmlElement("variant")]
        private string _variant;
        [XmlElement("growthDays")]
        private int _growthDays;
        [XmlElement("growthStage")]
        private int _growthStage;
        [XmlElement("flip")]
        private bool _flip;
        [XmlElement("fertilised")]
        private bool _fertilised;

		public Shrub()
			: base(needsTick: true)
		{
		}

		public Shrub(GameLocation location, Vector2 tile, string id, int growthStage = 0, string variant = null)
			: this()
		{
			this.Location = location;
			this.Tile = tile;
			this.Id = id;
			this.GrowthStage = growthStage;

			this._variant = variant;
        }

        public static ShrubDataEntry GetData(string itemId)
        {
            if (ItemRegistry.GetData(itemId) is ParsedItemData itemData && itemData.RawData is ShrubDataEntry shrubData)
            {
                return shrubData;
            }

            return null;
        }

        public static Shrub Get(GameLocation location, Vector2 tile)
        {
            if (location?.terrainFeatures.TryGetValue(tile, out TerrainFeature tf) == true && tf is Shrub shrub)
                return shrub;

            return null;
        }

        public static bool CanBePlacedHere(GameLocation location, Vector2 tile, out string error)
        {
            if (!location.IsOutdoors || location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Type", "Back") is not ("Grass" or "Dirt") || !location.CanItemBePlacedHere(tile, false, CollisionMask.All))
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

        public override bool tickUpdate(GameTime time)
        {
			if (this._shakeTimer > 0)
				this._shakeTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;

            return base.tickUpdate(time);
        }

        public override void dayUpdate()
        {
            base.dayUpdate();

            this._hits = 0;

            if (!this.PreventGrowth)
            {
                if (Shrub.CanBePlacedHere(this.Location, this.Tile, out _))
                {
                    ++this._growthDays;
                }
                else
                {
                    Game1.showRedMessage(ModEntry.I18n.Get("shrubs.error.growth", new { shrub = this.Data.DisplayName }));
                }
            }
        }

        public override bool seasonUpdate(bool onLoad)
        {
            if (!this.PreventGrowth && this._growthDays >= this.Data.GrowthDays)
                ++this.GrowthStage;

			this.loadSprite();

			return base.seasonUpdate(onLoad);
		}

		public void Shake(int ms)
        {
            this._shakeTimer = ms;
        }

		public bool Trim()
        {
			this.Shake(300);

            if (this._appearance is not null)
            {
                Game1.createRadialDebris(
                    Game1.currentLocation,
                    Debris.woodDebris,
                    (int)this.Tile.X,
                    (int)this.Tile.Y - 1,
                    Game1.random.Next(3, 6),
                    resource: false,
                    groundLevel: (int)this.Tile.Y,
                    color: Utility.StringToColor(this._appearance.DebrisColour));
            }

            int previous = this.GrowthStage;
            if (this.GrowthStage > 0 && ++this._hits >= 3)
                --this.GrowthStage;
            else if (this.GrowthStage == 0 && ++this._hits >= 6)
                return true;

            return previous != this.GrowthStage;
		}

        public bool Fertilise()
        {
            this.Shake(60);

            if (this.Data is not null && this.GrowthStage == this.Data.MaxGrowthStage)
            {
                Game1.showRedMessage(ModEntry.I18n.Get("shrubs.error.fertiliser.max"));
                this.Location.playSound("cancel");
                return false;
            }
            if (this._fertilised)
            {
                Game1.showRedMessage(ModEntry.I18n.Get("shrubs.error.fertiliser.once"));
                this.Location.playSound("cancel");
                return false;
            }

            this._fertilised = true;
            this._growthDays = this.Data.GrowthDays;

            this.Location.playSound("dirtyHit");

            return true;
        }

        public override void loadSprite()
        {
			if (this.Data is not null)
			{
				var context = new GameStateQueryContext(this.Location, null, null, null, null);
				foreach (var appearance in this.Data.Appearances)
				{
					if (appearance.GrowthStage == this.GrowthStage
                        && appearance.Variant == this._variant
                        && appearance.Season == this.Location.GetSeason()
                        && GameStateQuery.CheckConditions(appearance.Condition, context))
					{
						this._appearance = appearance;
                        this._texture = Game1.content.Load<Texture2D>(this.Data.TextureId);
                        this._flip = (this.Tile.X * 7 + this.Tile.Y * 11) % 3 == 0;
						return;
                    }
				}
			}
			Log.E($"no valid appearance found for shrub '{this.Id}' at {this.Location?.NameOrUniqueName} {this.Tile}");
        }

        public override bool performUseAction(Vector2 tileLocation)
        {
            this.Shake(60);

            return Game1.player.ActiveObject is null || !Game1.player.ActiveObject.canBePlacedHere(this.Location, tileLocation);
        }

        public override bool isActionable()
        {
            return true;
        }

        public override Rectangle getRenderBounds()
        {
            if (this._appearance is not null)
            {
                var bounds = this._appearance.DrawLayers[^1].TextureRegion;
                bounds.Offset(this.Tile * Game1.tileSize);
                return bounds;
            }
            return base.getRenderBounds();
        }

        public override bool performToolAction(Tool t, int damage, Vector2 tileLocation)
        {
            if (this.PreventInteractions)
                return false;

			var farmer = t.lastUser;

			if (t is Axe)
			{
				this.Location.playSound("axchop", tileLocation * Game1.tileSize);
				if (++this._hits > 3)
				{
					// destroy shrub
					Game1.createRadialDebris(
                        Game1.currentLocation,
                        Debris.woodDebris,
                        (int)this.Tile.X + Game1.random.Next(1 / 2 + 1), (int)this.Tile.Y + Game1.random.Next(1 / 2 + 1),
                        Game1.random.Next(3, 6),
                        resource: false);
                    return true;
				}
				else
                {
                    // damage shrub
					this.Location.debris.Add(new Debris(
                        Debris.woodDebris,
                        Game1.random.Next(1, 3),
                        t.getLastFarmerToUse().GetToolLocation() + new Vector2(16f, 0f),
                        t.getLastFarmerToUse().Position,
                        0,
                        Color.Khaki));
                    this.Shake(500);
                }
			}
			else if (t is Hoe)
            {
				// shrubs can be hoed at base growth stage to pick up
				if (this.GrowthStage == 0)
                {
                    this.Location.playSound("hoeHit", tileLocation * Game1.tileSize);

                    // safely remove shrub as object
                    Game1.createObjectDebris(this.Id, (int)this.Tile.X, (int)this.Tile.Y, this.Location);
                    return true;
                }
				else
				{
					// no effect on large shrubs
                    this.Location.playSound("dirtyHit", tileLocation * Game1.tileSize);
					this.Shake(500);
                    Game1.player.jitterStrength = 1f;
                    return false;
                }
            }
			else if (t is ShrubTool)
            {
                farmer.Stamina -= 1;

                if (this.Trim())
                {
                    if (this._hits >= 6)
                    {
                        // destroy shrub
                        Game1.createRadialDebris(
                            Game1.currentLocation,
                            Debris.woodDebris,
                            (int)this.Tile.X + Game1.random.Next(1 / 2 + 1), (int)this.Tile.Y + Game1.random.Next(1 / 2 + 1),
                            Game1.random.Next(3, 6),
                            resource: false);
                        return true;
                    }
                }
            }
            return false;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            if (this.isTemporarilyInvisible)
                return;

            float layerDepth = (this.getBoundingBox().Center.Y + this.Tile.X / 900f + 0.01f) / 10000f;
            float scale = Game1.pixelZoom;
			var position = (this.Tile + new Vector2(0.5f, 1f)) * Game1.tileSize;
			var shakeOffset = this._shakeTimer > 0 ? new Vector2(-2 + 4 * Game1.random.NextSingle(), -2 + 4 * Game1.random.NextSingle()) : Vector2.Zero;

			this.DrawShrub(spriteBatch, Game1.GlobalToLocal(Game1.viewport, position), shakeOffset, scale, layerDepth);
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 positionOnScreen, Vector2 tileLocation, float scale, float layerDepth)
        {
            this.DrawShrub(spriteBatch, positionOnScreen, Vector2.Zero, scale, layerDepth);
        }

        public void DrawShrub(SpriteBatch b, Vector2 position, Vector2 shakeOffset, float scale, float layerDepth)
        {
            if (this.Data is null || this._texture is null || this._appearance is null || this._appearance.DrawLayers is null)
            {
                var bounds = this.getRenderBounds();
                Utility.DrawErrorTexture(b, new Rectangle((int)position.X - bounds.Width / 2, (int)position.Y - bounds.Height, bounds.Width, bounds.Height), layerDepth);
            }
            else
            {
                foreach (var layer in this._appearance.DrawLayers)
                {
                    var source = layer.TextureRegion;
                    var origin = layer.TextureOrigin;
                    var drawPosition = layer.TextureOffset * Game1.pixelZoom + position;
                    if (layer.Shake && shakeOffset != Vector2.Zero)
                        drawPosition += shakeOffset;
                    b.Draw(this._texture, drawPosition, source, Color.White, 0, origin, scale, this._flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, layerDepth += .000001f);
                }
            }
        }
    }
}
