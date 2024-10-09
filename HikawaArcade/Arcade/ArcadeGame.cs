using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Minigames;
using System;
using System.Collections.Generic;
using System.IO;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Colour = Microsoft.Xna.Framework.Color;
using HikawaArcade.Arcade.Objects;

namespace HikawaArcade.Arcade
{
    public class ArcadeGame : IMinigame
	{
		internal static IModHelper Helper => ModEntry.Instance.Helper;
		internal static bool IsDebugMode => ModEntry.Config.DebugMode;
		internal static bool IsCheating => ModEntry.Config.DebugArcadeCheats;


		///////////////////////
		#region Constant Values
		///////////////////////


		/* Game attributes */

		// Game values
		internal static int GameLivesDefault = 1;
		internal static int GameEnergyThresholdLow = 3;
		internal static int GameDodgeDelay = 500;
		internal static int GameInvincibleDelay = 3000;
		internal static int GameDeathDelay = 3000;
		internal static int GameEndDelay = 5000;

		// Score values
		internal static int ScoreMax = 99999999;
		internal static int ScoreCake = 3000;
		internal static int ScoreCakeExtra = 150;
		internal static int ScoreBread = 10400;

		/* Sprite attributes */

		// Sprite dimensions and size multipliers
		/// <summary>
		/// Tile Dimensions (in pixels)
		/// </summary>
		public static int TD = 16;
		/// <summary>
		/// Render scaling for all drawn elements
		/// </summary>
		private static int SpriteScale = 1; // 2;
		/// <summary>
		/// Viewport X-axis dimension
		/// </summary>
		public static int Width = 320;
		/// <summary>
		/// Viewport Y-axis dimension
		/// </summary>
		public static int Height = 256;
		// Animation rates, lower is faster
		internal static int UiAnimTimescale = 85;
		internal static int PlayerAnimTimescale = 250;
		internal static int BulletAnimTimescale = 500;
		internal static int PowerupAnimTimescale = 100;

		/* Enum values */

		// Game common attributes
		public enum PaletteColour
		{
			Black,
			White,
			LightRed,
			Red,
			DarkRed,
			LightGreen,
			Green,
			DarkGreen,
			LightBlue,
			Blue,
			DarkBlue,
			Brown,
			MAX
		}
		public enum AlignX
		{
			Left,
			CentreLeft,
			Centre,
			CentreRight,
			Right
		}
		public enum AlignY
		{
			Top,
			CentreTop,
			Centre,
			CentreBottom,
			Bottom
		}
		public enum Move
		{
			Right,
			Left
		}
		// Player attributes
		public enum SpecialPower
		{
			None,
			Normal,
			Megaton,
			Sulphur,
			Incense
		}
		public enum PowerPhase
		{
			None,
			BeforeActive1,
			BeforeActive2,
			Active1,
			Active2,
			Active3,
			Active4,
			AfterActive1,
			AfterActive2
		}
		// Player special power animation
		internal static int TimeToPowerPhaseBeforeActive1 = 400;
		internal static int TimeToPowerPhaseBeforeActive2 = TimeToPowerPhaseBeforeActive1 + 400;
		internal static int TimeToPowerPhaseActive1 = TimeToPowerPhaseBeforeActive2 + 1000;
		internal static int TimeToPowerPhaseActive2 = TimeToPowerPhaseActive1 + 500;
		internal static int TimeToPowerPhaseActive3 = TimeToPowerPhaseActive2 + 500;
		internal static int TimeToPowerPhaseActive4 = TimeToPowerPhaseActive3 + 250;
		internal static int TimeToPowerPhaseAfterActive1 = TimeToPowerPhaseActive4 + 400;
		internal static int TimeToPowerPhaseAfterActive2 = TimeToPowerPhaseAfterActive1 + 400;
		internal static int TimeToPowerPhaseEnd = TimeToPowerPhaseAfterActive2 + 400;
		internal static readonly Dictionary<PowerPhase, int> PowerPhaseDurations = new Dictionary<PowerPhase, int>
		{
			{ PowerPhase.None, TimeToPowerPhaseBeforeActive1 },
			{ PowerPhase.BeforeActive1, TimeToPowerPhaseBeforeActive2 },
			{ PowerPhase.BeforeActive2, TimeToPowerPhaseActive1 },
			{ PowerPhase.Active1, TimeToPowerPhaseActive2 },
			{ PowerPhase.Active2, TimeToPowerPhaseActive3 },
			{ PowerPhase.Active3, TimeToPowerPhaseActive4 },
			{ PowerPhase.Active4, TimeToPowerPhaseAfterActive1 },
			{ PowerPhase.AfterActive1, TimeToPowerPhaseAfterActive2 },
			{ PowerPhase.AfterActive2, TimeToPowerPhaseEnd },
		};
		public enum PowerAnimationFrameGroup
		{
			None = 0,
			Normal = 0,
			Megaton = 0,
			Sulphur = 1,
			Incense = 1
		}

