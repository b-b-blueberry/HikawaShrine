using System;
using System.Collections.Generic;
using StardewValley;

namespace Hikawa.Objects.Events
{
	public static class EventSprites
	{
		private static Texture2D _sprites;

		public static Texture2D Sprites => _sprites
			??= ModEntry.Instance.Helper.GameContent.Load<Texture2D>(AssetManager.EventSpritesAssetName);

		#region Sprite areas

		// Starry sky
		public static readonly Rectangle StarrySkySourceArea = new(0, 0, 480, 640);

		// Shrine roof
		public static readonly Rectangle ShrineRoofSourceArea = new(0, 1008, 480, 128);

		// Trees
		public static readonly Rectangle ShrineTreeLeftSourceArea = new(0, 640, 158, 240);
		public static readonly Rectangle ShrineTreeRightSourceArea = new(EventSprites.ShrineTreeLeftSourceArea.Width, EventSprites.ShrineTreeLeftSourceArea.Y, 176, 240);

		// Full moon
		public static readonly Rectangle FullMoonSourceArea = new(384, 640, 96, 96);

		// Glare
		public static readonly Rectangle GlareSourceArea = new(208, 880, 112, 32);
		public static readonly Rectangle GlareDrawArea = new(0, 0, EventSprites.GlareSourceArea.Width, EventSprites.GlareSourceArea.Height);
		public static readonly Vector2 GlareDrawOrigin = new(0.5f, 1.66f);

		// Hand
		public static readonly List<Rectangle> HandSourceAreas = new()
		{
			// 0 4 3 1 2
			new Rectangle(64, 896, 128, 38),
			new Rectangle(160, 938, 48, 64),
			new Rectangle(112, 938, 48, 64),
			new Rectangle(0, 944, 64, 64),
			new Rectangle(64, 944, 48, 64),
		};
		public static readonly List<Rectangle> HandDrawAreas = new()
		{
			// 0 4 3 1 2
			// Order is important to preserve draw layering
			// Order to match HandSourceAreas
			new(16, -16, 128, 38),
			new(32, 0, 48, 64),
			new(16, 0, 48, 64),
			new(24, 0, 64, 64),
			new(0, 0, 48, 64),
		};
		public static readonly List<Vector2> HandDrawOrigins = new()
		{
			// Order to match HandDrawAreas
			new(0.8f, 0.25f),
			new(0, 0.25f),
			new(0, 0.25f),
			new(1, 0.25f),
			new(0, 0.25f),
		};

		#endregion

		#region Draw methods

		public static void DrawBlack(SpriteBatch b)
		{
			b.Draw(
				texture: Game1.staminaRect,
				destinationRectangle: new(
					x: 0,
					y: 0,
					width: Game1.graphics.GraphicsDevice.Viewport.Width,
					height: Game1.graphics.GraphicsDevice.Viewport.Height),
				color: Color.Black);
		}

		public static void DrawStarrySky(SpriteBatch b, Rectangle area, Vector2 position)
		{
			b.Draw(
				texture: EventSprites.Sprites,
				sourceRectangle: EventSprites.StarrySkySourceArea,
				position: new Vector2(area.Center.X, area.Center.Y) + position,
				rotation: 0,
				origin: new Vector2(0.5f) * Utility.PointToVector2(EventSprites.StarrySkySourceArea.Size),
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				color: Color.White,
				layerDepth: 0.0001f);
		}

		public static void DrawFullMoon(SpriteBatch b, Rectangle area, Vector2 position)
		{
			b.Draw(
				texture: EventSprites.Sprites,
				sourceRectangle: EventSprites.FullMoonSourceArea,
				position: new Vector2(area.Center.X, area.Bottom) + position,
				rotation: 0,
				origin: new Vector2(0.5f) * Utility.PointToVector2(EventSprites.FullMoonSourceArea.Size),
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				color: Color.White,
				layerDepth: 0.6f);
		}

		public static void DrawShrineRoof(SpriteBatch b, Rectangle area, Vector2 position)
		{
			b.Draw(
				texture: EventSprites.Sprites,
				sourceRectangle: EventSprites.ShrineRoofSourceArea,
				position: new Vector2(area.Center.X, area.Bottom) + position,
				rotation: 0,
				origin: new Vector2(0.5f, 1f) * Utility.PointToVector2(EventSprites.ShrineRoofSourceArea.Size),
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				color: Color.White,
				layerDepth: 0.5f);
		}

		public static void DrawShrineTrees(SpriteBatch b, Rectangle area, Vector2 position)
		{
			b.Draw(
				texture: EventSprites.Sprites,
				sourceRectangle: EventSprites.ShrineTreeLeftSourceArea,
				position: new Vector2(x: area.Left, y: area.Bottom) + position,
				rotation: 0,
				origin: new Vector2(0, 1) * Utility.PointToVector2(EventSprites.ShrineTreeLeftSourceArea.Size),
				scale: Game1.pixelZoom,
				effects: SpriteEffects.FlipHorizontally,
				color: Color.White,
				layerDepth: 0.7f);

			b.Draw(
				texture: EventSprites.Sprites,
				sourceRectangle: EventSprites.ShrineTreeRightSourceArea,
				position: new Vector2(x: area.Right, y: area.Bottom) + position,
				rotation: 0,
				origin: new Vector2(1, 1) * Utility.PointToVector2(EventSprites.ShrineTreeRightSourceArea.Size),
				scale: Game1.pixelZoom,
				effects: SpriteEffects.FlipHorizontally,
				color: Color.White,
				layerDepth: 0.77f);
		}

		public static void DrawGlare(SpriteBatch b, Rectangle area, Vector2 position, float glareAlpha, float handAlpha)
		{
			if (Math.Abs(0 - glareAlpha) > 0.001f)
			{
				b.Draw(
					texture: EventSprites.Sprites,
					sourceRectangle: EventSprites.GlareSourceArea,
					position: new Vector2(x: area.Center.X, y: area.Center.Y) + position,
					scale: Game1.pixelZoom,
					rotation: 0f,
					origin: EventSprites.GlareDrawOrigin * Utility.PointToVector2(EventSprites.GlareSourceArea.Size),
					effects: SpriteEffects.None,
					color: Color.White * glareAlpha,
					layerDepth: 0.8f);
			}

			if (Math.Abs(0 - handAlpha) > 0.001f)
			{
				Vector2 handOffset = new Vector2(-32, 0) * Game1.pixelZoom;
				for (int i = 0; i < EventSprites.HandSourceAreas.Count; ++i)
				{
					b.Draw(
						texture: EventSprites.Sprites,
						sourceRectangle: EventSprites.HandSourceAreas[i],
						position: position + handOffset + new Vector2(
							x: area.Center.X + EventSprites.HandDrawAreas[i].X * Game1.pixelZoom,
							y: area.Center.Y + EventSprites.HandDrawAreas[i].Y * Game1.pixelZoom
								+ (int)Math.Ceiling(position.Y + position.Y * Math.Abs(EventSprites.HandDrawAreas.Count / 2f - i) / 2f)),
						scale: Game1.pixelZoom,
						rotation: 0f,
						origin: EventSprites.HandDrawOrigins[i] * Utility.PointToVector2(EventSprites.HandSourceAreas[i].Size),
						effects: SpriteEffects.None,
						color: Color.White * handAlpha,
						layerDepth: 0.9f - i / 10000f);
				}
			}
		}

		#endregion
	}
}
