using Hikawa.Objects.Decor;
using System;
using System.Xml.Serialization;

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

			Utils.ApplyCustomSharedMapProperties(this);

			if (this.sharedLights.TryGetValue(HearthLight.GetId(where: this, which: 0), out LightSource light))
				this.HearthLight = light as HearthLight;

            // engawa. not cold or windy. rainy is ok and nice
            this.DoorsOpen = Game1.season is not Season.Winter && !Shrine.Get().IsDebrisWeatherHere();
		}

		public override void cleanupBeforePlayerExit()
		{
			Utils.ResetCustomSharedMapProperties(this);

			base.cleanupBeforePlayerExit();
		}

        public override void drawBackground(SpriteBatch b)
        {
            base.drawBackground(b);

			// engawa
			{

			}
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);

			// engawa
			// this would be unreasonably convoluted in content patcher
			{
				var texture = Game1.content.Load<Texture2D>($"{ModEntry.ModData.ContentPrefix}_{ModEntry.ModData.TilesheetHouse}");
				var tile = new Vector2(7, 2);
				var position = Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize);
				var source = new Rectangle(112, 400, 96, 48);

                if (DoorsOpen)
					source.Y += source.Height;

				// day
				b.Draw(texture, position, source, Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1);

                // night
                var alpha = Utils.GetProgressFromEveningIntoNighttime(this, Game1.timeOfDay);
                if (alpha > 0)
				{
					source.X += source.Width;
					b.Draw(texture, position, source, Color.White * alpha, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1);
				}
			}
        }
	}
}