		/* Text and digit graphics */

		internal static readonly Rectangle HudDigitSprite = new Rectangle( // HARD Y
			0, TD * 4, TD, TD);
		internal static readonly int HudStringsY = HudDigitSprite.Y + HudDigitSprite.Height;
		internal static readonly int HudStringsH = TD;

		/* HUD graphics */

		// Crosshair
		internal static readonly Rectangle CrosshairDimen = new Rectangle( // HARD Y
			TD * 2, 0, TD, TD);

		// Player life
		internal static readonly Rectangle HudLifeSprite = new Rectangle( // HARD Y
			TD * 12, 0, TD, TD);
		// Player energy
		internal static readonly Rectangle HudEnergySprite = new Rectangle( // HARD Y
			HudLifeSprite.X + HudLifeSprite.Width, 0, TD, TD);
		// Player portrait
		internal static readonly Rectangle HudPortraitSprite = new Rectangle(
			0, HudStringsY + HudStringsH, TD * 2, TD * 2);

		// Player score
		internal static readonly Rectangle HudScoreTextSprite = new Rectangle(
			0, HudStringsY, TD * 1, HudStringsH);
		// Stage goal
		internal static readonly Rectangle HudHealthTextSprite = new Rectangle( // WEIRD X
			HudEnergySprite.X + HudEnergySprite.Width, HudStringsY, TD * 3, TD);
		// Stage time
		internal static readonly Rectangle HudTimeSprite = new Rectangle( // HARD Y
			HudEnergySprite.X + HudEnergySprite.Width, 0, TD, TD);

		/* Player graphics */

		// Shared attributes
		internal static int PlayerW = TD * 2;
		internal static int PlayerX = 0;
		// Full-body sprites (body, arms and legs combined)
		internal static int PlayerFullH = TD * 3;
		internal static int PlayerFullY = TD * 8; // HARD Y
												 // Split-body sprites (body, arms or legs individually)
		internal static int PlayerSplitWH = TD * 2;

		// TODO: CONTENT: Resolve special/power animations

		// Full-body pre-special power pose
		internal static int PlayerPoseFrames = 3;
		internal static int PlayerPoseX = PlayerX + PlayerW * PlayerIdleFrames;
		// Full-body special power windup
		internal static int PlayerSpecialFrames = 2;
		internal static int PlayerSpecialX = PlayerPoseX + PlayerW * PlayerPoseFrames;
		// Full-body special power activated
		internal static int PlayerPowerX = PlayerSpecialX + PlayerW * PlayerSpecialFrames;
		internal static int PlayerPowerFrames = 3;

