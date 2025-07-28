using System;
using System.Collections.Generic;
using StardewValley;

namespace Hikawa.Objects.Locations
{
	public class ShrineBackground : Background
	{
		public class Cloud
		{
			public static float Wind;
			public static float RandomScale => (float)(Game1.random.NextDouble() * 3f + 1f);
			public bool IsOffScreen => this.X < -149 * Game1.pixelZoom;

			public float X;
			public float Scale;

			public void Move(float dt)
			{
				float idle = dt * 0.6f;
				this.X += -(idle + Cloud.Wind) * this.Scale / Game1.pixelZoom;
			}
		}

		public bool IsWindy;
		public float AdjustedTime;
		public float DarknessRatio;
		public List<Cloud> Clouds;

		public ShrineBackground(GameLocation location) : base(location, color: Color.White, onlyMapBG: false)
		{
			this.summitBG = false;
			this.initialViewportY = Game1.viewport.Y;
			this.tempSprites = [];
			this.cloudsTexture = Game1.content.Load<Texture2D>("Minigames\\Clouds");

			this.IsWindy = Game1.currentLocation.IsDebrisWeatherHere();
			this.Clouds = new((3 + Game1.dayOfMonth % 3) * (this.IsWindy ? 2 : 1));
			for (int i = 0; i < this.Clouds.Capacity; ++i)
			{
				this.Clouds.Add(new()
				{
					X = Game1.random.Next(location.Map.DisplayWidth),
					Scale = Cloud.RandomScale
				});
			}
		}

		public override void update(xTile.Dimensions.Rectangle viewport)
		{
			float dt = Game1.currentGameTime.ElapsedGameTime.Milliseconds / 50f; // Elapsed time
			float decay = -0.0035f;
			Cloud.Wind = Math.Max(0, Cloud.Wind + dt * (decay - Game1.windGust / 350f));
			foreach (Cloud cloud in this.Clouds)
			{
				cloud.Move(dt);
				if (cloud.IsOffScreen)
				{
					cloud.X = this.location.Map.DisplayWidth;
					cloud.Scale = Cloud.RandomScale;
				}
			}

			base.update(viewport);
		}

		public override void draw(SpriteBatch b)
		{
			if (Game1.currentLocation is null || Game1.viewport.X <= -1000 || Game1.viewport.Y > 28 * Game1.tileSize)
				return;

			bool isRain = Game1.isRaining;
			bool isGreenRain = Utility.isGreenRainDay(Game1.dayOfMonth, Game1.season);
			bool isWindy = this.IsWindy;
			bool isWinter = Game1.IsWinter;
			bool isDark = Game1.isStartingToGetDarkOut(Game1.currentLocation);

			int seasonOffset = Game1.IsWinter ? 2 : Game1.IsFall ? 1 : 0;
			int yOffset = -Game1.viewport.Y / Game1.pixelZoom + this.initialViewportY / Game1.pixelZoom;
			float alpha = 1f;
			float skyAlpha = 1f;
			float cloudAlpha = 1f;
			float preciseTime = ModEntry.State.Value.PreciseTime;
			Color bgColor = isRain ? Color.SlateBlue : Color.White;
			Color fgColor = isRain ? Color.SlateGray : Color.White;
			Color bgEndColor = isRain || isWinter ? Color.DarkSlateGray : Color.DarkBlue;
			Color fgEndColor = isRain || isWinter ? Color.DarkSlateGray : Color.Blue;
			Rectangle source;
			Vector2 zero = new Vector2(x: Game1.viewport.X, y: Game1.viewport.Y) * -1f;
			Vector2 offset = new Vector2(0, 2) * Game1.tileSize;

			if (isDark)
			{
				this.c = new Color(
					r: 255f,
					g: 255f - Math.Max(100f, preciseTime - 1800f),
					b: 255f - Math.Max(100f, (preciseTime - 1800f) / 2f));
				cloudAlpha = 1f - Utils.RatioFromPreciseTime(startTime: Game1.getStartingToGetDarkTime(Game1.currentLocation), endTime: 2100);
				skyAlpha = Math.Clamp((2200f - preciseTime) / 200f, 0, 1);
				alpha = Math.Clamp((2000f - preciseTime) / 100f, 0, 1);
				bgColor = Color.Lerp(bgColor, bgEndColor, 1 - alpha);
				fgColor = Color.Lerp(fgColor, fgEndColor, 0.5f - alpha);
				bgColor.A = (byte)(255 * Math.Max(alpha, 0.5f));
				fgColor.A = 255;
			}

			Color cloudColor = Color.White;

			Rectangle display = new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height);

