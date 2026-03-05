using Hikawa.Objects.Decor;
using System;
using System.Xml.Serialization;
using xTile;

namespace Hikawa.Objects.Locations
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(House)}")] // SpaceCore serialisation signature
	public class House : GameLocation
    {
        [XmlIgnore]
        public HearthLight HearthLight;
        [XmlIgnore]
        public bool DoorsOpen;

        public House() : base() {}

		public House(string filename, string locationName) : base(filename, locationName) {}

		public static GameLocation Get()
		{
			return Game1.getLocationFromName(ModEntry.ModData.MapHouse);
		}

		public override void UpdateWhenCurrentLocation(GameTime time)
		{
			base.UpdateWhenCurrentLocation(time);

			if (this.HearthLight is not null)
			{
				// smoke
				if (time.TotalGameTime.Ticks % 66 == 0)
				{
					var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
						textureName: "LooseSprites/Cursors",
						sourceRect: new Rectangle(372, 1956, 10, 10),
						position: this.HearthLight.position.Value
							+ new Vector2(-42),
						flipped: false,
						alphaFade: 0.002f,
						color: new Color(r: 165, g: 110, b: 110));
					sprite.alpha = 0.6f;
					sprite.motion = new Vector2(0f, -0.5f);
					sprite.acceleration = new Vector2(0.0015f, 0f);
					sprite.interval = 99999f;
					sprite.layerDepth = 1f;
					sprite.scale = Game1.pixelZoom / 2;
					sprite.scaleChange = 0.02f;
					sprite.rotationChange = Game1.random.Next(-2, 3) * (float)Math.PI / 256f;
					this.TemporarySprites.Add(sprite);
				}
				// embers
				if (time.TotalGameTime.Ticks % 66 == 0)
				{
					Color[] colours = [Color.White, Color.OrangeRed, Color.Gold, Color.RosyBrown];
					var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
						textureName: null,
						sourceRect: new Rectangle(0, 0, 1, 1),
						position: this.HearthLight.position.Value
							+ new Vector2(x: -Game1.tileSize, y: 0f) / 2f
							+ new Vector2(x: (float)Game1.random.NextDouble() * Game1.tileSize, y: (float)Game1.random.NextDouble() * Game1.tileSize / 4f),
						flipped: false,
						alphaFade: 0.0025f,
						color: colours[Game1.random.Next(colours.Length)]);
					sprite.alpha = 0.4f + (float)Game1.random.NextDouble() * 0.4f;
					sprite.motion = new Vector2(0f, -0.25f);
					sprite.acceleration = new Vector2(0.0015f, 0f);
					sprite.interval = 99999f;
					sprite.xPeriodic = true;
					sprite.xPeriodicLoopTime = 1500f;
					sprite.xPeriodicRange = Game1.tileSize / 8 + (float)(Game1.random.NextDouble() * Game1.tileSize / 8);
					sprite.layerDepth = 1f;
					sprite.scale = Game1.pixelZoom;
					sprite.scaleChange = -0.02f;
					sprite.texture = Game1.staminaRect;
					this.TemporarySprites.Add(sprite);
				}
			}
		}

		protected override void resetLocalState()
		{
			base.resetLocalState();
		}

		protected override void resetSharedState()
		{
			base.resetSharedState();

			var shrine = Shrine.Get();

			Utils.ApplyCustomSharedMapProperties(this);

			if (this.sharedLights.TryGetValue(HearthLight.GetId(where: this, which: 0), out LightSource light))
				this.HearthLight = light as HearthLight;

            // engawa.
			// not cold. windy and rainy is ok and nice. no freak weather
            this.DoorsOpen = Game1.season is not Season.Winter && !shrine.IsGreenRainingHere();
		}

		public override void cleanupBeforePlayerExit()
		{
			Utils.ResetCustomSharedMapProperties(this);

			base.cleanupBeforePlayerExit();
		}

        public override void drawFloorDecorations(SpriteBatch b)
        {
            base.drawFloorDecorations(b);

			// engawa
			// lives in this method because draw() places it above the front layer (occludes houseplants etc)
            // this would be unreasonably convoluted in content patcher
			{
                var texture = Game1.content.Load<Texture2D>(AssetManager.HouseSpritesAssetName);
                var tile = new Vector2(7, 2);
                var position = Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize);
                var source = new Rectangle(112, 400, 96, 48);

                if (this.DoorsOpen)
                    source.Y += source.Height;

                // day
                b.Draw(texture, position, source, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, .00002f);

                // night
                var alpha = Utils.GetProgressFromEveningIntoNighttime(this, Game1.timeOfDay);
                if (alpha > 0)
                {
                    source.X += source.Width;
                    b.Draw(texture, position, source, Color.White * alpha, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, .00004f);
                }
			}
        }

        public override void drawBackground(SpriteBatch b)
        {
            base.drawBackground(b);

			// engawa
            if (this.DoorsOpen)
			{
                // landscape
                var texture = Game1.content.Load<Texture2D>(AssetManager.HouseSpritesAssetName);
				var tile = new Vector2(7, 2);
				var position = Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize);
                //var parallax = (new Vector2(this.Map.DisplayWidth / 2, this.Map.DisplayHeight / 4) - new Vector2(Math.Clamp(Game1.player.Position.X, 0, Game1.tileSize * tile.X * 2), Math.Clamp(Game1.player.Position.Y, 0, Game1.tileSize * 12))) / 25f;
                //var parallax = (new Vector2(this.Map.DisplayWidth / 2, this.Map.DisplayHeight / 4) - Game1.player.Position) / 10f;
                //var parallax = (new Vector2(0, this.Map.DisplayHeight / 8) - new Vector2(Game1.viewport.X, Game1.viewport.Y)) / 10f;
                var parallax = (new Vector2(this.Map.DisplayWidth / 2, this.Map.DisplayHeight / 4) - new Vector2(Math.Clamp(Game1.viewport.X, 0, Game1.tileSize * tile.X * 2), Math.Clamp(Game1.viewport.Y, 0, Game1.tileSize * 12))) / 25f;
				var source = new Rectangle(112, 400, 96, 48);
                var wind = Game1.isDebrisWeather ? 18 : 12;

				// day
				source.Y += source.Height * 2;
                b.Draw(texture, position + parallax, source, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, .00001f);
                // night
                var nightRatio = Utils.RatioFromPreciseTime(Game1.getStartingToGetDarkTime(this), 2100);
                if (nightRatio > 0)
				{
					source.X += source.Width;
                    b.Draw(texture, position + parallax, source, Color.White * nightRatio, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, .00003f);
                }

                // falling leaves
                if (Game1.season is not Season.Winter)
                {
                    // modified DrawSmokeParticles
                    texture = Game1.mouseCursors;

                    var frames = 11;
                    var colour = Color.White;
                    var ms = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
                    var origin = new Vector2(Game1.tileSize / Game1.pixelZoom / 2);
                    var scale = Game1.pixelZoom;
                    var interval = 7777;
                    var num = 4;
                    for (var i = 0; i < num; ++i)
                    {
                        var time = (float)((ms + i * i / 3 * interval / num) % interval);
                        var ratio = time / interval;
                        var frameRate = 2000 + i * 333 % 150;
                        var leafSource = new Rectangle(352, 1183, 16, 16);
                        leafSource.Y += Game1.seasonIndex * leafSource.Height;
                        leafSource.X += (int)(frames * (time % frameRate) / frameRate) * leafSource.Width;
                        b.Draw(
                            texture: texture,
                            position: position
                                + parallax
								+ new Vector2(i * 1f / num * source.Width + ratio * -wind, ratio * source.Height * .666f) * Game1.pixelZoom
                                ,
                            sourceRectangle: leafSource,
                            color: Color.Lerp(colour, Color.Black, nightRatio) * (1 - ratio * ratio),
                            rotation: 0,
                            origin: origin,
                            scale: scale / 2f,
                            effects: SpriteEffects.None,
                            layerDepth: .00005f + i * .00001f);
                    }
                }
				}
			}

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
        }
	}
}