		// Split-body leg frames
		internal static int PlayerIdleFrames = 2;
		internal static int PlayerLegsIdleX = PlayerX;
		internal static int PlayerRunFrames = 4;
		internal static int PlayerLegsRunX = PlayerLegsIdleX + PlayerW * PlayerIdleFrames;
		// Split-body body frames
		internal static int PlayerBodyY = PlayerFullY + PlayerFullH;
		internal static int PlayerBodySideFireX = PlayerX;
		internal static int PlayerBodyUpFireX = PlayerBodySideFireX + PlayerSplitWH;
		internal static int PlayerBodyRunX = PlayerBodyUpFireX + PlayerSplitWH;
		internal static int PlayerBodyRunFireX = PlayerBodyRunX + PlayerSplitWH * PlayerRunFrames;
		// Split-body arm frames
		internal static int PlayerLegsY = PlayerBodyY + PlayerSplitWH;
		internal static int PlayerArmsX = PlayerLegsRunX + PlayerW * PlayerRunFrames;
		internal static int PlayerArmsY = PlayerLegsY;
		// Special powers
		internal static int PowerFxY = TD * 5; // HARD Y
											  // Shadows
		internal static readonly Rectangle ActorShadowRect = new Rectangle(
			HudDigitSprite.X + (HudDigitSprite.Width * 10),
			HudDigitSprite.Y,
			TD * 2,
			TD);
		internal static readonly Rectangle LootShadowRect = new Rectangle(
			ActorShadowRect.X + ActorShadowRect.Width,
			ActorShadowRect.Y,
			TD,
			TD);

		/* Cutscene graphics */


		#endregion


		//////////////////////////
		#region Gameplay Variables
		//////////////////////////

		// Game state
		public Rectangle Viewport { get; private set; }
		public Player Player = null;
		public Scene Scene = null;
		public Arcade.Objects.Stats Stats = null;
		public UI UI = null;
		public Random Random = null;
		public Colour[] ColourPalette = new Colour[(int)PaletteColour.MAX];
		public bool IsTimePassing = false;

		// Templates
		public Dictionary<string, Bullet> BulletTemplate = null;
		public Dictionary<string, Monster> MonsterTemplate = null;
		public Dictionary<string, Particle> ParticleTemplate = null;
		public Dictionary<string, Pickup> PickupTemplate = null;
		public Dictionary<string, Scene> SceneTemplate = null;
		public Dictionary<string, Stage> StageTemplate = null;
		public Dictionary<string, Player> PlayerTemplate = null;

		// Audio
		public ICue Music;
		public readonly string MusicBeforePlaying = null;
		public bool ShouldPlaySound = true;
		public bool ShouldPlayMusic = true;

		// Powers
		public SpecialPower ActiveSpecialPower = SpecialPower.None;
		public PowerPhase ActivePowerPhase = PowerPhase.None;
		public int PowerTimer = 0;

		// Graphics
		public Texture2D ArcadeTexture = null;
		public Texture2D Player2Texture = null;

		// Cheats
		private static int CheatQueueLength = 6;
		private readonly Queue<Keys> CheatQueue = new Queue<Keys>(capacity: ArcadeGame.CheatQueueLength);

		#endregion

		public ArcadeGame()
		{
			// Save previous music track
			const string noneMusicId = "none";
			this.MusicBeforePlaying = Game1.getMusicTrackName();
			if (string.IsNullOrWhiteSpace(this.MusicBeforePlaying))
				this.MusicBeforePlaying = noneMusicId;
			Game1.changeMusicTrack(newTrackName: noneMusicId);

			this.ArcadeTexture = this.LoadTextures();
			this.Player2Texture = this.LoadPlayer2Texture(player1Texture: this.ArcadeTexture);
		}

		public static void Start()
		{
			ArcadeGame game = new ArcadeGame();
			Helper.Events.GameLoop.UpdateTicked += game.LoadLate;
		}

		public void HandleCheatCodes(Keys k)
		{
			switch (k)
			{
				case Keys.D1:
					Log.D(this.Player.HealthCur < this.Player.Health
						? $"_health : {this.Player.HealthCur} -> {++this.Player.HealthCur}"       // Modifies health value
						: $"_health : {this.Player.HealthCur} == HealthMax");
					break;
				case Keys.D2:
					Log.D(this.Player.EnergyCur < this.Player.Energy
						? $"_energy : {this.Player.EnergyCur} -> {++this.Player.EnergyCur}"       // Modifies energy value
						: $"_energy : {this.Player.EnergyCur} == EnergyMax");
					break;
				case Keys.D3:
					Log.D($"_lives : {this.Player.Lives} -> {this.Player.Lives + 1}");
					break;
				case Keys.OemOpenBrackets:
					if (this.Scene is Stage stage)
					{
						Log.D($"_whichStage : {stage.Id} -> {stage.Id + 1}");
						stage.EndBehaviour?.Invoke();
					}
					break;
			}
		}