			// background
			if (isRain)
			{
				// StardewValley.Menus.ShippingMenu.cs

				// grey skies
				source = new Rectangle(isGreenRain ? 640 : 639, 858, 1, 184);
				b.Draw(
					texture: Game1.mouseCursors,
					destinationRectangle: new Rectangle(0, 0, display.Width, display.Height),
					sourceRectangle: source,
					color: isWinter ? Color.LightSlateGray : (isGreenRain ? Color.LightGreen : Color.SlateGray));
				if (isGreenRain)
				{
					b.Draw(
						texture: Game1.mouseCursors,
						destinationRectangle: new Rectangle(0, 0, display.Width, display.Height),
						sourceRectangle: source,
						color: Color.DimGray * 0.8f);
				}

				// stormclouds
				float weatherX = preciseTime / 500f * (display.Width + 2048);
				if (isRain)
				{
					for (int x = -244; x < Game1.uiViewport.Width + 244; x += 244)
					{
						b.Draw(Game1.mouseCursors, zero + new Vector2(x + weatherX / 2f % 244f, 32f), new Rectangle(643, 1142, 61, 53), Color.DarkSlateGray * 1f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
					}
					for (int x2 = 0; x2 < Game1.viewport.Width; x2 += 639)
					{
						b.Draw(Game1.mouseCursors, zero + new Vector2(x2 * 4, Game1.uiViewport.Height - 192), new Rectangle(0, isWinter ? 1034 : 737, 639, 48), (isWinter ? (Color.White * 0.25f) : new Color(30, 62, 50)), 0f, Vector2.Zero, 4f, SpriteEffects.FlipHorizontally, 1f);
						b.Draw(Game1.mouseCursors, zero + new Vector2(x2 * 4, Game1.uiViewport.Height - 128), new Rectangle(0, isWinter ? 1034 : 737, 639, 32), (isWinter ? (Color.White * 0.5f) : new Color(30, 62, 50)), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
					}
					for (int x3 = -244; x3 < Game1.uiViewport.Width + 244; x3 += 244)
					{
						b.Draw(Game1.mouseCursors, zero + new Vector2(x3 + weatherX % 244f, -32f), new Rectangle(643, 1142, 61, 53), Color.SlateGray * 0.85f, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
					}
					for (int x4 = -244; x4 < Game1.uiViewport.Width + 244; x4 += 244)
					{
						b.Draw(Game1.mouseCursors, zero + new Vector2(x4 + weatherX * 1.5f % 244f, -128f), new Rectangle(643, 1142, 61, 53), Color.LightSlateGray, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
					}
				}
			}
			else
			{
				// light skies
				int skyH = (int)offset.Y + 20 * Game1.tileSize;
				int skyY = 6 * Game1.tileSize;
				// fill colour
				b.Draw(
					texture: Game1.staminaRect,
					destinationRectangle: display,
					sourceRectangle: null,
					color: Color.AliceBlue,
					rotation: 0f,
					origin: Vector2.Zero,
					effects: SpriteEffects.None,
					layerDepth: 0f);
				// sprite
				source = new(703, 1912, 1, 264);
				b.Draw(
					texture: Game1.mouseCursors,
					destinationRectangle: new Rectangle(0, (int)(zero.Y - skyY * skyAlpha), display.Width, skyH),
					sourceRectangle: source,
					color: Color.White,
					rotation: 0f,
					origin: Vector2.Zero,
					effects: SpriteEffects.None,
					layerDepth: 0.000010f);

				if (isDark)
				{
					// dark skies
					source = new(702, 1912, 1, 264);
					b.Draw(
						texture: Game1.mouseCursors,
						destinationRectangle: new Rectangle(0, 0, display.Width, skyH),
						sourceRectangle: source,
						color: Color.White * (1f - skyAlpha),
						rotation: 0f,
						origin: Vector2.Zero,
						effects: SpriteEffects.None,
						layerDepth: 0.000011f);

					// stars
					source = new(0, 1453, 638, 195);
					for (int i = 0; i < display.Width; i += source.Width)
					{
						float scale = 2f;
						b.Draw(
							texture: Game1.mouseCursors,
							position: zero
								+ offset
								+ new Vector2(i * scale, -1.5f * Game1.tileSize),
							sourceRectangle: source,
							color: Color.White * (1f - skyAlpha),
							rotation: 0f,
							origin: Vector2.Zero,
							scale: scale,
							effects: SpriteEffects.None,
							layerDepth: 0.000012f);
					}

					if (!isWinter)
					{
						// evening skies
						int eveningStartTime = Game1.getStartingToGetDarkTime(Game1.currentLocation);
						int eveningEndTime = Game1.getTrulyDarkTime(Game1.currentLocation);
						int eveningRange = eveningEndTime - eveningStartTime;
						eveningStartTime += eveningRange / 6;
						eveningEndTime += eveningRange / 6;
						eveningRange = eveningRange / 4 * 5;
						float eveningRatio = Math.Clamp((preciseTime - eveningStartTime) / eveningRange, 0, 1);
						float eveningAlpha = Utils.CircularFromRatio(eveningRatio);
						cloudColor = Color.Lerp(Color.White, Color.Salmon, eveningAlpha);
						cloudAlpha += eveningAlpha * 0.75f;
						source = new(544, 208, 16, 240);
						b.Draw(
							texture: ModEntry.Sprites,
							destinationRectangle: new Rectangle(0, (int)(zero.Y - skyY * eveningAlpha), display.Width, skyH),
							sourceRectangle: source,
							color: Color.White * eveningAlpha,
							rotation: 0f,
							origin: Vector2.Zero,
							effects: SpriteEffects.None,
							layerDepth: 0.000015f);
					}
				}

				// clouds
				if (!isWinter && cloudAlpha > 0.075f)
				{
					// StardewValley.Menus.TitleMenu.cs
					float height = 2.75f * Game1.tileSize;
					for (int i = 0; i < this.Clouds.Count; i++)
					{
						b.Draw(
							texture: this.cloudsTexture,
							position: zero
								+ offset
								+ new Vector2(x: this.Clouds[i].X, y: height - i * 12 * Game1.pixelZoom),
							sourceRectangle: (i % 3 == 0) ? new Rectangle(152, 447, 123, 55) : ((i % 3 == 1) ? new Rectangle(0, 471, 149, 66) : new Rectangle(410, 467, 63, 37)),
							color: cloudColor * cloudAlpha * (this.Clouds[i].Scale / Game1.pixelZoom),
							rotation: 0f,
							origin: Vector2.Zero,
							scale: this.Clouds[i].Scale,
							effects: SpriteEffects.None,
							layerDepth: 0.000025f + i / 100000f);
					}
				}
			}

			// mountains
			source = new Rectangle(
				x: 0,
				y: 736,
				width: 639,
				height: 149);
			int rows = 2;
			int columns = (int)Math.Ceiling((double)Game1.currentLocation.Map.DisplayWidth / Game1.pixelZoom / source.Width);
			for (int i = 0; i < columns * rows; ++i)
			{
				int row = i / columns;
				int col = i % columns;
				bool isUpper = row == 0;
				b.Draw(
					texture: Game1.mouseCursors,
					position: offset +
						new Vector2(0, yOffset / (row == 0 ? 2.5f : 4f)) + Game1.GlobalToLocal(new Vector2(
							x: (col == 0 ? 0 : source.Width * Game1.pixelZoom * col) + (row % 2 == 0 ? 0 : -source.Width * Game1.pixelZoom / 2),
							y: row == 0 ? source.Height * 1.25f : source.Height * 4.25f)),
					sourceRectangle: new Rectangle(
						x: source.X,
						y: source.Y + (seasonOffset * source.Height),
						width: source.Width,
						height: source.Height),
					color: isUpper ? bgColor : fgColor,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: Game1.pixelZoom,
					effects: SpriteEffects.None,
					layerDepth: 0.0001f * i);
			}

			// trees
			for (int i = 0; i < 2; ++i)
			{
				Rectangle fillSource = new Rectangle(
					x: 320,
					y: 0,
					width: 240,
					height: 208);
				b.Draw(
					texture: ModEntry.Sprites,
					position: offset + 
						new Vector2(0, yOffset / 5f) + Game1.GlobalToLocal(new Vector2(
							x: i == 0 ? 0 : Game1.currentLocation.Map.DisplayWidth - fillSource.Width * Game1.pixelZoom,
							y: source.Height * 7.5f)),
					sourceRectangle: new Rectangle(
						x: fillSource.X,
						y: fillSource.Y/* + (seasonOffset * fillSource.Width)*/,
						width: fillSource.Width,
						height: fillSource.Height),
					color: Color.White,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: Game1.pixelZoom,
					effects: i == 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
					layerDepth: 0.0005f * i);
			}
		}
	}
}
