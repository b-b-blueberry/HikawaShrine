using HikawaArcade.Arcade.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Scenes
{
    public class GameOver : Scene
	{
		private int GameRestartTimer;
		private int CurrentMenuOption;

		public override void Draw(SpriteBatch b, Rectangle viewport)
		{
			base.Draw(b, viewport: viewport);

			this.Game.DrawColour(b: b, viewport: viewport, colour: PaletteColour.Black, layerDepth: 1 / 10000f);

			b.DrawString(
				Game1.dialogueFont,
				Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
				new Vector2(viewport.Left, viewport.Top)
				+ new Vector2(6f, 7f) * TD,
				Color.White,
				0.0f,
				Vector2.Zero,
				1f,
				SpriteEffects.None,
				1f);

			b.DrawString(
				Game1.dialogueFont,
				Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
				new Vector2(viewport.Left, viewport.Top)
				+ new Vector2(6f, 7f) * TD
				+ new Vector2(-1f, 0.0f),
				Color.White,
				0.0f,
				Vector2.Zero,
				1f,
				SpriteEffects.None,
				1f);

			b.DrawString(
				Game1.dialogueFont,
				Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
				new Vector2(viewport.Left, viewport.Top)
				+ new Vector2(6f, 7f) * TD
				+ new Vector2(1f, 0.0f),
				Color.White,
				0.0f,
				Vector2.Zero,
				1f,
				SpriteEffects.None,
				1f);

			string text1 = Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11917");
			if (this.CurrentMenuOption == 0)
				text1 = "> " + text1;

			string text2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11919");
			if (this.CurrentMenuOption == 1)
				text2 = "> " + text2;

			if (this.GameRestartTimer <= 0 || this.GameRestartTimer / 500 % 2 == 0)
			{
				b.DrawString(
					Game1.smallFont,
					text1,
					new Vector2(viewport.Left, viewport.Top)
					+ new Vector2(6f, 9f) * TD,
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);
			}

			b.DrawString(
				Game1.smallFont,
				text2,
				new Vector2(viewport.Left, viewport.Top)
				+ new Vector2(6f, 9f) * TD
				+ new Vector2(0.0f, 2f / 3f),
				Color.White,
				0.0f,
				Vector2.Zero,
				1f,
				SpriteEffects.None,
				1f);
		}
	}
}