		#region Inherited methods

		public void receiveLeftClick(int x, int y, bool playSound = true)
		{
			this.Scene.HandleClick(x: x, y: y);
		}

		public void leftClickHeld(int x, int y)
		{
			try
			{
				this.receiveLeftClick(x, y);
			}
			catch (Exception e)
			{
				Log.E($"Error in button press for {this}:{e}");
				this.QuitMinigame();
				Game1.currentMinigame = null;
			}
		}

		public void receiveRightClick(int x, int y, bool playSound = true) { }

		public void releaseLeftClick(int x, int y)
		{
			try
			{
				this.Scene.HandleClickReleased(x: x, y: y);
			}
			catch (Exception e)
			{
				Log.E($"Error in button press for {this}:{e}");
				this.QuitMinigame();
				Game1.currentMinigame = null;
			}
		}

		public void releaseRightClick(int x, int y) { }

		public void receiveKeyPress(Keys k)
		{
			try
			{
				if (k == Keys.Escape)
				{
					// End minigame
					this.Player.HasPlayerQuit = true;
					return;
				}
				if (this.PowerTimer <= 0)
				{
					this.Player.HandleInput(k);
				}
				this.Scene.HandleInput(k: k);
			}
			catch (Exception e)
			{
				Log.E($"Error in key press for {this}:{e}");
				this.QuitMinigame();
				Game1.currentMinigame = null;
			}
		}

		public void receiveKeyRelease(Keys k)
		{
			try
			{
				if (this.PowerTimer <= 0)
				{
					this.Player.HandleInputReleased(k);
				}
				if (ArcadeGame.IsCheating)
				{
					this.HandleCheatCodes(k: k);
				}
				this.Scene.HandleInputReleased(k: k);
			} 
			catch (Exception e)
			{
				Log.E($"Error in key press for {this}:{e}");
				this.QuitMinigame();
				Game1.currentMinigame = null;
			}
		}

		public bool overrideFreeMouseMovement() { return Game1.options.SnappyMenus; }

		public void receiveEventPoke(int data) {}

		public string minigameId() { return ModEntry.ArcadeMinigameId; }

		public bool doMainGameUpdates() { return false; }

		public bool forceQuit() { return false; }

		public void unload()
		{
			// End minigame music
			Game1.stopMusicTrack(Game1.MusicContext.MiniGame);
		}

		public void changeScreenSize()
		{
			this.Viewport = new Rectangle(
				x: (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width - (ArcadeGame.Width * SpriteScale)) / 2,
				y: (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height - (ArcadeGame.Height * SpriteScale)) / 2,
				width: ArcadeGame.Width,
				height: ArcadeGame.Height);
		}

