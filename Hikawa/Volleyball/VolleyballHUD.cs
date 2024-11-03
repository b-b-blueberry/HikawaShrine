using StardewValley;

namespace Hikawa.Volleyball
{
	public static class VolleyballHUD
	{
		private static readonly Rectangle[] Digits = new Rectangle[10];
		private static Point DigitOrigin = new Point(x: 240, y: 16);
		private static Point DigitSize = new Point(x: 16, y: 16);
		private static Rectangle DigitSlice = new Rectangle(x: 2, y: 1, width: 12, height: 15);
		private static int DigitKerning = 2;
		private static bool IsInit = false;

		internal static void Init()
		{
			if (VolleyballHUD.IsInit)
			{
				return;
			}
			Rectangle slice = new Rectangle(location: VolleyballHUD.DigitOrigin, size: VolleyballHUD.DigitSize);
			const int rows = 2;
			for (int i = 0; i < VolleyballHUD.Digits.Length; ++i)
			{
				if (i > 0)
				{
					if (i % (VolleyballHUD.Digits.Length / rows) == 0)
					{
						slice.X = VolleyballHUD.DigitOrigin.X;
						slice.Y += slice.Height;
					}
					else
					{
						slice.X += slice.Width;
					}
				}
				VolleyballHUD.Digits[i] = new Rectangle(
					x: slice.X + VolleyballHUD.DigitSlice.X,
					y: slice.Y + VolleyballHUD.DigitSlice.Y,
					width: VolleyballHUD.DigitSlice.Width,
					height: VolleyballHUD.DigitSlice.Height);
			}
			VolleyballHUD.IsInit = true;
		}

		public static Vector2 DrawDigits(SpriteBatch b, Vector2 position, Vector2 origin, uint value, float scale)
		{
			foreach (char digit in value.ToString())
			{
				b.Draw(
					texture: ModEntry.Sprites,
					position: position,
					sourceRectangle: VolleyballHUD.Digits[digit - '0'],
					color: Color.White,
					rotation: 0f,
					origin: origin * Utility.PointToVector2(VolleyballHUD.DigitSize),
					scale: scale,
					effects: SpriteEffects.None,
					layerDepth: 1f);
				position.X += (VolleyballHUD.DigitSlice.Width + VolleyballHUD.DigitKerning) * scale;
			}
			return position;
		}

		public static Rectangle GetNPCPortraitSourceArea(NPC npc)
		{
			Rectangle source = npc.getMugShotSourceRect();
			source.Y += 1;
			source.Height = 16;
			return source;
		}

		public static void Draw(SpriteBatch b, Character[] characters, int scoreL, int scoreR)
		{
			const float scale = Game1.pixelZoom;
			const float layerDepth = 1f;
			Vector2 anchor = new Vector2(x: Game1.viewport.Width / 2, y: 0);

			// Versus
			b.Draw(
				texture: ModEntry.Sprites,
				sourceRectangle: AssetManager.ExtraSpritesVolleyballVersusArea,
				position: anchor,
				color: Color.White,
				rotation: 0,
				origin: new Vector2(x: AssetManager.ExtraSpritesVolleyballVersusArea.Width / 2, y: 0),
				scale: scale,
				effects: SpriteEffects.None,
				layerDepth: 1f);

			// Scores
			Vector2 spacing = new Vector2(x: 8, y: 4);
			VolleyballHUD.DrawDigits(
				b: b,
				position: anchor + new Vector2(
					x: 8 + (AssetManager.ExtraSpritesVolleyballVersusArea.Width + spacing.X) / 2 * -scale,
					y: (AssetManager.ExtraSpritesVolleyballVersusArea.Height + spacing.Y) / 2 * scale),
				origin: new Vector2(0.5f),
				value: (uint)scoreL,
				scale: scale);
			VolleyballHUD.DrawDigits(
				b: b,
				position: anchor + new Vector2(
					x: 8 + (AssetManager.ExtraSpritesVolleyballVersusArea.Width + spacing.X) / 2 * scale,
					y: (AssetManager.ExtraSpritesVolleyballVersusArea.Height + spacing.Y) / 2 * scale),
				origin: new Vector2(0.5f),
				value: (uint)scoreR,
				scale: scale);

			for (int i = 0; i < characters.Length; ++i)
			{
				// Portraits
				Point size = Point.Zero;
				float xOffsetPerChara = 16 + 4; // Standard horizontal spacing between origin of each player icon
				bool isDoubles = characters.Length > 2; // Whether there are more than 2 player icons to display
				bool isLeftTeam = i < characters.Length / 2; // Whether player is on left side of play area
				int xFlipPerTeam = isLeftTeam ? -1 : 1; // Side of centre per player icon
				int xMultiplierPerTeam = isLeftTeam ? 1 - i : i; // Distance per player icon from centre
				Vector2 position = anchor
					+ new Vector2(x: VolleyballHUD.DigitSize.X * scale * xFlipPerTeam, y: 0)
					+ new Vector2(x: (isDoubles ? xMultiplierPerTeam % 2 : xMultiplierPerTeam / 2) * xOffsetPerChara * xFlipPerTeam, y: 2) * scale
					+ new Vector2(x: AssetManager.ExtraSpritesVolleyballVersusArea.Width / 4 * 3 * xFlipPerTeam, y: 0) * scale;
				Vector2 offset = Vector2.Zero;
				if (characters[i] is Farmer farmer)
				{
					size = new Point(x: 16, y: 16);
					offset = new Vector2(x: -size.X, y: 0) * scale / 2;
					farmer.FarmerRenderer.drawMiniPortrat(
						b: b,
						position: position + offset,
						layerDepth: layerDepth,
						scale: scale,
						facingDirection: Game1.down,
						who: farmer);
				}
				else if (characters[i] is NPC npc)
				{
					Rectangle source = npc.getMugShotSourceRect();
					source.Y += 1;
					source.Height = 16;
					size = source.Size;
					offset = new Vector2(x: -size.X, y: 0) * scale / 2;
					b.Draw(
						texture: npc.Sprite.spriteTexture,
						position: position + offset,
						sourceRectangle: source,
						color: Color.White,
						rotation: 0,
						origin: new Vector2(x: 0, y: 1),
						scale: scale,
						effects: SpriteEffects.None,
						layerDepth: layerDepth);
				}

				// Player tags
				if (characters[i] is not null)
				{
					Rectangle source = AssetManager.ExtraSpritesVolleyballPlayerTagArea;
					source.Height = 10;
					source.X += i * source.Width;
					b.Draw(
						texture: ModEntry.Sprites,
						sourceRectangle: source,
						position: position + offset + new Vector2(x: 0, y: source.Height + 4) * scale + Utility.PointToVector2(size) * scale / 2,
						color: Color.White,
						rotation: 0,
						origin: Utility.PointToVector2(source.Size) / 2,
						scale: scale,
						effects: SpriteEffects.None,
						layerDepth: 1f);
				}
			}
		}
	}
}
