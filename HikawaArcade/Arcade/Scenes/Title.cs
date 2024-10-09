using HikawaArcade.Arcade.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Scenes
{
    public class Title : Scene
	{
		/* Title screen graphics */

		// 1P START
		// above full sailor frames
		private static readonly Rectangle Title1PStartSprite = new Rectangle(
			TD * 6,
			HudDigitSprite.Y + HudDigitSprite.Height,
			TD * 4,
			TD * 1);
		// コードネームは
		// beneath split sailor frames
		private static readonly Rectangle TitleCodenameSprite = new Rectangle(
			TD * 1,
			PlayerLegsY + PlayerSplitWH,
			TD * 4,
			TD * 2);
		// セーラー
		// beneath codename text
		private static readonly Rectangle TitleSailorSprite = new Rectangle(
			0,
			TitleCodenameSprite.Y + TitleCodenameSprite.Height,
			TD * 7,
			TD * 5);
		// © テレビ望月 / 東映動画・1996
		// beneath sailor text
		private static readonly Rectangle TitleSignatureSprite = new Rectangle(
			0,
			TitleSailorSprite.Y + TitleSailorSprite.Height,
			TD * 6,
			TD * 2);
		// Red V/
		// beneath split sailor frames, beside sailor text
		private static readonly Rectangle TitleRedBannerSprite = new Rectangle(
			TitleSailorSprite.Width,
			PlayerLegsY + PlayerSplitWH,
			TD * 5,
			TD * 8);
		// White V/
		// beneath split sailor frames, beside red V with spacing on either side
		private static int TitleLightShaftW = 8;
		private static readonly Rectangle TitleWhiteBannerSprite = new Rectangle(
			TitleRedBannerSprite.X + TitleRedBannerSprite.Width,
			TitleRedBannerSprite.Y,
			TitleRedBannerSprite.Width + TD * 2,
			TitleRedBannerSprite.Height);

		// Title screen, splash screen and standby
		private static float AnimCutsceneBackgroundSpeed = 0.1f;
		private static int MsBeforeWhite = 800; // blank before light shaft
		private static int MsAfterWhite = 1200; // blank after light shaft
		private static int MsPhase1 = 750; // red V/
		private static int MsPhase2 = 400; // コードネームは
		private static int MsPhase3 = 400; // セーラー
		private static int MsPhase4 = 400; // © BLUEBERRY 1996
		private static int MsPhase5 = 400; // fire to start
		private static int MsBlinkPrompt = 600; // fire to start blink period

		private float CutsceneBackgroundPosition = 0;

		private readonly Pool<Particle> Petals;


		public Title()
			: base()
		{
			this.Petals = new Pool<Particle>(capacity: 32);
		}

		public override void HandleClick(int x, int y)
		{
			if (this.Phase < 4)
			{
				this.Phase = 4; // Skip the splash animation
			}
			else
			{
				++this.Phase;
			}
		}

		public override void HandleInput(Keys k)
		{
		}

		public override void HandleInputReleased(Keys k)
		{
		}

		public void UpdateParticles(TimeSpan time)
		{
			this.Petals.ForEach((Particle petal) =>
			{
				bool isDead = petal.Update(time: time) == State.IsDead;
				if (petal.Position.X > Width || petal.Position.Y > Height)
					isDead = true;
				return isDead;
			});
		}

		public override State Update(TimeSpan time)
		{
			// Progress through a small intro sequence
			this.Timer += time.Milliseconds;

			double delta = time.Milliseconds * (double)AnimCutsceneBackgroundSpeed;
			this.CutsceneBackgroundPosition = (float)(this.CutsceneBackgroundPosition + delta) % 96f;

			if (this.Timer >= Title.MsPhase2 && (this.Timer / time.Milliseconds) % (1000 / time.Milliseconds) == 0)
			{
				// Spawn petals
				Particle particle = this.Petals.Get();
				particle = this.Game.ParticleTemplate["Petal"].CopyTo(particle) as Particle;
				particle.Origin = particle.Position = new Vector2(this.Game.Random.Next(-particle.SpriteSize.X, Width - particle.SpriteSize.X), -particle.SpriteSize.Y);
				particle.Fire(target: new Vector2(particle.Position.X + this.Game.Random.Next(48, 128), Height));
				particle.SpriteMirror = (int)particle.Origin.X % 4 == 3
					? SpriteEffects.None
					: SpriteEffects.FlipHorizontally;
				if (particle.Origin.X % 10 > 4)
					particle.Colour = Color.LightPink;
				else if (particle.Origin.X % 10 > 2)
					particle.Colour = Color.PaleVioletRed;
			}
			this.UpdateParticles(time: time);

			switch (this.Phase)
			{
				case 0:
					if (this.Timer >= Title.MsBeforeWhite)
					{
						// Move the lightshaft V/ texture across the screen
						this.CutsceneBackgroundPosition += Width / UiAnimTimescale;
					}
					if (this.Timer >= Title.MsAfterWhite)
					{
						++this.Phase; // Start showing all the title screen elements after it's held on blank for a bit
						this.Game.PlaySound("wand");
						this.Game.PlayMusic("cm:blueberry.hikawa.arcade_main:Cowboy_OVERWORLD");
					}
					break;
				case 1:
					if (this.Timer >= Title.MsPhase1)
					{
						++this.Phase;
						this.Game.PlaySound("drumkit6");
					}
					break;
				case 2:
					if (this.Timer >= Title.MsPhase2)
					{
						++this.Phase;
						this.Game.PlaySound("drumkit6");
					}
					break;
				case 3:
					if (this.Timer >= Title.MsPhase3)
					{
						++this.Phase;
						this.Game.PlaySound("drumkit6");
					}
					break;
				case 4:
					if (this.Timer < Title.MsPhase4)
						this.Timer = Title.MsPhase4;
					break;
				case 5:
					// End the cutscene and begin the game
					// after the user clicks past the end of intro cutscene (phase 4)
					this.Game.PlaySound("cowboy_gunload");
					return State.IsDead;
			}

			return State.IsAlive;
		}

		public override void Draw(SpriteBatch b, Rectangle viewport)
		{
			base.Draw(b, viewport: viewport);

			this.Game.DrawColour(b: b, viewport: viewport, colour: PaletteColour.DarkBlue, layerDepth: 1);

			// Draw petals
			this.Petals.ForEach((Particle petal) =>
			{
				petal.Draw(b: b, viewport: viewport);
			});

			// Draw each phase of the intro
			if (this.Phase == 0)
			{
				// Draw the V/ banner as if illuminated by a shaft of light moving across the screen
				if (this.Timer >= MsBeforeWhite)
				{
					// White V/
					this.Game.Draw(
						b: b,
						viewport: viewport,
						area: new Rectangle(
							(viewport.Width / 2) + (TD * 1) + (int)this.CutsceneBackgroundPosition,
							(viewport.Height / 2) - (TD * 2),
							TitleLightShaftW,
							TitleWhiteBannerSprite.Height),
						sourceRectangle: new Rectangle(
							TitleWhiteBannerSprite.X + (int)this.CutsceneBackgroundPosition,
							TitleWhiteBannerSprite.Y,
							TitleLightShaftW,
							TitleWhiteBannerSprite.Height),
						origin: new Vector2(0, TitleWhiteBannerSprite.Height / 2));
				}
			}

			if (this.Phase >= 1)
			{
				// Draw the coloured title banner on black

				// Red V/
				this.Game.Draw(
					b: b,
					viewport: viewport,
					position: new Vector2(
						(TD * 1),
						(TD * -2)),
					sourceRectangle: TitleRedBannerSprite,
					origin: new Vector2(0, TitleRedBannerSprite.Height / 2),
					alignX: AlignX.Centre,
					alignY: AlignY.Centre);
			}

			if (this.Phase >= 2)
			{
				// Draw the coloured title banner with all title screen text

				// コードネームは
				this.Game.Draw(
					b: b,
					viewport: viewport,
					position: new Vector2(
						-24,
						-64),
					sourceRectangle: TitleCodenameSprite,
					origin: new Vector2(TitleCodenameSprite.Width / 2, TitleCodenameSprite.Height / 2),
					alignX: AlignX.Centre,
					alignY: AlignY.Centre);
			}

			if (this.Phase >= 3)
			{
				// セーラー
				this.Game.Draw(
					b: b,
					viewport: viewport,
					position: new Vector2(
						-24,
						-24),
					sourceRectangle: TitleSailorSprite,
					origin: new Vector2(TitleSailorSprite.Width / 2, TitleSailorSprite.Height / 2),
					alignX: AlignX.Centre,
					alignY: AlignY.Centre);
			}

			if (this.Phase >= 4)
			{
				// Draw flashing 'fire to start' and signature text

				// © テレビ望月 / © 東映動画・1996
				this.Game.Draw(
					b: b,
					viewport: viewport,
					position: new Vector2(
						0,
						16),
					sourceRectangle: TitleSignatureSprite,
					origin: TitleSignatureSprite.Size.ToVector2() / 2,
					alignX: AlignX.Centre,
					alignY: AlignY.CentreBottom);

				if (this.Timer >= MsPhase5 && (this.Timer / MsBlinkPrompt) % 2 == 0)
				{
					// 1P START
					this.Game.Draw(
						b: b,
						viewport: viewport,
						position: new Vector2(
							0,
							-16),
						sourceRectangle: Title1PStartSprite,
						origin: Title1PStartSprite.Size.ToVector2() / 2,
						alignX: AlignX.Centre,
						alignY: AlignY.CentreBottom);
				}
			}
		}
	}
}