		public bool tick(GameTime time)
		{
			try
			{
				// Hide game cursor
				Game1.mouseCursorTransparency = 0;

				TimeSpan t = time.ElapsedGameTime;

				if (this.IsTimePassing)
				{
					if (this.Player.Update(t) == GameElement.State.IsDead)
					{
						return this.QuitMinigame();
					}
					if (this.Scene.Update(t) == GameElement.State.IsDead)
					{
						this.Scene.EndBehaviour?.Invoke();
					}
					return this.UI.Update(t) == GameElement.State.IsDead;
				}
			}
			catch (Exception e)
			{
				Log.E($"Error while updating {this}:{e}");
				this.QuitMinigame();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Inherited from IMinigame.
		/// Calls each render method from the minigame.
		/// </summary>
		public void draw(SpriteBatch b)
		{
			void start() => b.Begin(
				sortMode: SpriteSortMode.FrontToBack,
				blendState: BlendState.AlphaBlend,
				samplerState: SamplerState.PointClamp);

			try
			{
				start();
				this.Scene.Draw(b: b, viewport: this.Viewport);
				b.End();

				start();
				this.UI.Draw(b: b, viewport: this.Viewport, scene: this.Scene, player: this.Player, stats: this.Stats);
				b.End();

				// Draw cursor
				start();
				this.Draw(
					b: b,
					viewport: this.Viewport,
					position: this.GetViewportCursorPosition(),
					sourceRectangle: ArcadeGame.CrosshairDimen,
					origin: CrosshairDimen.Size.ToVector2() / 2);
				b.End();
			}
			catch (Exception e)
			{
				Log.E($"Error while rendering {this}:{e}");
				this.QuitMinigame();
				Game1.currentMinigame = null;
			}
		}

		#endregion

		public Texture2D LoadTextures()
		{
			Texture2D texture = ArcadeGame.Helper.GameContent.Load<Texture2D>(ModEntry.ArcadeSpritesAssetName);

			// Populate colour palette
			texture.GetData(
				level: 0,
				rect: new Rectangle(0, 0, this.ColourPalette.Length, 1),
				data: this.ColourPalette,
				startIndex: 0,
				elementCount: this.ColourPalette.Length);

			return texture;
		}

		public Texture2D LoadPlayer2Texture(Texture2D player1Texture)
		{
			// Load texture as a duplicate of the usual arcade game set, bottom cropped out
			Rectangle rect = new Rectangle(
				0,
				0,
				player1Texture.Width,
				PlayerLegsY + PlayerSplitWH);

			Colour[] pixels = new Colour[rect.Width * rect.Height];
			player1Texture.GetData(0, rect, pixels, 0, pixels.Length);
			
			// Swap out copy colours in the player sprites region with player 2's theme
			for (int y = HudPortraitSprite.Y; y < rect.Height; ++y)
			{
				for (int x = 0; x < rect.Width; ++x)
				{
					int i = x + y * rect.Width;
					if (pixels[i].A == 0)
						continue;
					if (pixels[i] == pixels[(int)PaletteColour.LightRed])
						pixels[i] = pixels[(int)PaletteColour.LightGreen];
					else if (pixels[i] == pixels[(int)PaletteColour.Red])
						pixels[i] = pixels[(int)PaletteColour.Green];
					else if (pixels[i] == pixels[(int)PaletteColour.DarkRed])
						pixels[i] = pixels[(int)PaletteColour.DarkGreen];
				}
			}

			// Copy new sprite set to player 2's draw texture
			Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, rect.Width, rect.Height);
			texture.SetData(pixels);
			return texture;
		}

		private void LoadLate(object sender, UpdateTickedEventArgs e)
		{
			Helper.Events.GameLoop.UpdateTicked -= this.LoadLate;

			Game1.currentMinigame = this;

			if (!(Game1.musicPlayerVolume > 0) || ModEntry.Config.DebugMode && !ModEntry.Config.DebugArcadeMusic)
				this.ShouldPlayMusic = false;

			// Load templates
			this.BulletTemplate = Helper.ModContent.Load<Dictionary<string, Bullet>>("ArcadeGunGame/Data/Bullet.json");
			this.MonsterTemplate = Helper.ModContent.Load<Dictionary<string, Monster>>("ArcadeGunGame/Data/Monster.json");
			this.ParticleTemplate = Helper.ModContent.Load<Dictionary<string, Particle>>("ArcadeGunGame/Data/Particle.json");
			this.PickupTemplate = Helper.ModContent.Load<Dictionary<string, Pickup>>("ArcadeGunGame/Data/Pickup.json");
			this.PlayerTemplate = Helper.ModContent.Load<Dictionary<string, Player>>("ArcadeGunGame/Data/Player.json");
			this.SceneTemplate = Helper.ModContent.Load<Dictionary<string, Scene>>("ArcadeGunGame/Data/Scene.json");
			this.StageTemplate = Helper.ModContent.Load<Dictionary<string, Stage>>("ArcadeGunGame/Data/Stage.json");
            //ArcadeGunGame.Template = Helper.ModContent.Load<Dictionary<string, >>("ArcadeGunGame/Data/.json");

            // Load base game objects
            this.Player = this.PlayerTemplate["V1"];
			this.Scene = null;
			this.UI = new UI();
			this.Stats = new Arcade.Objects.Stats();
			this.Music = null;
			this.Random = new Random(Seed: (int)(Game1.stats.DaysPlayed));

			// Open title scene
			this.Title();
			this.changeScreenSize();
		}

		public bool QuitMinigame()
		{
			Helper.Events.GameLoop.UpdateTicked -= this.LoadLate;
			this.StopMusic();
			Game1.changeMusicTrack(this.MusicBeforePlaying);
			return true;
		}

		public void StageEnding(Stage stage)
		{
			if (this.Player.Lives <= 0)
			{
				this.Scene = new Scenes.GameOver();
			}
			else
			{
				stage.Stats.AddTo(this.Stats);
			}
		}

		public void ResetGame()
		{
			this.StopMusic();

			// Reset player
			this.Player.Reset();
			this.Scene.Reset();
			this.Stats.Reset();

			this.ActiveSpecialPower = SpecialPower.None;
			this.ActivePowerPhase = PowerPhase.None;

			// Reduce game score
			this.Stats.Score /= 2;

			// Resume game
			this.IsTimePassing = true;
		}

		public void Title()
		{
			//this.PlayMusic(ModConsts.CueArcadeMenuMusic);
			this.Scene = new Scenes.Title()
			{
				EndBehaviour = () =>
				{
					Stage stage = this.StageTemplate["1-1"]; // todo: this will affect the TEMPLATE OBJECT. use copyto on live object
					stage.EndBehaviour = () => this.StageEnding(stage);
					this.Scene = stage;
				}
			};
			this.ResetGame();
		}

		public void PlaySound(string id)
		{
			if (!this.ShouldPlaySound || string.IsNullOrWhiteSpace(id))
				return;

			Game1.playSound(cueName: id);
		}

		public void PlayMusic(string id)
		{
			if (!this.ShouldPlayMusic || string.IsNullOrWhiteSpace(id))
				return;

			this.Music = Game1.soundBank.GetCue(name: id);
			this.Music?.Play();
		}

		public void StopMusic()
		{
			this.Music?.Stop(AudioStopOptions.AsAuthored);
			//Game1.stopMusicTrack(Game1.MusicContext.MiniGame);
		}

		public Vector2 GetViewportCursorPosition()
		{
			return (Helper.Input.GetCursorPosition().ScreenPixels - this.Viewport.Location.ToVector2() + new Vector2(8)) / SpriteScale;
		}

		public static Rectangle ScaleRectangle(Rectangle r, int scale)
		{
			return new Rectangle(x: r.X * scale, y: r.Y * scale, width: r.Width * scale, height: r.Height * scale);
		}

		public static Vector2 AlignToViewport(Rectangle viewport, Vector2 position, AlignX alignX, AlignY alignY)
		{
			Vector2 v = new Vector2(
				x: (viewport.X + (viewport.Width * (int)alignX / (int)AlignX.Right) + position.X),
				y: (viewport.Y + (viewport.Height * (int)alignY / (int)AlignY.Bottom) + position.Y));
			return v;
		}

		public static Rectangle AlignToViewport(Rectangle viewport, Rectangle area, AlignX alignX, AlignY alignY)
		{
			return new Rectangle(
				x: (viewport.X + (viewport.Width * (int)alignX / (int)AlignX.Right) + area.X),
				y: (viewport.Y + (viewport.Height * (int)alignY / (int)AlignY.Bottom) + area.Y),
				width: area.Width,
				height: area.Height);
		}

		public void DrawString(
			SpriteBatch b,
			Rectangle viewport,
			SpriteFont font,
			string text,
			Colour colour,
			Vector2 position,
			Vector2? origin = null,
			int scale = 1,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			SpriteEffects effects = SpriteEffects.None,
			float layerDepth = 1)
		{
			b.DrawString(
				spriteFont: font,
				text: text,
				position: ArcadeGame.AlignToViewport(viewport: viewport, position: position, alignX: alignX, alignY: alignY) * SpriteScale,
				color: colour,
				rotation: 0,
				origin: origin ?? Vector2.Zero,
				scale: scale,
				effects: effects,
				layerDepth: layerDepth);
		}

		public void DrawString(
			SpriteBatch b,
			Rectangle viewport,
			SpriteFont font,
			string text,
			PaletteColour colour,
			Vector2 position,
			Vector2? origin = null,
			int scale = 1,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			SpriteEffects effects = SpriteEffects.None,
			float layerDepth = 1)
		{
			b.DrawString(
				spriteFont: font,
				text: text,
				position: ArcadeGame.AlignToViewport(viewport: viewport, position: position, alignX: alignX, alignY: alignY) * SpriteScale,
				color: this.ColourPalette[(int)colour],
				rotation: 0,
				origin: origin ?? Vector2.Zero,
				scale: scale,
				effects: effects,
				layerDepth: layerDepth);
		}

		public void DrawColour(
			SpriteBatch b,
			Colour colour,
			Rectangle? area = null,
			Vector2? origin = null,
			float layerDepth = 1)
		{
			b.Draw(
				texture: Game1.staminaRect,
				sourceRectangle: Game1.staminaRect.Bounds,
				destinationRectangle: area ?? Game1.staminaRect.Bounds,
				color: colour,
				rotation: 0,
				origin: origin ?? Vector2.Zero,
				effects: SpriteEffects.None,
				layerDepth: layerDepth);
		}

		public void DrawColour(
			SpriteBatch b,
			PaletteColour colour,
			Rectangle? area = null,
			Vector2? origin = null,
			float layerDepth = 1)
		{
			b.Draw(
				texture: Game1.staminaRect,
				destinationRectangle: area ?? Game1.staminaRect.Bounds,
				sourceRectangle: Game1.staminaRect.Bounds,
				color: this.ColourPalette[(int)colour],
				rotation: 0,
				origin: origin ?? Vector2.Zero,
				effects: SpriteEffects.None,
				layerDepth: layerDepth);
		}

		public void DrawColour(
			SpriteBatch b,
			Rectangle viewport,
			Colour colour,
			Rectangle? area = null,
			Vector2? origin = null,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			float layerDepth = 1)
		{
			area = ArcadeGame.AlignToViewport(
				viewport: viewport,
				area: area ?? viewport,
				alignX: alignX,
				alignY: alignY);
			b.Draw(
				texture: Game1.staminaRect,
				sourceRectangle: Game1.staminaRect.Bounds,
				destinationRectangle: ArcadeGame.ScaleRectangle(r: area.Value, scale: SpriteScale),
				color: colour,
				rotation: 0,
				origin: origin ?? Vector2.Zero,
				effects: SpriteEffects.None,
				layerDepth: layerDepth);
		}

		public void DrawColour(
			SpriteBatch b,
			Rectangle viewport,
			PaletteColour colour,
			Rectangle? area = null,
			Vector2? origin = null,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			float layerDepth = 1)
		{
			b.Draw(
				texture: Game1.fadeToBlackRect,
				destinationRectangle: area.HasValue
					? ArcadeGame.ScaleRectangle(r: ArcadeGame.AlignToViewport(viewport: viewport, area: area.Value, alignX: alignX, alignY: alignY), scale: SpriteScale)
					: viewport,
				sourceRectangle: Game1.staminaRect.Bounds,
				color: this.ColourPalette[(int)colour],
				rotation: 0,
				origin: origin ?? Vector2.Zero,
				effects: SpriteEffects.None,
				layerDepth: layerDepth);
		}

		public void Draw(
			SpriteBatch b,
			Rectangle viewport,
			Rectangle area,
			Rectangle sourceRectangle,
			Vector2? origin = null,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			Texture2D texture = null,
			Colour? color = null,
			SpriteEffects? effects = null,
			float layerDepth = 1)
		{
			area = ArcadeGame.AlignToViewport(
				viewport: viewport,
				area: area,
				alignX: alignX,
				alignY: alignY);
			b.Draw(
				texture: texture ?? this.ArcadeTexture,
				destinationRectangle: ArcadeGame.ScaleRectangle(r: area, scale: SpriteScale),
				sourceRectangle: sourceRectangle,
				color: color ?? Colour.White,
				rotation: 0,
				origin: origin ?? Vector2.Zero,
				effects: effects ?? SpriteEffects.None,
				layerDepth: layerDepth);
		}

		public void Draw(
			SpriteBatch b,
			Rectangle viewport,
			Vector2 position,
			Rectangle sourceRectangle,
			Vector2? origin = null,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			Texture2D texture = null,
			Colour? colour = null,
			SpriteEffects? effects = null,
			float layerDepth = 1)
		{
			var pos = ArcadeGame.AlignToViewport(viewport: viewport, position: position, alignX: alignX, alignY: alignY) * SpriteScale;
			b.Draw(
				texture: texture ?? this.ArcadeTexture,
				position: pos,
				sourceRectangle: sourceRectangle,
				color: colour ?? Colour.White,
				rotation: 0,
				origin: origin ?? Vector2.Zero,
				scale: SpriteScale,
				effects: effects ?? SpriteEffects.None,
				layerDepth: layerDepth);
		}
	}

	#region Nice code

	/*
		// Crop.harvest() -- Nice for delayed sounds and flow
		if ((int)harvestMethod == 1)
		{
			if (junimoHarvester != null)
			{
				DelayedAction.playSoundAfterDelay("daggerswipe", 150, junimoHarvester.currentLocation);
			}
			if (junimoHarvester != null && Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), 64, junimoHarvester.currentLocation))
			{
				junimoHarvester.currentLocation.playSound("harvest");
			}
			if (junimoHarvester != null && Utility.isOnScreen(junimoHarvester.getTileLocationPoint(), 64, junimoHarvester.currentLocation))
			{
				DelayedAction.playSoundAfterDelay("coin", 260, junimoHarvester.currentLocation);
			}
			if (junimoHarvester != null)
			{
				junimoHarvester.tryToAddItemToHut(harvestedItem2.getOne());
			}
			else
			{
				Game1.createItemDebris(harvestedItem2.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
			}
			success = true;
		}


		// AbigailGame.tick() -- Nice for collisions
		if ((double) Utility.distance((float) this.playerBoundingBox.Center.X, 
		(float) (AbigailGame.powerups[index].position.X + AbigailGame.TD / 2), 
		(float) this.playerBoundingBox.Center.Y, 
		(float) (AbigailGame.powerups[index].position.Y + AbigailGame.TD / 2)) <= (double) (AbigailGame.TD + 3) 
		&& (AbigailGame.powerups[index].position.X < AbigailGame.TD 
		|| AbigailGame.powerups[index].position.X >= 16 * AbigailGame.TD - AbigailGame.TD 
		|| (AbigailGame.powerups[index].position.Y < AbigailGame.TD 
		|| AbigailGame.powerups[index].position.Y >= 16 * AbigailGame.TD - AbigailGame.TD)))
		{
		if (AbigailGame.powerups[index].position.X + AbigailGame.TD / 2 < this.playerBoundingBox.Center.X)
			++AbigailGame.powerups[index].position.X;
		if (AbigailGame.powerups[index].position.X + AbigailGame.TD / 2 > this.playerBoundingBox.Center.X)
			--AbigailGame.powerups[index].position.X;
		if (AbigailGame.powerups[index].position.Y + AbigailGame.TD / 2 < this.playerBoundingBox.Center.Y)
			++AbigailGame.powerups[index].position.Y;
		if (AbigailGame.powerups[index].position.Y + AbigailGame.TD / 2 > this.playerBoundingBox.Center.Y)
			--AbigailGame.powerups[index].position.Y;
		}
	*/

	#endregion
}
