using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Minigames;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Locations;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa
{
	internal class ArcadeGunGame : IMinigame
	{
		private static readonly IModHelper Helper = ModEntry.Instance.Helper;
		private static readonly bool IsDebugMode = ModEntry.Instance.Config.DebugMode;


		///////////////////////
		#region Constant Values
		///////////////////////


		/* Enum values */

		// Game common attributes
		public enum MenuOption
		{
			Retry,
			Quit
		}
		public enum Move
		{
			None,
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
		private const int TimeToSpecialPhase1 = 1000; // stand ahead
		private const int TimeToSpecialPhase2 = TimeToSpecialPhase1 + 1400; // raise compact

		// World special power effects
		private const int TimeToPowerPhase1 = 1500; // activation
		private const int TimeToPowerPhase2 = TimeToPowerPhase1 + 3500; // white glow
		private const int TimeToPowerPhase3 = TimeToPowerPhase2 + 1500; // cooldown

		private static readonly Dictionary<PowerPhase, int> PowerPhaseDurations = new Dictionary<PowerPhase, int>
		{
			{ PowerPhase.None, 400 },
			{ PowerPhase.BeforeActive1, 200 },
			{ PowerPhase.BeforeActive2, 400 },
			{ PowerPhase.Active1, 1000 },
			{ PowerPhase.Active2, 500 },
			{ PowerPhase.Active3, 500 },
			{ PowerPhase.Active4, 250 },
			{ PowerPhase.AfterActive1, 400 },
			{ PowerPhase.AfterActive2, 400 },
		};
		public enum PowerAnimationFrameGroup
		{
			None = 0,
			Normal = 0,
			Megaton = 0,
			Sulphur = 1,
			Incense = 1
		}
		// Bullets and projectiles
		public enum BulletType
		{
			None,
			Player, // players
			Gun,
			Bomb, // no collision, different arc, explodes on death
			Energy,
			Petal, // menu screen effects
		}
		private static readonly Dictionary<BulletType, int> BulletSize = new Dictionary<BulletType, int>
		{
			{BulletType.None, 0},
			{BulletType.Player, TD},
			{BulletType.Gun, TD / 2},
			{BulletType.Bomb, 0},
			{BulletType.Energy, TD},
			{BulletType.Petal, TD},
		};
		private static readonly Dictionary<BulletType, Vector2> BulletSpeed = new Dictionary<BulletType, Vector2>
		{
			{BulletType.None, Vector2.Zero},
			{BulletType.Player, Vector2.One * 3f * SpriteScale},
			{BulletType.Gun, Vector2.One * 2f * SpriteScale},
			{BulletType.Bomb, Vector2.One * 1.5f * SpriteScale},
			{BulletType.Energy, Vector2.One * 3f * SpriteScale},
			{BulletType.Petal, Vector2.One * 3f * SpriteScale},
		};
		private static readonly Dictionary<BulletType, float> BulletSpin = new Dictionary<BulletType, float>
		{ // todo: add values in radians
			{BulletType.None, 0f},
			{BulletType.Player, 0f},
			{BulletType.Gun, 0f},
			{BulletType.Bomb, 0f},
			{BulletType.Energy, 0f},
		};
		private static readonly Dictionary<BulletType, int[]> BulletAnimations = new Dictionary<BulletType, int[]>
		{
			{ BulletType.None, new []{ 0 } },
			{ BulletType.Player, new []{ 0 } },
			{ BulletType.Gun, new []{ 0 } },
			{ BulletType.Bomb, new []{ 0 } },
			{ BulletType.Energy, new []{ 0 } },
			{ BulletType.Petal, new []{ 0, 1, 0, 2 } },
		};
		// Loot drops and powerups
		public enum LootDrops
		{
			None,
			Life,
			Energy,
			Cake,
			Time
		}
		private static readonly Dictionary<LootDrops, double> LootRollGets = new Dictionary<LootDrops, double>
		{
			{LootDrops.None, 0},
			{LootDrops.Life, 0.1d},
			{LootDrops.Energy, 0.2d},
			{LootDrops.Cake, 0.5d},
			{LootDrops.Time, 0.9d}
		};
		private static readonly Dictionary<LootDrops, int> LootDurations = new Dictionary<LootDrops, int>
		{
			{LootDrops.None, 0},
			{LootDrops.Life, 4},
			{LootDrops.Energy, 3},
			{LootDrops.Cake, 5},
			{LootDrops.Time, 5}
		};
		// Monsters
		public enum MonsterSpecies
		{
			Mafia,
		}
		private static readonly Dictionary<MonsterSpecies, int> MonsterHealth = new Dictionary<MonsterSpecies, int>
		{
			{MonsterSpecies.Mafia, 1},
		};
		private static readonly Dictionary<MonsterSpecies, int> MonsterSpeed = new Dictionary<MonsterSpecies, int>
		{
			{MonsterSpecies.Mafia, 5},
		};
		private static readonly Dictionary<MonsterSpecies, int> MonsterPower = new Dictionary<MonsterSpecies, int>
		{
			{MonsterSpecies.Mafia, 1},
		};
		private static readonly Dictionary<MonsterSpecies, bool> MonsterIsFlying = new Dictionary<MonsterSpecies, bool>
		{
			{MonsterSpecies.Mafia, false},
		};
		private static readonly Dictionary<MonsterSpecies, int> MonsterBullets = new Dictionary<MonsterSpecies, int>
		{
			{MonsterSpecies.Mafia, 1},
		};
		private static readonly Dictionary<MonsterSpecies, bool> MonsterFireStaggered = new Dictionary<MonsterSpecies, bool>
		{
			{MonsterSpecies.Mafia, false},
		};
		private static readonly Dictionary<MonsterSpecies, int> MonsterScore = new Dictionary<MonsterSpecies, int>
		{
			{MonsterSpecies.Mafia, 150},
		};
		private static readonly Dictionary<MonsterSpecies, int> MonsterAimWidth = new Dictionary<MonsterSpecies, int>
		{
			{MonsterSpecies.Mafia, 32 * SpriteScale},
		};
		private static readonly Dictionary<MonsterSpecies, int> MonsterFireTime = new Dictionary<MonsterSpecies, int>
		{
			{MonsterSpecies.Mafia, 1600},
		};
		private static readonly Dictionary<MonsterSpecies, int> MonsterIdleTime = new Dictionary<MonsterSpecies, int>
		{
			{MonsterSpecies.Mafia, 1600},
		};
		private static readonly Dictionary<MonsterSpecies, bool> MonsterFiringCancellable = new Dictionary<MonsterSpecies, bool>
		{
			{MonsterSpecies.Mafia, true},
		};
		private static readonly Dictionary<MonsterSpecies, double> MonsterLootRates = new Dictionary<MonsterSpecies, double>
		{
			{MonsterSpecies.Mafia, 0.5d},
		};

		/* Game attributes */

		// Game values
		private const int GameLivesDefault = 1;
		private const int GameEnergyThresholdLow = 3;
		private const int GameDodgeDelay = 500;
		private const int GameFireDelay = 20;
		private const int GameInvincibleDelay = 5000;
		private const int GameDeathDelay = 3000;
		private const int GameEndDelay = 5000;
		private const int StageTimeMax = 99000;
		private const int StageTimeInitial = 60000;
		private const int StageTimeExtra = 15000;
		private const int StageTimeCritical = 5000;
		private const int StageTimeHudDigits = 2;

		// Score values
		private const int ScoreMax = 99999999;
		private const int ScoreCake = 5000;
		private const int ScoreCakeExtra = 150;
		private const int ScoreBread = 12500;

		/* Sprite attributes */

		// Sprite dimensions and size multipliers
		private const int TD = 16;
		private const int SpriteScale = 2;
		private const int CursorScale = 4;
		// Animation rates
		private const int UiAnimTimescale = 85;
		private const int PlayerAnimTimescale = 200;
		private const int EnemyAnimTimescale = 200;
		// Boundary dimensions for gameplay, menus, and cutscenes
		private const int MapWidthInTiles = 20;
		private const int MapHeightInTiles = 18;

		/* Cutscene and unique animation times */

		// Title screen, splash screen and standby
		private const int TimeToTitleBeforeWhite = 800; // blank before light shaft
		private const int TimeToTitleAfterWhite = TimeToTitleBeforeWhite + 1200; // blank after light shaft
		private const int TimeToTitlePhase1 = TimeToTitleAfterWhite + 750; // red V/
		private const int TimeToTitlePhase2 = TimeToTitlePhase1 + 400; // コードネームは
		private const int TimeToTitlePhase3 = TimeToTitlePhase2 + 400; // セーラー
		private const int TimeToTitlePhase4 = TimeToTitlePhase3 + 400; // © BLUEBERRY 1996
		private const int TimeToTitlePhase5 = TimeToTitlePhase4 + 400; // fire to start
		private const int TimeToBlinkPrompt = 600; // fire to start blink period

		// HUD graphics

		// player crosshair
		public static readonly Rectangle CrosshairDimen = new Rectangle( // HARD Y
			TD * 2, 0, TD, TD);

		// text strings
		private static readonly Rectangle HudDigitSprite = new Rectangle( // HARD Y
			0, TD * 6, TD, TD);
		private static readonly int HudStringsY = HudDigitSprite.Y + HudDigitSprite.Height;
		private static readonly int HudStringsH = TD * 1;

		/* Game object graphics */

		// cakes and sweets
		private const int CakesFrames = 9;
		public static readonly Rectangle CakeSprite = new Rectangle( // HARD Y
			0, TD * 1, TD, TD);
		// projectiles
		private const int ProjectileVariants = 2;
		public static readonly Dictionary<BulletType, Rectangle> ProjectileSrcRects = new Dictionary<BulletType, Rectangle>
		{
			{ BulletType.None, new Rectangle(0, TD * 4, TD, TD) }, 
			{ BulletType.Player, new Rectangle(0, TD * 4, TD, TD) },
			{ BulletType.Gun, new Rectangle(0, TD * 4, TD, TD) },
			{ BulletType.Bomb, new Rectangle(0, TD * 4, TD, TD) },
			{ BulletType.Energy, new Rectangle(0, TD * 4, TD, TD) },
			{ BulletType.Petal, new Rectangle(0, TD * 4, TD, TD) },
		};
		// monsters
		private const int EnemyRunFrames = 3;
		private static readonly Dictionary<MonsterSpecies, Rectangle> MonsterSrcRects = new Dictionary<MonsterSpecies, Rectangle>
		{
			{MonsterSpecies.Mafia, new Rectangle(0, TD * 24, TD * 2, TD * 3)},
		};

		/* Player graphics */

		// Shared attributes
		private const int PlayerW = TD * 2;
		private const int PlayerX = 0;
		// Full-body sprites (body, arms and legs combined)
		private const int PlayerFullH = TD * 3;
		private const int PlayerFullY = TD * 8; // HARD Y
		// Split-body sprites (body, arms or legs individually)
		private const int PlayerSplitWH = TD * 2;

		// Full-body pre-special power pose
		private const int PlayerPoseFrames = 3;
		private const int PlayerPoseX = PlayerX + PlayerW * PlayerIdleFrames;
		// Full-body special power windup
		private const int PlayerSpecialFrames = 2;
		private const int PlayerSpecialX = PlayerPoseX + PlayerW * PlayerPoseFrames;
		// Full-body special power activated
		private const int PlayerPowerX = PlayerSpecialX + PlayerW * PlayerSpecialFrames;
		private const int PlayerPowerFrames = 3;

		// Split-body leg frames
		private const int PlayerIdleFrames = 2;
		private const int PlayerLegsIdleX = PlayerX;
		private const int PlayerRunFrames = 4;
		private const int PlayerLegsRunX = PlayerLegsIdleX + PlayerW * PlayerIdleFrames;
		// Split-body body frames
		private const int PlayerBodyY = PlayerFullY + PlayerFullH;
		private const int PlayerBodySideFireX = PlayerX;
		private const int PlayerBodyUpFireX = PlayerBodySideFireX + PlayerSplitWH;
		private const int PlayerBodyRunX = PlayerBodyUpFireX + PlayerSplitWH;
		private const int PlayerBodyRunFireX = PlayerBodyRunX + PlayerSplitWH * PlayerRunFrames;
		// Split-body arm frames
		private const int PlayerLegsY = PlayerBodyY + PlayerSplitWH;
		private const int PlayerArmsX = PlayerLegsRunX + PlayerW * PlayerRunFrames;
		private const int PlayerArmsY = PlayerLegsY;
		// Special powers
		private const int PowerFxY = TD * 5; // HARD Y
		// Shadows
		private static readonly Rectangle ActorShadowRect = new Rectangle(
			CakeSprite.X + CakeSprite.Width * CakesFrames,
			CakeSprite.Y,
			TD * 2,
			TD);
		private static readonly Rectangle LootShadowRect = new Rectangle(
			ActorShadowRect.X + ActorShadowRect.Width,
			ActorShadowRect.Y,
			TD,
			TD);

		/* Title screen graphics */

		// Fire to start
		// above full sailor frames
		private static readonly Rectangle TitleFireToStartSprite = new Rectangle( // HARD Y
			0,
			TD * 7,
			TD * 6,
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
		// © テレビ望月・東映動画 / © BLUEBERRY 1996
		// beneath sailor text
		private static readonly Rectangle TitleSignatureSprite = new Rectangle(
			0,
			TitleSailorSprite.Y + TitleSailorSprite.Height,
			TD * 8,
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
		private const int TitleLightShaftW = SpriteScale * 8;
		private static readonly Rectangle TitleWhiteBannerSprite = new Rectangle(
			TitleRedBannerSprite.X + TitleRedBannerSprite.Width,
			TitleRedBannerSprite.Y,
			TitleRedBannerSprite.Width + TD * 2,
			TitleRedBannerSprite.Height);

		/* HUD graphics */

		// Player score
		private static readonly Rectangle HudScoreTextSprite = new Rectangle(
			0, HudStringsY, TD * 1, HudStringsH);
		// Player portrait
		private static readonly Rectangle HudPortraitSprite = new Rectangle( // HARD Y
			0, TD * 2, TD * 2, TD * 2);
		// Player life
		private static readonly Rectangle HudLifeSprite = new Rectangle( // HARD Y
			TD * 12, 0, TD, TD);
		// Player energy
		private static readonly Rectangle HudEnergySprite = new Rectangle( // HARD Y
			HudLifeSprite.X + HudLifeSprite.Width, 0, TD, TD);

		/* Cutscene graphics */

		private const float AnimCutsceneBackgroundSpeed = 0.1f;

		#endregion


		//////////////////////////
		#region Gameplay Variables
		//////////////////////////


		/* Minigame meta attributes */

		private static Rectangle _gamePixelDimen;
		private static Point _gameEndPixel;
		private static bool _playMusic = true;
		private behaviorAfterMotionPause _behaviorAfterPause;
		public delegate void behaviorAfterMotionPause();

		private static readonly Texture2D BlackoutPixel = new Texture2D(
			Game1.graphics.GraphicsDevice, 1, 1);
		private static readonly Texture2D ColorFillPixel = new Texture2D(
			Game1.graphics.GraphicsDevice, 1, 1);

		/* Game actors and objects */

		private static Player _player;
		private static List<Bullet> _playerBullets = new List<Bullet>();
		private static List<Bullet> _enemyBullets = new List<Bullet>();
		private static List<Monster> _enemies = new List<Monster>();
		private static List<TemporaryAnimatedSprite> _temporaryAnimatedSprites = new List<TemporaryAnimatedSprite>();
		private static List<Powerup> _powerups = new List<Powerup>();
		private static List<Point>[] _spawnQueue = new List<Point>[4];
		private static int[,] _stageMap = new int[MapHeightInTiles, MapWidthInTiles];

		/* Game state */

		private static int _whichStage;
		private static int _whichWorld;
		private static int _whichPlaythrough;
		private static int _stageMilliseconds;
		private static int _totalTime;
		private static int _totalScore;
		private static int _totalShotsSuccessful;
		private static int _totalShotsFired;
		private static int _totalMonstersBonked;
		private static int _gameOverOption;

		/* Music cues */

		private static ICue gameMusic;

		/* HUD graphics */

		private static Texture2D _arcadeTexture;
		private static Color _screenFlashColor;
		private static float _cutsceneBackgroundPosition;
		// Player score
		private static int _hudScoreDstX;
		private static int _hudScoreDstY;
		// Player portrait
		private static int _hudPortraitDstX;
		private static int _hudPortraitDstY;
		// Player life
		private static int _hudLifeDstX;
		private static int _hudLifeDstY;
		// Player energy
		private static int _hudEnergyDstX;
		private static int _hudEnergyDstY;
		// Game world and stage
		private static int _hudWorldDstX;
		private static int _hudWorldDstY;
		// Stage timer
		private static int _hudTimeDstX;
		private static int _hudTimeDstY;

		/* Timers and countdowns */

		// gameplay
		// endgame
		private static int _gameRestartTimer;
		private static int _gameEndTimer;
		// cutscenes
		private static int _screenFlashTimer;
		private static int _cutsceneTimer;
		private static int _cutscenePhase;
		// menus
		private static bool _onTitleScreen;
		private static bool _onGameOver;
		private static bool _onWorldComplete;
		private static bool _onGameComplete;

		#endregion


		public ArcadeGunGame()
		{
			changeScreenSize();

			if (ModEntry.Instance.Config.DebugMode && !ModEntry.Instance.Config.DebugArcadeMusic)
				_playMusic = false;

			Game1.changeMusicTrack("none", false, Game1.MusicContext.MiniGame);

			// Load arcade game assets
			BlackoutPixel.SetData(new [] { Color.Black });
			ColorFillPixel.SetData(new [] { Color.Bisque });
			_arcadeTexture = Helper.Content.Load<Texture2D>(
				Path.Combine("assets", ModConsts.SpritesDirectory, 
					$"{ModConsts.ArcadeSpritesFile}.png"));
			// Reload assets customised by the arcade game
			// ie. LooseSprites/Cursors
			Helper.Events.GameLoop.UpdateTicked += InvalidateCursorsOnNextTick;

			// Init game statistics
			_totalShotsSuccessful = 0;
			_totalShotsFired = 0;
			_totalTime = 0;

			// Go to the title screen
			if (IsDebugMode && !ModEntry.Instance.Config.DebugArcadeSkipIntro)
				ResetAndReturnToTitle();
			else
				ResetGame();
		}

		#region Player input methods

		public void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (_onTitleScreen)
				_cutscenePhase++; // Progress through the start menu cutscene
			if (_cutsceneTimer <= 0
			    && _player.PowerTimer <= 0
			    && _player.RespawnTimer <= 0 && _gameEndTimer <= 0 && _gameRestartTimer <= 0
			    && _player.FireTimer <= 1)
			{
				// Position the target on the centre of the cursor
				var target = new Vector2(
					Helper.Input.GetCursorPosition().ScreenPixels.X + TD / 2 * CursorScale,
					Helper.Input.GetCursorPosition().ScreenPixels.Y + TD / 2 * CursorScale);
				_player.Fire(target); // Fire lightgun trigger
			}
		}

		public void leftClickHeld(int x, int y) { receiveLeftClick(x, y); }

		public void receiveRightClick(int x, int y, bool playSound = true) { }

		public void releaseLeftClick(int x, int y) { }

		public void releaseRightClick(int x, int y) { }

		public void receiveKeyPress(Keys k)
		{	
			var flag = false;
			if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k)
			    && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, Keys.Left))
			{
				// Move left
				_player.AddMovementDirection(Move.Left);
				flag = true;
			}
			if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k)
			    && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
			{
				// Move right
				_player.AddMovementDirection(Move.Right);
				flag = true;
			}
			if (flag)
				return;
			
			if (IsDebugMode && ModEntry.Instance.Config.DebugArcadeCheats) {
				switch (k)
				{
					case Keys.D1:
						Log.D(_player.Health < _player.HealthMax
							? $"_health : {_player.Health} -> {++_player.Health}"		// Modifies health value
							: $"_health : {_player.Health} == HealthMax");
						break;
					case Keys.D2:
						Log.D(_player.Energy < _player.EnergyMax
							? $"_energy : {_player.Energy} -> {++_player.Energy}"		// Modifies energy value
							: $"_energy : {_player.Energy} == EnergyMax");
						break;
					case Keys.D3:
						Log.D($"_lives : {_player.Lives} -> {_player.Lives + 1}");
						break;
					case Keys.OemOpenBrackets:
						Log.D($"_whichStage : {_whichStage} -> {_whichStage + 1}");
						EndCurrentStage();
						break;
					case Keys.OemCloseBrackets:
						Log.D($"_whichWorld : {_whichWorld} -> {_whichWorld + 1}");
						EndCurrentWorld();
						break;
				}
			}

			switch (k)
			{
				case Keys.Enter:
				case Keys.Space:
				case Keys.X:
					// Special power trigger
					if (_player.PowerTimer <= 0 
					    && _player.Energy >= GameEnergyThresholdLow)
					{
						_player.SpriteMirror = SpriteEffects.None;
						_player.PowerBeforeActive();
					}
					break;
				case Keys.Escape:
					// End minigame
					_player.HasPlayerQuit = true;
					break;
				case Keys.A:
					// Move left
					_player.AddMovementDirection(Move.Left);
					_player.AnimationTimer = 0;
					break;
				case Keys.D:
					// Move right
					_player.AddMovementDirection(Move.Right);
					_player.AnimationTimer = 0;
					break;
			}
		}

		public void receiveKeyRelease(Keys k)
		{
			// Accept new input
			if (k != Keys.None)
			{
				const Keys keys = Keys.Down;
				if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k))
					// Move right
					k = Keys.D;
				else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k))
					// Move left
					k = Keys.A;
				else if (Game1.options.doesInputListContain(Game1.options.actionButton, k))
					// Dodge
					k = keys;
			}
			// Otherwise clear old input
			else
			{
				var flag = false;
				if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k)
				    && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, Keys.Left))
				{
					if (_player.MovementDirections.Contains(Move.Right))
					{
						_player.MovementDirections.Remove(Move.Right);
					}
					flag = true;
				}
				if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k)
				    && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
				{
					if (_player.MovementDirections.Contains(Move.Left))
					{
						_player.MovementDirections.Remove(Move.Left);
					}
					flag = true;
				}
				if (flag)
					return;
			}

			// Update inputs
			
			if (k == Keys.A)
			{
				// Move left
				if (!_player.MovementDirections.Contains(Move.Left))
					return;
				_player.MovementDirections.Remove(Move.Left);
			}
			else if (k == Keys.D)
			{
				// Move right
				if (!_player.MovementDirections.Contains(Move.Right))
					return;
				_player.MovementDirections.Remove(Move.Right);
			}
		}

		#endregion

		#region Inherited methods

		public bool overrideFreeMouseMovement() { return Game1.options.SnappyMenus; }

		public void receiveEventPoke(int data) { }

		public string minigameId() { return ModConsts.ArcadeMinigameId; }

		public bool doMainGameUpdates() { return false; }

		public bool forceQuit() { return false; }

		#endregion

		#region Minigame manager methods

		private static void InvalidateCursorsOnNextTick(object sender, UpdateTickedEventArgs e)
		{
			Helper.Events.GameLoop.UpdateTicked -= InvalidateCursorsOnNextTick;
			Helper.Content.InvalidateCache("LooseSprites/Cursors");
		}

		public void changeScreenSize()
		{
			// Initialise pixel dimension bounds for game drawing
			_gamePixelDimen.X =
				(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width - MapWidthInTiles * TD * SpriteScale) / 2;
			_gamePixelDimen.Y = 
				(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height - MapHeightInTiles * TD * SpriteScale) / 2;
			_gameEndPixel = new Point(
				_gamePixelDimen.X + MapWidthInTiles * TD * SpriteScale,
				_gamePixelDimen.Y + MapHeightInTiles * TD * SpriteScale);
			_gamePixelDimen.Width = _gameEndPixel.X - _gamePixelDimen.X;
			_gamePixelDimen.Height = _gameEndPixel.Y - _gamePixelDimen.Y;

			// Initialise HUD element positions
			// Player score
			_hudScoreDstX = _gamePixelDimen.X;
			_hudScoreDstY = _gamePixelDimen.Y - HudDigitSprite.Height * SpriteScale;
			// Player portrait
			_hudPortraitDstX = _gamePixelDimen.X;
			_hudPortraitDstY = _gameEndPixel.Y;
			// Player life
			_hudLifeDstX = _hudPortraitDstX + HudPortraitSprite.Width * SpriteScale;
			_hudLifeDstY = _hudPortraitDstY;
			// Player energy
			_hudEnergyDstX = _hudLifeDstX;
			_hudEnergyDstY = _hudLifeDstY + HudLifeSprite.Height * SpriteScale;
			// Game world and stage
			_hudWorldDstX = _gameEndPixel.X;
			_hudWorldDstY = _gameEndPixel.Y;
			// Stage time remaining
			_hudTimeDstX = _gameEndPixel.X;
			_hudTimeDstY = _gamePixelDimen.Y - HudDigitSprite.Height * SpriteScale;
			
			Log.D("_gamePixelDimen:\n"
				  + $"({Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width} - {MapWidthInTiles * TD * SpriteScale}) / 2, "
			      + $"({Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height} - {MapHeightInTiles * TD * SpriteScale}) / 2\n"
			      + $"x = {_gamePixelDimen.X}, y = {_gamePixelDimen.Y}"
				  + $", w = {_gamePixelDimen.Width}, h = {_gamePixelDimen.Height}",
				IsDebugMode);
			Log.D("_gamePixelDimen.Center:\n"
			      + $"{_gamePixelDimen.Center.ToString()}\n",
				IsDebugMode);
			Log.D("GameEndCoords:\n"
			      + $"{_gamePixelDimen.X} + {MapWidthInTiles * TD} * {SpriteScale}\n"
			      + $"= {_gameEndPixel.X}, {_gameEndPixel.Y}",
				IsDebugMode);
		}

		private static bool QuitMinigame()
		{
			if (Game1.currentMinigame == null && Game1.IsMusicContextActive(Game1.MusicContext.MiniGame))
			{
				if (gameMusic != null && gameMusic.IsPlaying)
					gameMusic.Stop(AudioStopOptions.Immediate);
				Game1.stopMusicTrack(Game1.MusicContext.MiniGame);
			}
			if (Game1.currentLocation != null
			    && Game1.currentLocation.Name.Equals((object)"Saloon") && Game1.timeOfDay >= 1700)
				Game1.changeMusicTrack("Saloon1");
			Game1.currentMinigame = null;
			Helper.Content.InvalidateCache("LooseSprites/Cursors");
			return true;
		}

		public void unload()
		{
			_player.Reset();
			Game1.stopMusicTrack(Game1.MusicContext.MiniGame);
		}

		/// <summary>
		/// Starts running down the clock to return to the title screen after resetting the game state.
		/// </summary>
		private static void GameOver()
		{
			_onGameOver = true;
			_gameRestartTimer = 2000;

			// todo: ingame event involving arcade gungame
			if (Game1.currentLocation.currentEvent != null)
				++Game1.currentLocation.currentEvent.CurrentCommand;
		}

		/// <summary>
		/// Flags for  to title screen start prompt
		/// </summary>
		private static void ResetAndReturnToTitle()
		{
			ResetGame();
			_onTitleScreen = true;
			PlayMusic(gameMusic, "dog_bark");
		}

		/// <summary>
		/// Completely resets all game actors, objects, players, and world state.
		/// Returns to the title screen.
		/// </summary>
		private static void ResetGame()
		{
			// Reset player
			_player = new Player();

			// Reset game actors and objects
			_enemyBullets.Clear();
			_enemies.Clear();
			_temporaryAnimatedSprites.Clear();

			// Reset cutscene and menu timers
			_cutsceneTimer = 0;
			_cutscenePhase = 0;
			_cutsceneBackgroundPosition = 0f;

			// Shaft the players' score
			AddScore(-1 * _totalScore / 2);

			// Reset game music
			gameMusic = null;

			// Reset the game world
			ResetWorld();
		}

		private static void ResetWorld()
		{
			_powerups.Clear();
			_enemies.Clear();
			_enemyBullets.Clear();
			_playerBullets.Clear();

			_whichStage = 0;
			_whichWorld = 0;
			_whichPlaythrough = 0;
			_onGameOver = false;
		}

		#endregion

		#region Minigame progression methods

		private void EndCurrentStage()
		{
			_player.MovementDirections.Clear();

			// todo: set cutscenes, begin events, etc
			// todo: add stage time to score
		}

		private void EndCurrentWorld()
		{
			// todo: set cutscenes, begin events, etc
		}

		private static void PlayMusic(ICue cue, string id)
		{
			if (!_playMusic) return;

			cue = Game1.soundBank.GetCue(id);
			cue.Play();
			Game1.musicPlayerVolume = Game1.options.musicVolumeLevel;
			Game1.musicCategory.SetVolume(Game1.musicPlayerVolume);
		}

		private static void StartNewStage()
		{
			++_whichStage;
			_stageMap = GetMap(_whichStage);
			// todo: set timer per stage/world/boss
			_stageMilliseconds = StageTimeInitial;

			//Log.D($"Current play/world/stage: {whichPlaythrough}/{_whichWorld}/{_whichStage} with {_stageTimer}s");
		}

		private static void StartNewWorld()
		{
			++_whichWorld;
			_whichStage = -1;
			StartNewStage();
		}

		private static void StartNewPlaythrough()
		{
			_onTitleScreen = false;
			_cutsceneTimer = 0;
			_cutscenePhase = 0;

			++_whichPlaythrough;
			_whichWorld = -1;
			StartNewWorld();
		}

		private static int[,] GetMap(int wave)
		{
			return null;

			var map = new int[MapHeightInTiles, MapWidthInTiles];
			for (var i = 0; i < MapHeightInTiles; ++i)
			{
				for (var j = 0; j < MapWidthInTiles; ++j)
					map[i, j] = i != 0 && i != 15 && (j != 0 && j != 15)
					            || (i > 6 && i < 10 || j > 6 && j < 10)
						? (i == 0 || i == 15 || (j == 0 || j == 15)
							? (Game1.random.NextDouble() < 0.15 ? 1 : 0)
							: (i == 1 || i == 14 || (j == 1 || j == 14)
								? 2
								: (Game1.random.NextDouble() < 0.1 ? 4 : 3)))
						: 5;
			}
			switch (wave)
			{
				case -1:
					for (var i = 0; i < MapHeightInTiles; ++i)
					{
						for (var j = 0; j < MapWidthInTiles; ++j)
						{
							if (map[i, j] == 0 || map[i, j] == 1
							                   || (map[i, j] == 2 || map[i, j] == 5))
								map[i, j] = 3;
						}
					}
					break;
				case 0:
					for (var i = 0; i < MapHeightInTiles; ++i)
					{
						for (var j = 0; j < MapWidthInTiles; ++j)
						{
							map[i, j] = 0;
						}
					}
					break;
				default:
					map[4, 4] = 5;
					map[12, 4] = 5;
					map[4, 12] = 5;
					map[12, 12] = 5;
					break;
			}
			return map;
		}

		#endregion

		#region Minigame gameplay methods

		private static void AddScore(int score)
		{
			var debugscore = _totalScore;
			_totalScore = Math.Min(ScoreMax, _totalScore + score);
			//Log.D($"Score : {debugscore} => {_totalScore}");
		}

		private static void SpawnMonster(MonsterSpecies which, Vector2 where)
		{
			Game1.playSound("pullItemFromWater");
			_enemies.Add(new Monster(which, 
				new Rectangle(
					(int)where.X,
					(int)where.Y,
					MonsterSrcRects[which].Width * SpriteScale, 
					MonsterSrcRects[which].Height * SpriteScale), 
				MonsterHealth[which]));
		}

		/// <summary>
		/// Creates a new dropped powerup object moving from one point on-screen to another.
		/// </summary>
		/// <param name="which">Type of powerup to spawn in.</param>
		/// <param name="where">Spawn position.</param>
		private static void SpawnPowerup(LootDrops which, Vector2 where)
		{
			// Correct out-of-bounds spawns
			if (where.X < _gamePixelDimen.X)
				where.X = _gamePixelDimen.X;
			if (where.X > _gameEndPixel.X - TD * SpriteScale)
				where.X = _gameEndPixel.X - TD * SpriteScale;
			if (where.Y > _player.Position.Y)
				where.Y = _player.Position.Y;

			// Spawn powerup
			Game1.playSound("coin");
			_powerups.Add(new Powerup(which, 
				new Rectangle((int)where.X, (int)where.Y, TD, TD)));
		}

		/// <summary>
		/// Creates a new bullet object at a point on-screen for either the player or some monster.
		/// </summary>
		/// <param name="which">Projectile behaviour, also determines whether player or not.</param>
		/// <param name="where">Spawn position.</param>
		/// <param name="dest">Target for vector of motion from spawn position.</param>
		/// <param name="power">Health deducted from collider on hit.</param>
		/// <param name="who">Actor that fired the projectile.</param>
		private static void SpawnBullet(BulletType which, Vector2 where, Vector2 dest, int power, ArcadeActor who)
		{
			// Rotation to aim towards target
			var radiansBetween = Vector.RadiansBetween(dest, where);
			radiansBetween -= (float)(Math.PI / 2.0d);
			//Log.D($"RadiansBetween {where}, {dest} = {radiansBetween:0.00}rad", _isDebugMode);

			// Vector of motion
			var motion = Vector.PointAt(where, dest);
			motion.Normalize();
			_player.LastAimMotion = motion;
			//Log.D($"Normalised motion = {motion.X:0.00}x, {motion.Y:0.00}y, mirror={_player.SpriteMirror}");

			// Spawn position
			var position = where + motion * (BulletSpeed[which] * 5);
			var collisionBox = new Rectangle(
				(int)position.X,
				(int)position.Y,
				BulletSize[which],
				BulletSize[which]);

			// Add the bullet to the active lists for the respective spawner
			var bullet = new Bullet(which, power, who, collisionBox, motion, radiansBetween, dest);
			if (who is Player)
				_playerBullets.Add(bullet);
			else
				_enemyBullets.Add(bullet);
		}

		#endregion

		#region Per-tick updates

		private void UpdateTimers(TimeSpan elapsedGameTime)
		{
			// Run through the restart game timer
			if (_gameRestartTimer > 0)
			{
				_gameRestartTimer -= elapsedGameTime.Milliseconds;
				if (_gameRestartTimer <= 0)
				{
					unload();
					Game1.currentMinigame = new ArcadeGunGame();
				}
			}

			// Run through screen effects
			if (_screenFlashTimer > 0)
				_screenFlashTimer -= elapsedGameTime.Milliseconds;
		}

		private void UpdateGame(TimeSpan elapsedGameTime)
		{
			// Update player
			_player.Update(elapsedGameTime);
			// Update bullets
			foreach (var bullet in _playerBullets.ToArray())
				bullet.Update(elapsedGameTime);
			foreach (var bullet in _enemyBullets.ToArray())
				bullet.Update(elapsedGameTime);
			// Update powerups
			foreach (var powerup in _powerups.ToArray())
				powerup.Update(elapsedGameTime);
			// Update enemies
			foreach (var enemy in _enemies.ToArray())
				enemy.Update(elapsedGameTime);
		}

		private void UpdateMenus(TimeSpan elapsedGameTime)
		{
			// Sit on the start game screen until clicked away
			if (_onGameOver || _onTitleScreen)
			{
				// Progress through a small intro sequence
				_cutsceneTimer += elapsedGameTime.Milliseconds;
				//Log.D($"phase={_cutscenePhase} | timer={_cutsceneTimer}");
				switch (_cutscenePhase)
				{
					case 0:
						if (_cutsceneTimer >= TimeToTitleBeforeWhite)
						{
							// Move the lightshaft V/ texture across the screen
							_cutsceneBackgroundPosition += _gamePixelDimen.Width / UiAnimTimescale;
						}
						if (_cutsceneTimer >= TimeToTitleAfterWhite)
						{
							++_cutscenePhase; // Start showing all the title screen elements after it's held on blank for a bit
							Game1.playSound("wand");
						}
						break;
					case 1:
						if (_cutsceneTimer >= TimeToTitlePhase1)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					case 2:
						if (_cutsceneTimer >= TimeToTitlePhase2)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					case 3:
						if (_cutsceneTimer >= TimeToTitlePhase3)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					case 4:
						if (_cutsceneTimer < TimeToTitlePhase4)
							_cutsceneTimer = TimeToTitlePhase4;
						break;
					case 5:
						// End the cutscene and begin the game
						// after the user clicks past the end of intro cutscene (phase 4)
						_whichPlaythrough = -1;
						StartNewPlaythrough();
						Game1.playSound("cowboy_gunload");
						break;
				}
			}

			// Run through the end of world cutscene
			else if (_onWorldComplete)
			{
				var delta = elapsedGameTime.Milliseconds * (double)AnimCutsceneBackgroundSpeed;
				_cutsceneBackgroundPosition = (float)(_cutsceneBackgroundPosition + delta) % 96f;
			}
		}

		public void OnSecondUpdate()
		{
			// Gameplay checks
			if (!_onTitleScreen && _cutsceneTimer <= 0 && _player.PowerTimer <= 0)
			{
				// Count down stage timer
				if (_stageMilliseconds <= 0)
				{
					//GameOver(); // todo re-enable game over by timeout
					_stageMilliseconds = StageTimeInitial;
				}

				// todo: remove DEBUG cake spawning
				/*
				if (Game1.random.NextDouble() > 0.75d)
				{
					var where = new Vector2(
						Game1.random.Next(_gamePixelDimen.X + TD * SpriteScale, _gameEndPixel.X - TD * SpriteScale),
						Game1.random.Next(_gamePixelDimen.Y + TD * SpriteScale, _gamePixelDimen.Center.Y));
					SpawnPowerup(LootDrops.Cake, where);
				}
				*/

				// todo: remove DEBUG monster spawning
				if (!_enemies.Any())
				{
					var which = MonsterSpecies.Mafia;
					var xpos = Game1.random.NextDouble() < 0.5d 
						? _gamePixelDimen.X - MonsterSrcRects[which].Width * SpriteScale
						: _gameEndPixel.X;
					xpos = _gameEndPixel.X;
					var where = new Vector2(xpos, _gamePixelDimen.Center.Y);
					Log.D($"Spawning {which} at {where.ToString()}, "
					      + $"{(where.X < _gamePixelDimen.Center.X ? "left" : "right")} side.");
					SpawnMonster(which, where);
				}
			}
		}

		public bool tick(GameTime time)
		{
			var elapsedGameTime = time.ElapsedGameTime;
			//Log.D($"_stageMilliseconds: {_stageMilliseconds} | _stageTimer: {_stageTimer}");

			if (_player.HasPlayerQuit)
				return QuitMinigame();

			// Per-second updates
			if ((_stageMilliseconds / elapsedGameTime.Milliseconds) % (1000 / elapsedGameTime.Milliseconds) == 0)
				OnSecondUpdate();

			// Per-millisecond updates
			UpdateTimers(elapsedGameTime);
			for (var i = _temporaryAnimatedSprites.Count - 1; i >= 0; --i)
				if (_temporaryAnimatedSprites[i].update(time))
					_temporaryAnimatedSprites.RemoveAt(i);
			if (!_onTitleScreen && _cutsceneTimer <= 0)
				UpdateGame(elapsedGameTime);
			UpdateMenus(elapsedGameTime);

			return false;
		}

		#endregion

		#region Draw methods

		/// <summary>
		/// DEBUG: render line from start to end of bullet target trail
		/// </summary>
		private static void DrawTracer(SpriteBatch b)
		{
			return;
			if (!ModEntry.Instance.Config.DebugMode || !_playerBullets.Any()) return;

			// create 1x1 white texture for line drawing
			var t = new Texture2D(Game1.graphics.GraphicsDevice, 2, 2);
			t.SetData(new[] { Color.White, Color.White, Color.White, Color.White });

			var startpoint = _playerBullets[_playerBullets.Count - 1].Origin;
			var endpoint = _playerBullets[_playerBullets.Count - 1].Target;
			var line = endpoint - startpoint;
			var angle = Vector.RadiansBetween(startpoint, endpoint);
			b.Draw(
				_arcadeTexture,
				new Rectangle(
					(int)startpoint.X,
					(int)startpoint.Y,
					(int)line.Length(),
					1),
				null,
				Color.Red,
				angle,
				Vector2.Zero,
				SpriteEffects.None,
				1);
		}

		/// <summary>
		/// Renders a number digit-by-digit on screen to the target rectangle using the arcade number sprites.
		/// </summary>
		/// <param name="number">Number to draw.</param>
		/// <param name="maxDigits">Number of digits to draw. Will draw leading zeroes if number is shorter.</param>
		/// <param name="drawLeftToRight">Whether the X origin is a start or end point. Number will not be reversed.</param>
		/// <param name="where">Target rectangle to draw to, corrected to SpriteScale.</param>
		/// <param name="origin">Offset of x,y coordinates to draw to.</param>
		/// <param name="layerDepth">Occlusion value between 0f and 1f.</param>
		private static void DrawDigits(SpriteBatch b, int number, int maxDigits, bool drawLeftToRight, 
			Rectangle where, Vector2 origin, float layerDepth)
		{
			var destX = drawLeftToRight 
				? where.X + maxDigits * HudDigitSprite.Width * SpriteScale
				: where.X;
			var digits = 1;
			var divisor = 1;
			while (digits <= maxDigits)
			{
				var index = number >= divisor 
					? (number % (divisor * 10)) / divisor 
					: 0;
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						destX - HudDigitSprite.Width * SpriteScale * digits,
						 where.Y,
						 where.Width * SpriteScale,
						 where.Height * SpriteScale),
					new Rectangle(
						HudDigitSprite.X + HudDigitSprite.Width * index,
						HudDigitSprite.Y,
						HudDigitSprite.Width,
						HudDigitSprite.Height),
					Color.White,
					0.0f,
					origin,
					SpriteEffects.None,
					layerDepth);

				divisor *= 10;
				digits++;
			}
		}

		/// <summary>
		/// Renders in-game UI elements to show player and stage state.
		/// Health, energy, score, world and stage, stage time remaining, stage progress, ...
		/// </summary>
		private static void DrawHud(SpriteBatch b)
		{
			// Note: draw all HUD elements to depth 1f to show through flashes, effects, objects, etc.

			// todo: Draw enemy health bar to destrect width * health / healthmax
			//			or Draw enemy health bar as icons onscreen (much nicer)

			// todo: draw extra flair around certain hud elements
			// see 1581165827502.jpg of viking with hud

			// Player total score
			DrawDigits(b, _totalScore, 8, true,
				new Rectangle(_hudScoreDstX, _hudScoreDstY, HudDigitSprite.Width, HudDigitSprite.Height),
				Vector2.Zero, 1f);

			// Stage time remaining countdown
			b.Draw(
				_arcadeTexture,
				new Rectangle(
					_hudTimeDstX - HudDigitSprite.Width * (StageTimeHudDigits + 2) * SpriteScale,
					_hudTimeDstY,
					TD * SpriteScale, 
					TD * SpriteScale),
				new Rectangle( // HARD Y
					TD * 14,
					0,
					TD,
					TD),
				Color.White,
				0f,
				Vector2.Zero,
				SpriteEffects.None,
				1f);
			if (_stageMilliseconds >= 10000 
			    || _stageMilliseconds < 10000 && _stageMilliseconds % 400 < 200)
				DrawDigits(b, _stageMilliseconds / 1000, StageTimeHudDigits, false,
					new Rectangle(_hudTimeDstX, _hudTimeDstY, HudDigitSprite.Width, HudDigitSprite.Height),
					Vector2.Zero, 1f);

			// Player portrait
			var whichPortrait = 1; // Default
			
			if (_player.Health == 0 && _stageMilliseconds % 1000 < 400)
				whichPortrait = 8; // Out 2
			else if (_player.Health == 0)
				whichPortrait = 7; // Out 1
			else if (_player.ActivePowerPhase == PowerPhase.AfterActive2)
				whichPortrait = 6; // Power end 2
			else if (_player.ActivePowerPhase == PowerPhase.AfterActive1)
				whichPortrait = 5; // Power end 1
			else if (_player.ActivePowerPhase == PowerPhase.BeforeActive1 
			         || _player.ActivePowerPhase == PowerPhase.BeforeActive2)
				whichPortrait = 4; // Power start
			else if (_player.HurtTimer > 0)
				whichPortrait = 3; // Hurt
			else if (_player.RespawnTimer > 0)
				whichPortrait = 2; // Respawn and end-of-stage pose

			// Frame
			b.Draw(
				_arcadeTexture,
				new Rectangle(
					_hudPortraitDstX,
					_hudPortraitDstY,
					HudPortraitSprite.Width * SpriteScale,
					HudPortraitSprite.Height * SpriteScale),
				new Rectangle(
					HudPortraitSprite.X,
					HudPortraitSprite.Y,
					HudPortraitSprite.Width,
					HudPortraitSprite.Height),
				Color.White,
				0.0f,
				Vector2.Zero,
				SpriteEffects.None,
				1f);
			// Character
			b.Draw(
				_arcadeTexture,
				new Rectangle(
					_hudPortraitDstX,
					_hudPortraitDstY,
					HudPortraitSprite.Width * SpriteScale,
					HudPortraitSprite.Height * SpriteScale),
				new Rectangle(
					HudPortraitSprite.X + HudPortraitSprite.Width * whichPortrait,
					HudPortraitSprite.Y,
					HudPortraitSprite.Width,
					HudPortraitSprite.Height),
				Color.White,
				0.0f,
				Vector2.Zero,
				SpriteEffects.None,
				1f - 1f / 10000f);

			if (_player.Health > 1 || 
			    _player.Health == 1 && _stageMilliseconds % 400 < 200)
			{
				for (var i = 0; i < _player.Health; ++i)
				{
					// Player health icons
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_hudLifeDstX + HudLifeSprite.Width * SpriteScale * i,
							_hudLifeDstY,
							HudLifeSprite.Width * SpriteScale,
							HudLifeSprite.Height * SpriteScale),
						new Rectangle(
							HudLifeSprite.X,
							HudLifeSprite.Y,
							HudLifeSprite.Width,
							HudLifeSprite.Height),
						Color.White,
						0.0f,
						Vector2.Zero,
						SpriteEffects.None,
						1f);
				}
			}

			for (var i = 0; i < _player.Energy; ++i)
			{
				// Player energy icons
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						_hudEnergyDstX + HudEnergySprite.Width * SpriteScale * i,
						_hudEnergyDstY,
						HudEnergySprite.Width * SpriteScale,
						HudEnergySprite.Height * SpriteScale),
					new Rectangle(
						HudEnergySprite.X,
						HudEnergySprite.Y,
						HudEnergySprite.Width,
						HudEnergySprite.Height),
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f);
			}
		}

		/// <summary>
		/// Renders in-game stage background elements.
		/// </summary>
		private static void DrawBackground(SpriteBatch b)
		{
			switch (_player.ActiveSpecialPower)
			{
				case SpecialPower.Normal:
				case SpecialPower.Megaton:
					if (_player.ActivePowerPhase > PowerPhase.BeforeActive2 && _player.ActivePowerPhase < PowerPhase.AfterActive2)
					{
						// Draw a black overlay
						b.Draw(
							BlackoutPixel,
							new Rectangle(
								_gamePixelDimen.X,
								_gamePixelDimen.Y,
								_gamePixelDimen.Width,
								_gamePixelDimen.Height),
							new Rectangle(
								0,
								0,
								BlackoutPixel.Width,
								BlackoutPixel.Height),
							Color.Black,
							0.0f,
							Vector2.Zero,
							SpriteEffects.None,
							0f);
					}
					else
					{
						goto case SpecialPower.None;
					}
					break;
				case SpecialPower.Sulphur:
				case SpecialPower.Incense:
				case SpecialPower.None:
					// Draw the game map
					b.Draw(
						ColorFillPixel,
						new Rectangle(
							_gamePixelDimen.X,
							_gamePixelDimen.Y,
							_gamePixelDimen.Width,
							_gamePixelDimen.Height),
						new Rectangle(
							0,
							0,
							ColorFillPixel.Width,
							ColorFillPixel.Height),
						Color.White,
						0.0f,
						Vector2.Zero,
						SpriteEffects.None,
						0f);
					break;
			}
		}

		/// <summary>
		/// Renders text and elements in menus, cutscenes, title screen steps, ...
		/// </summary>
		private static void DrawMenus(SpriteBatch b)
		{
			// Display Start menu
			if (_onTitleScreen)
			{
				// Draw a black backdrop
				b.Draw(
					BlackoutPixel,
					new Rectangle(
						_gamePixelDimen.X,
						_gamePixelDimen.Y,
						_gamePixelDimen.Width,
						_gamePixelDimen.Height),
					new Rectangle(
						0,
						0,
						BlackoutPixel.Width,
						BlackoutPixel.Height),
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					0f);

				// Render each phase of the intro
				if (_cutscenePhase == 0)
				{
					// Draw the V/ banner as if illuminated by a shaft of light moving across the screen
					if (_cutsceneTimer >= TimeToTitleBeforeWhite)
					{
						// White V/
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								_gamePixelDimen.X + _gamePixelDimen.Width / 2 + TD * 2 + (int)_cutsceneBackgroundPosition,
								_gamePixelDimen.Y + _gamePixelDimen.Height / 2 - TD * 4,
								TitleLightShaftW,
								TitleWhiteBannerSprite.Height * SpriteScale),
							new Rectangle(
								TitleWhiteBannerSprite.X + (int)_cutsceneBackgroundPosition,
								TitleWhiteBannerSprite.Y,
								TitleLightShaftW,
								TitleWhiteBannerSprite.Height),
							Color.White,
							0.0f,
							new Vector2(0, TitleWhiteBannerSprite.Height / 2),
							SpriteEffects.None,
							0.5f);
					}
				}

				if (_cutscenePhase >= 1)
				{
					// Draw the coloured title banner on black

					// Red V/
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_gamePixelDimen.X + _gamePixelDimen.Width / 2 + TD * 2,
							_gamePixelDimen.Y + _gamePixelDimen.Height / 2 - TD * 4,
							TitleRedBannerSprite.Width * SpriteScale,
							TitleRedBannerSprite.Height * SpriteScale),
						new Rectangle(
							TitleRedBannerSprite.X,
							TitleRedBannerSprite.Y,
							TitleRedBannerSprite.Width,
							TitleRedBannerSprite.Height),
						Color.White,
						0.0f,
						new Vector2(0, TitleRedBannerSprite.Height / 2),
						SpriteEffects.None,
						0.5f);
				}

				if (_cutscenePhase >= 2)
				{
					// Draw the coloured title banner with all title screen text

					// コードネームは
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_gamePixelDimen.X + _gamePixelDimen.Width / 2 - TD / 2 * 6 - TitleCodenameSprite.Width,
							_gamePixelDimen.Y + _gamePixelDimen.Height / 2 - TD * 6 - TitleCodenameSprite.Height,
							TitleCodenameSprite.Width * SpriteScale,
							TitleCodenameSprite.Height * SpriteScale),
						new Rectangle(
							TitleCodenameSprite.X,
							TitleCodenameSprite.Y,
							TitleCodenameSprite.Width,
							TitleCodenameSprite.Height),
						Color.White,
						0.0f,
						new Vector2(0, TitleCodenameSprite.Height / 2),
						SpriteEffects.None,
						1.0f);
				}

				if (_cutscenePhase >= 3)
				{
					// セーラー
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_gamePixelDimen.X + _gamePixelDimen.Width / 2 - TD * 3 - TitleSailorSprite.Width,
							_gamePixelDimen.Y + _gamePixelDimen.Height / 2 - TD * 3,
							TitleSailorSprite.Width * SpriteScale,
							TitleSailorSprite.Height * SpriteScale),
						new Rectangle(
							TitleSailorSprite.X,
							TitleSailorSprite.Y,
							TitleSailorSprite.Width,
							TitleSailorSprite.Height),
						Color.White,
						0.0f,
						new Vector2(0, TitleSailorSprite.Height / 2),
						SpriteEffects.None,
						0.9f);
				}

				if (_cutscenePhase >= 4)
				{
					// Display flashing 'fire to start' text and signature text

					// © テレビ望月・東映動画 / © BLUEBERRY 1996
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_gamePixelDimen.X + _gamePixelDimen.Width / 2,
							_gamePixelDimen.Y + _gamePixelDimen.Height - TD * 5 - TitleSignatureSprite.Height,
							TitleSignatureSprite.Width * SpriteScale,
							TitleSignatureSprite.Height * SpriteScale),
						new Rectangle(
							TitleSignatureSprite.X,
							TitleSignatureSprite.Y,
							TitleSignatureSprite.Width,
							TitleSignatureSprite.Height),
						Color.White,
						0.0f,
						new Vector2(TitleSignatureSprite.Width / 2, TitleSignatureSprite.Height / 2),
						SpriteEffects.None,
						1f);

					if (_cutsceneTimer >= TimeToTitlePhase5 && (_cutsceneTimer / TimeToBlinkPrompt) % 2 == 0)
					{
						// "Fire to start"
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								_gamePixelDimen.X + _gamePixelDimen.Width / 2,
								_gamePixelDimen.Y + _gamePixelDimen.Height / 20 * 14,
								TitleFireToStartSprite.Width * SpriteScale,
								TitleFireToStartSprite.Height * SpriteScale),
							new Rectangle(
								0,
								TitleFireToStartSprite.Y,
								TitleFireToStartSprite.Width,
								TitleFireToStartSprite.Height),
							Color.White,
							0.0f,
							new Vector2(TitleFireToStartSprite.Width / 2, TitleFireToStartSprite.Height / 2),
							SpriteEffects.None,
							1f);
					}
				}
			}

			// Display Game Over menu options
			else if (_onGameOver)
			{
				b.Draw(
					Game1.staminaRect,
					new Rectangle(
						_gamePixelDimen.X,
						_gamePixelDimen.Y,
						16 * TD,
						16 * TD),
					Game1.staminaRect.Bounds,
					Color.Black,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					0.0001f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					new Vector2(_gamePixelDimen.X, _gamePixelDimen.Y) 
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
					new Vector2(_gamePixelDimen.X, _gamePixelDimen.Y) 
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
					new Vector2(_gamePixelDimen.X, _gamePixelDimen.Y) 
					+ new Vector2(6f, 7f) * TD 
					+ new Vector2(1f, 0.0f),
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				var text1 = Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11917");
				if (_gameOverOption == 0)
					text1 = "> " + text1;

				var text2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11919");
				if (_gameOverOption == 1)
					text2 = "> " + text2;

				if (_gameRestartTimer <= 0 || _gameRestartTimer / 500 % 2 == 0)
				{
					b.DrawString(
						Game1.smallFont,
						text1,
						new Vector2(_gamePixelDimen.X, _gamePixelDimen.Y) 
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
					new Vector2(_gamePixelDimen.X, _gamePixelDimen.Y) 
					+ new Vector2(6f, 9f) * TD 
					+ new Vector2(0.0f, 2f / 3f),
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);
			}
			// Show cutscene between worlds
			else if (_onWorldComplete)
			{
				// todo: display world stats per stage
				// todo: display total time
			}
		}

		/// <summary>
		/// Inherited from IMinigame.
		/// Calls each render method from the minigame.
		/// </summary>
		public void draw(SpriteBatch b)
		{
			b.Begin(
				SpriteSortMode.FrontToBack,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				null,
				null);
				
			// Render screen flash effects
			if (_screenFlashTimer > 0)
			{
				b.Draw(
					Game1.staminaRect,
					new Rectangle(
						_gamePixelDimen.X,
						_gamePixelDimen.Y,
						_gamePixelDimen.Width,
						_gamePixelDimen.Height),
					Game1.staminaRect.Bounds,
					_screenFlashColor,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f - 1f / 10000f);
			}

			// Draw the active game
			if (!_onTitleScreen && _player.RespawnTimer <= 0.0)
			{
				/*
				// debug makito
				// very important
				b.Draw(
						_arcadeTexture, 
						new Rectangle(
							_gamePixelDimen.Width,
							_gamePixelDimen.Height,
							TD * SpriteScale,
							TD * SpriteScale),
						new Rectangle(0, 0, TD, TD),
						Color.White,
						0.0f,
						new Vector2(TD / 2, TD / 2),
						player.SpriteMirror,
						1f);
				// very important
				// debug makito
				*/

				// Draw game elements
				DrawBackground(b);
				_player.Draw(b);
				DrawHud(b);
				if (ModEntry.Instance.Config.DebugMode)
					DrawTracer(b); // todo: remove DEBUG tracer

				// Draw game objects
				foreach (var bullet in _playerBullets)
					bullet.Draw(b);
				foreach (var bullet in _enemyBullets)
					bullet.Draw(b);
				foreach (var temporarySprite in _temporaryAnimatedSprites)
					temporarySprite.draw(b, true);
				foreach (var monster in _enemies)
					monster.Draw(b);
				foreach (var powerup in _powerups)
					powerup.Draw(b);
			}

			// Draw menus and cutscenes
			DrawMenus(b);

			b.End();
		}

		#endregion
		
		#region Objects

		/// <summary>
		/// Root of all custom objects.
		/// Able to be drawn on-screen and checked per tick.
		/// </summary>
		public abstract class ArcadeObject
		{
			public Vector2 Position;
			public Rectangle CollisionBox;
			public SpriteEffects SpriteMirror;

			protected ArcadeObject() { }

			protected ArcadeObject(Rectangle collisionBox)
			{
				CollisionBox = collisionBox;
				Position = new Vector2(CollisionBox.X, CollisionBox.Y);
			}

			public abstract void Update(TimeSpan elapsedGameTime);
			public abstract void Draw(SpriteBatch b);
		}

		/// <summary>
		/// Root of all custom actors.
		/// Able to live and be killed.
		/// </summary>
		public abstract class ArcadeActor : ArcadeObject
		{
			public int Health;
			public int HealthMax;
			public int Speed;
			public int SpeedMax;
			public int Power;
			public BulletType BulletType;

			public int FireTimer;
			public int HurtTimer;
			public int InvisibleTimer;
			public int InvincibleTimer;

			//public int AnimationPhase;
			public int AnimationTimer;

			protected ArcadeActor() { }

			protected ArcadeActor(Rectangle collisionBox, int health, int healthMax) 
				: base(collisionBox)
			{
				HealthMax = healthMax > 0 ? healthMax : health;
				Health = health;
			}

			internal virtual bool TakeDamage(int damage)
			{
				// Reduce health and check for death
				Health = Math.Max(0, Health - damage);
				return Health > 0;
			}

			internal abstract void Fire(Vector2 target);
			internal abstract void BeforeDeath();
			protected abstract void Die();
		}

		public class Player : ArcadeActor
		{
			public int Lives;
			public int Energy;
			public int EnergyMax;

			public SpecialPower ActiveSpecialPower;
			public PowerPhase ActivePowerPhase;
			public int PowerTimer;

			public bool HasPlayerQuit;
			public List<Move> MovementDirections = new List<Move>();
			public Vector2 LastAimMotion = Vector2.Zero;

			public int RespawnTimer;

			public Player()
			{
				SetStats();
				Reset();
			}

			private void SetStats()
			{
				BulletType = BulletType.Player;
				HealthMax = 4;
				SpeedMax = 5;
				Power = 1;
				EnergyMax = 7;
			}

			internal void Reset()
			{
				ResetStats();
				ResetPosition();
				ResetTimers();
			}

			private void ResetStats()
			{
				Health = HealthMax;
				Lives = GameLivesDefault;
				Speed = SpeedMax;
				Energy = 3; // todo: return to 0
				ActiveSpecialPower = SpecialPower.None;
				ActivePowerPhase = PowerPhase.None;
			}

			private void ResetPosition()
			{
				SpriteMirror = SpriteEffects.None;
				MovementDirections.Clear();

				// Spawn in the bottom-centre of the playable window
				Position = new Vector2(
					_gamePixelDimen.X + _gamePixelDimen.Width / 2 + PlayerW * SpriteScale / 2,
					_gamePixelDimen.Y + _gamePixelDimen.Height - PlayerFullH * SpriteScale - TD * SpriteScale / 2
				);
				// Player bounding box only includes the bottom 2/3rds of the sprite
				CollisionBox = new Rectangle(
					(int)Position.X,
					(int)Position.Y + PlayerFullH - PlayerSplitWH,
					PlayerW * SpriteScale,
					PlayerSplitWH * SpriteScale);

				//Log.D("player:" + $"\nx = {CollisionBox.X}, y = {CollisionBox.Y}, w = {CollisionBox.Width}, h = {CollisionBox.Height}");
			}

			private void ResetTimers()
			{
				AnimationTimer = 0;
				FireTimer = 0;
				HurtTimer = 0;
				PowerTimer = 0;
				//InvisibleTimer = 0;
				//InvincibleTimer = 0;
				RespawnTimer = 0;
			}

			/// <summary>
			/// Deducts health from the player.
			/// </summary>
			/// <param name="damage">Amount of health to remove at once.</param>
			/// <returns>Whether the player will survive.</returns>
			internal override bool TakeDamage(int damage)
			{
				var lastHealth = Health;

				var survives = base.TakeDamage(damage);

				Log.D($"Player hit for {damage} : {lastHealth}/{HealthMax} => {Health}/{HealthMax}");

				if (survives)
				{
					// todo: animate player

					// Flash translucent red
					_screenFlashColor = new Color(new Vector4(255, 0, 0, 0.25f));
					_screenFlashTimer = 200;
					HurtTimer = 200;
					// Grant momentary invincibility
					InvincibleTimer = GameInvincibleDelay;
				}
				else
				{
					_player.BeforeDeath();
				}

				return survives;
			}

			/// <summary>
			/// Pre-death effects.
			/// </summary>
			internal override void BeforeDeath()
			{
				// todo: fill this function with pre-death animation timer etc

				Game1.playSound("cowboy_dead");
				Die();
			}

			/// <summary>
			/// Deducts a life from the player and starts a respawn or restart game countdown.
			/// </summary>
			protected override void Die()
			{
				// todo: understand wtf this function does

				--Lives;
				RespawnTimer = GameDeathDelay;
				_spawnQueue = new List<Point>[4];
				for (var i = 0; i < 4; ++i)
					_spawnQueue[i] = new List<Point>();

				_temporaryAnimatedSprites.Add(
					new TemporaryAnimatedSprite(
						"LooseSprites\\Cursors",
						new Rectangle(
							464,
							1808,
							16,
							16),
						120f,
						5,
						0,
						new Vector2(_gamePixelDimen.X, _gamePixelDimen.Y), 
						false,
						false,
						1f,
						0.0f,
						Color.White,
						3f,
						0.0f,
						0.0f,
						0.0f,
						true));

				if (Lives >= 0)
					return;

				_temporaryAnimatedSprites.Add(
					new TemporaryAnimatedSprite(
						"LooseSprites\\Cursors",
						new Rectangle(
							464,
							1808,
							16,
							16),
						550f,
						5,
						0,
						new Vector2(_gamePixelDimen.X, _gamePixelDimen.Y),
						false,
						false,
						1f,
						0.0f,
						Color.White,
						3f,
						0.0f,
						0.0f,
						0.0f,
						true)
					{
						alpha = 1f / 1000f,
						endFunction = GameOverCheck
					});

				RespawnTimer *= 3;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="extra">Inherited from endFunction in TemporaryAnimtedSprite.</param>
			internal void GameOverCheck(int extra)
			{
				// todo probably remove this function, this and Die() are messed up entirely

				if (Lives >= 0)
				{
					Respawn(); // work out relationship with Die()
					return;
				}
				GameOver();
			}

			internal void Respawn()
			{
				InvincibleTimer = GameInvincibleDelay;
				Health = HealthMax;
			}

			internal void AddMovementDirection(Move direction)
			{
				if (MovementDirections.Contains(direction)) return;
				MovementDirections.Add(direction);
			}

			internal void PickupLoot(Powerup loot)
			{
				switch (loot.Type)
				{
					case LootDrops.Cake:
						if (loot.TextureRect.Y == 0)
						{ // maki maki makito
							Game1.playSound("crystal");
							AddScore(ScoreBread);
						}
						else
						{
							Game1.playSound("coin");
							AddScore(ScoreCake + loot.TextureRect.X / CakeSprite.Width * ScoreCakeExtra);
						}
						break;
					case LootDrops.Life:
						Game1.playSound("powerup");
						Health = Math.Min(HealthMax, Health + 1);
						break;
					case LootDrops.Energy:
						Game1.playSound("powerup");
						Energy = Math.Min(EnergyMax, Energy + 1);
						break;
					case LootDrops.Time:
						Game1.playSound("reward");
						_stageMilliseconds = Math.Min(StageTimeMax, _stageMilliseconds + StageTimeExtra);
						break;
				}
			}

			/// <summary>
			/// Player pressed hotkey to use special power, starts up an animation before kicking in.
			/// </summary>
			internal void PowerBeforeActive()
			{
				ActivePowerPhase = PowerPhase.BeforeActive1;
			}

			/// <summary>
			/// Power after-use before-active animation has ended, so start playing out the power's effects.
			/// </summary>
			internal void PowerActive()
			{
				// Energy levels between 0 and the low-threshold will use a light special power
				ActiveSpecialPower = Energy >= EnergyMax
					? SpecialPower.Normal
					: SpecialPower.Megaton;
				
				ActivePowerPhase = PowerPhase.AfterActive1;
			}

			/// <summary>
			/// Power's effects have finished playing out, play a wind-down animation and resolve the effects.
			/// </summary>
			internal void PowerAfterActive()
			{
				ActiveSpecialPower = SpecialPower.None;
				ActivePowerPhase = PowerPhase.None;
			}

			internal override void Fire(Vector2 target)
			{
				// Position the source around the centre of the player
				var src = new Vector2(
					CollisionBox.X + CollisionBox.Width / 2,
					CollisionBox.Y + CollisionBox.Height / 2);

				SpawnBullet(BulletType, src, target, Power, this);
				FireTimer = GameFireDelay;

				// Mirror player sprite to face target
				if (MovementDirections.Count == 0)
					SpriteMirror = target.X < Position.X + CollisionBox.Width / 2
						? SpriteEffects.FlipHorizontally
						: SpriteEffects.None;

				Game1.playSound("Cowboy_gunshot");

				++_totalShotsFired;
			}

			public override void Update(TimeSpan elapsedGameTime)
			{
				if (HurtTimer > 0)
					HurtTimer -= elapsedGameTime.Milliseconds;

				// todo: relegate outside player update routines to this method
				// Run down player invincibility
				if (InvincibleTimer > 0)
					InvincibleTimer -= elapsedGameTime.Milliseconds;

				// Run down player lightgun animation
				if (FireTimer > 0)
					--FireTimer;
				else
					LastAimMotion = Vector2.Zero;
			
				/* Player special powers */
				
				// Move through the power animations and effects
				if (PowerTimer > 0 || ActiveSpecialPower != SpecialPower.None)
				{
					PowerTimer += elapsedGameTime.Milliseconds;
					if (PowerTimer >= PowerPhaseDurations[ActivePowerPhase])
					{
						++ActivePowerPhase;
					}
				}
				else if (ActiveSpecialPower != SpecialPower.None)
				{
					PowerTimer += elapsedGameTime.Milliseconds;

					if (ActivePowerPhase == PowerPhase.AfterActive2 
					    && PowerTimer >= PowerPhaseDurations[ActivePowerPhase])
					{
						// Return to usual game flow
						PowerAfterActive();
					}
				}
				
				// While the player has agency
				else if (PowerTimer <= 0)
				{
					// Run down the death timer
					if (RespawnTimer > 0.0)
						RespawnTimer -= elapsedGameTime.Milliseconds;
					// Run down the stage timer while alive
					else
					{
						_stageMilliseconds -= elapsedGameTime.Milliseconds;
						// Count up total playthrough timer
						++_totalTime;
					}

					// Handle player movement
					if (MovementDirections.Count > 0)
					{
						switch (MovementDirections.ElementAt(0))
						{
							case Move.Right:
								SpriteMirror = SpriteEffects.None;
								if (Position.X + CollisionBox.Width < _gameEndPixel.X)
									Position.X += Speed;
								else
									Position.X = _gameEndPixel.X - CollisionBox.Width;
								break;
							case Move.Left:
								SpriteMirror = SpriteEffects.FlipHorizontally;
								if (Position.X > _gamePixelDimen.X)
									Position.X -= Speed;
								else
									Position.X = _gamePixelDimen.X;
								break;
						}
					}

					AnimationTimer += elapsedGameTime.Milliseconds;
					AnimationTimer %= (PlayerRunFrames) * PlayerAnimTimescale;

					// Update collision box values to sync with position
					CollisionBox.X = (int)Position.X;
				}
			}

			public override void Draw(SpriteBatch b)
			{
				// Flicker sprite visibility while invincible
				if (InvincibleTimer > 0 && InvincibleTimer / 100 % 2 != 0) return;

				var destRects = new Rectangle[3];
				var srcRects = new Rectangle[3];

				// Draw full body action sprites
				if (ActiveSpecialPower != SpecialPower.None)
				{   // Player used a special power
					// Draw power effects by type
					if (ActiveSpecialPower == SpecialPower.Normal)
					{   // Player used Venus Love Shower / THRESHOLD_LOW === POWER_NORMAL
						// . . . .
					}
					// Draw full body sprite by phase
					destRects[0] = new Rectangle(
						(int)Position.X,
						(int)Position.Y,
						CollisionBox.Width,
						PlayerFullH * SpriteScale);
					srcRects[0] = new Rectangle(
						PlayerPowerX
						+ PlayerW * (int)(PowerAnimationFrameGroup)ActiveSpecialPower
						+ PlayerW * (int)ActivePowerPhase,
						PlayerFullY,
						PlayerW,
						PlayerFullH);
				}
				else if (PowerTimer > 0)
				{   // Activated special power
					destRects[0] = new Rectangle(
						(int)Position.X,
						(int)Position.Y,
						CollisionBox.Width,
						PlayerFullH * SpriteScale);
					srcRects[0] = new Rectangle(
						PlayerSpecialX + PlayerW * (int)ActivePowerPhase,
						PlayerFullY,
						PlayerW,
						PlayerFullH);
				}
				else if (RespawnTimer > 0)
				{   // Player dying
					// . .. . .
				}
				// Draw full body idle sprite
				else if (FireTimer <= 0 && MovementDirections.Count == 0)
				{   // Standing idle
					destRects[0] = new Rectangle(
						(int)Position.X,
						(int)Position.Y,
						CollisionBox.Width,
						PlayerFullH * SpriteScale);
					srcRects[0] = new Rectangle(
						PlayerX,
						PlayerFullY,
						PlayerW,
						PlayerFullH);
				}
				// Draw appropriate sprite upper body
				else
				{
					if (FireTimer > 0)
					{
						var whichFrame = 0; // authors note: it was quicker to swap the level and below sprites 
											// than to fix my stupid broken logic
						if (LastAimMotion != Vector2.Zero)
						{
							// Firing arms
							if ((int)Math.Ceiling(LastAimMotion.X) == (int)SpriteMirror
							|| MovementDirections.Count > 0 && Math.Abs(LastAimMotion.Y) >= 0.9f)
								whichFrame = 4; // Aiming backwards, also aiming upwards while running
							else if (Math.Abs(LastAimMotion.Y) >= 0.9f) // Aiming upwards while standing
								whichFrame = 5; // invalid index, therefore no visible arms
							else if (LastAimMotion.Y < -0.6f) // Aiming low
								whichFrame = 3;
							else if (LastAimMotion.Y < -0.2f) // Aiming level
								whichFrame = 2;
							else if (LastAimMotion.Y > 0.2f) // Aiming below
								whichFrame = 1;
							destRects[2] = new Rectangle(
								(int)Position.X + (PlayerSplitWH / 2 * SpriteScale) 
								* (SpriteMirror == SpriteEffects.None ? 1 : -1),
								(int)Position.Y,
								CollisionBox.Width,
								CollisionBox.Height);
							srcRects[2] = new Rectangle(
								PlayerArmsX + PlayerW * whichFrame,
								PlayerArmsY,
								PlayerW,
								PlayerSplitWH);
						}
						if (MovementDirections.Count > 0)
						{   // Firing running torso
							whichFrame = AnimationTimer / PlayerAnimTimescale;
							destRects[0] = new Rectangle(
								(int)Position.X,
								(int)Position.Y,
								CollisionBox.Width,
								CollisionBox.Height);
							srcRects[0] = new Rectangle(
								PlayerBodyRunFireX + PlayerW * whichFrame,
								PlayerBodyY,
								PlayerW,
								PlayerSplitWH);
						}
						else
						{   // Firing standing torso
							whichFrame = PlayerBodySideFireX; // Aiming sideways
							if (Math.Abs(LastAimMotion.Y) >= 0.9f
								|| (int)Math.Ceiling(LastAimMotion.X) == (int)SpriteMirror)
								whichFrame = PlayerBodyUpFireX; // Aiming upwards
							destRects[0] = new Rectangle(
								(int)Position.X,
								(int)Position.Y,
								CollisionBox.Width,
								CollisionBox.Height);
							srcRects[0] = new Rectangle(
								whichFrame,
								PlayerBodyY,
								PlayerW,
								PlayerSplitWH);
						}
					}
					else
					{   // Running torso
						var whichFrame = AnimationTimer / PlayerAnimTimescale;
						destRects[0] = new Rectangle(
							(int)Position.X,
							(int)Position.Y,
							CollisionBox.Width,
							CollisionBox.Height);
						srcRects[0] = new Rectangle(
							PlayerBodyRunX + PlayerW * whichFrame,
							PlayerBodyY,
							PlayerW,
							PlayerSplitWH);
					}
				}

				// Draw appropriate sprite legs
				if (MovementDirections.Count > 0)
				{   // Running
					var whichFrame = AnimationTimer / PlayerAnimTimescale;
					destRects[1] = new Rectangle(
						(int)Position.X,
						(int)Position.Y + (PlayerFullH - PlayerSplitWH) * SpriteScale,
						CollisionBox.Width,
						CollisionBox.Height);
					srcRects[1] = new Rectangle(
						PlayerLegsRunX + PlayerW * whichFrame,
						PlayerLegsY,
						PlayerW,
						PlayerSplitWH);
				}
				else if (FireTimer > 0)
				{   // Standing and firing
					if (LastAimMotion != Vector2.Zero)
					{   // Firing legs
						var whichFrame = 0; // Aiming sideways
						if (Math.Abs(LastAimMotion.Y) > 0.9f)
							whichFrame = 1; // Aiming upwards
						destRects[1] = new Rectangle(
							(int)Position.X,
							(int)Position.Y + (PlayerFullH - PlayerSplitWH) * SpriteScale,
							CollisionBox.Width,
							CollisionBox.Height);
						srcRects[1] = new Rectangle(
							PlayerX + PlayerW * whichFrame,
							PlayerLegsY,
							PlayerW,
							PlayerSplitWH);
					}
				}

				// Draw the player from each component sprite
				for (var i = 2; i >= 0; --i)
				{
					if (srcRects[i] != Rectangle.Empty)
					{
						b.Draw(
							_arcadeTexture,
							destRects[i],
							srcRects[i],
							Color.White,
							0.0f,
							Vector2.Zero,
							SpriteMirror,
							Position.Y / 10000f - i / 1000f + 1f / 1000f);
					}
				}

				// Draw the player's shadow
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)Position.X,
						(int)(Position.Y + PlayerFullH * SpriteScale - ActorShadowRect.Height * SpriteScale),
						ActorShadowRect.Width * SpriteScale,
						ActorShadowRect.Height * SpriteScale), 
					ActorShadowRect,
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					Position.Y / 10000f - 5f / 1000f + 1f / 1000f);
			}
		}

		public class Monster : ArcadeActor
		{
			public MonsterSpecies Species;
			public bool Flying;
			public bool ScaleWithDistance;
			public Rectangle MovementTarget;
			public List<Vector2> AimTargets = new List<Vector2>();

			public int IdleTimer;

			public Monster(MonsterSpecies species, Rectangle collisionBox, int health, int healthMax = -1) 
				: base(collisionBox, health, healthMax)
			{
				SetStats(species);
			}

			private void SetStats(MonsterSpecies species)
			{
				Species = species;
				switch (species)
				{
					default:
						SpeedMax = MonsterSpeed[species];
						Speed = SpeedMax;
						Power = MonsterPower[species];
						Flying = MonsterIsFlying[species];
						ScaleWithDistance = false;
						MovementTarget = Rectangle.Empty;
						break;
				}
			}

			/// <summary>
			/// Deducts health from the monster.
			/// </summary>
			/// <param name="damage">Amount of health to remove at once.</param>
			/// <returns>Whether the monster will survive.</returns>
			internal override bool TakeDamage(int damage)
			{
				var lastHealth = Health;

				++_totalShotsSuccessful;

				var survives = base.TakeDamage(damage);

				Log.D($"{Species} hit for {damage} : {lastHealth}/{HealthMax} => {Health}/{HealthMax}");

				if (survives)
					HurtTimer = 20;
				else
					BeforeDeath();
				return survives;
			}

			internal override void Fire(Vector2 where)
			{
				var type = ArcadeGunGame.BulletType.None;
				switch (Species)
				{
					default:
						type = BulletType.Gun;
						break;
				}
				var src = new Vector2(CollisionBox.Center.X, CollisionBox.Center.Y);

				Log.D($"{Species} firing from {src.ToString()} => {where.ToString()} / {Power} power / {type} type");

				SpawnBullet(type, src, where, Power, this);
			}

			internal void GetNewAimTargets()
			{
				AimTargets.Clear();
				if (MonsterBullets[Species] == 1)
				{
					var target = Vector2.Zero;

					// Fire as close to the player as possible within the aim bounds for this monster
					if (_player.CollisionBox.X < CollisionBox.Center.X - MonsterAimWidth[Species])
						target.X = _player.CollisionBox.Center.X;
					else if (_player.CollisionBox.Center.X > CollisionBox.Center.X + MonsterAimWidth[Species])
						target.X = CollisionBox.Center.X + MonsterAimWidth[Species];
					else
						target.X = _player.CollisionBox.Center.X;
					target.Y = _player.CollisionBox.Center.Y;

					AimTargets.Add(target);
				}
				else
				{
					// todo: multi-bullet sprays
				}

				var str = "";
				foreach (var target in AimTargets)
					str += $"{target.ToString()} ";
				Log.D($"{Species} aim targets: {str}");
			}

			internal virtual void GetNewMovementTarget()
			{
				var xpos = 0;
				if (Flying)
				{
					// todo: flying enemem
				}
				else
				{
					xpos = Game1.random.Next(_gamePixelDimen.Width / 8, _gamePixelDimen.Width / 2);

					// Initial target moves a short distance into the screen
					if (Position.X <= _gamePixelDimen.X)
					{
						// Spawn from left side
						xpos = _gamePixelDimen.X + xpos;
					}
					else if (Position.X + CollisionBox.Width >= _gameEndPixel.X)
					{
						// Spawn from right side
						xpos = _gameEndPixel.X - xpos;
					}
					
					// Repeat targets swap sides
					else if (Position.X < _gamePixelDimen.Center.X)
					{
						xpos = _gameEndPixel.X - xpos;
					}
					else
					{
						xpos = _gamePixelDimen.X + xpos;
					}

					// Avoid stutter-stepping
					if (Math.Abs(Position.X - xpos) < CollisionBox.Width * 2)
					{
						xpos += (int)(Position.X - xpos);
					}
				}

				SpriteMirror = Position.X < _gamePixelDimen.Center.X 
					? SpriteEffects.None
					: SpriteEffects.FlipHorizontally;
				MovementTarget = new Rectangle(xpos, (int)Position.Y, CollisionBox.Width, CollisionBox.Height);

				Log.D($"{Species} moving at {CollisionBox.Center.ToString()} => {MovementTarget.Center.ToString()}, " + $"{(Position.X < _gamePixelDimen.Center.X ? "left" : "right")} side.");
			}

			internal virtual void OnReachedMovementTarget()
			{
				Log.D($"{Species} moved to {CollisionBox.Center.ToString()} == {MovementTarget.Center.ToString()}");
				AnimationTimer = 0;
				SpriteMirror = SpriteEffects.None;
				switch (Species)
				{
					default:
						// todo reset move target for some species
						MovementTarget = Rectangle.Empty;
						if (FireTimer <= 0)
							FireTimer = MonsterFireTime[Species];
						if (IdleTimer <= 0)
							IdleTimer = MonsterIdleTime[Species];
						break;
				}
			}

			/// <summary>
			/// Has a chance to create a new Powerup object at some position.
			/// </summary>
			/// <param name="collisionBox">Spawn position and touch bounds.</param>
			internal virtual void GetLootDrop(Rectangle collisionBox)
			{
				var rand = Game1.random.NextDouble();

				var rate = MonsterLootRates[Species];
				var rateTime = rate / 2 - (_stageMilliseconds - StageTimeInitial) / 500000f;

				var which = LootDrops.None;
				if (rand < LootRollGets[LootDrops.Life] * rate)
					which = LootDrops.Life;
				else if (rand < LootRollGets[LootDrops.Energy] * rate)
					which = LootDrops.Energy;
				else if (rand < LootRollGets[LootDrops.Cake] * rate)
					which = LootDrops.Cake;
				else if (rand < LootRollGets[LootDrops.Time] * rateTime)
					which = LootDrops.Time;
				
				Log.D($"GetLootDrop : rates: "
				      + $"life={LootRollGets[LootDrops.Life] * rate:0.0000} "
				      + $"energy={LootRollGets[LootDrops.Energy] * rate:0.0000} "
				      + $"cake={LootRollGets[LootDrops.Cake] * rate:0.0000} "
				      + $"time={LootRollGets[LootDrops.Time] * rate:0.0000} ");
				Log.D($"GetLootDrop : "
				      + $"rand={rand:0.00} "
				      + $"rate={rate:0.00000} "
				      + $"time={LootRollGets[LootDrops.Time] * rate} "
				      + $"rateTime={LootRollGets[LootDrops.Time] * rateTime:0.00000}");
				Log.D($"GetLootDrop : "
				      + $"which={which}");

				// good players get cake
				if (which == LootDrops.Life && _player.Health == _player.HealthMax)
					which = LootDrops.Cake;
				// bad players get clock
				if (which == LootDrops.Cake && _stageMilliseconds <= StageTimeCritical)
					which = LootDrops.Time;

				if (which != LootDrops.None)
					SpawnPowerup(
						which,
						new Vector2(collisionBox.X, collisionBox.Y));
			}

			internal override void BeforeDeath()
			{
				// Reward points
				++_totalMonstersBonked;
				AddScore(MonsterScore[Species]);

				// Try to throw loot upon dying
				GetLootDrop(new Rectangle(
					CollisionBox.X + CollisionBox.Width / 2,
					CollisionBox.Y + CollisionBox.Height / 2,
					TD,
					TD));

				Die();
			}

			protected override void Die()
			{
				Log.D($"{Species} died");
				_enemies.Remove(this);
			}

			public override void Update(TimeSpan elapsedGameTime)
			{
				// Prepare to fire
				if (FireTimer == MonsterFireTime[Species])
					GetNewAimTargets();

				// Run down timers
				if (IdleTimer > 0)
					IdleTimer -= elapsedGameTime.Milliseconds;
				if (HurtTimer > 0) 
					HurtTimer -= elapsedGameTime.Milliseconds;

				if (FireTimer > 0)
					FireTimer -= elapsedGameTime.Milliseconds;
				if (InvincibleTimer > 0)
					InvincibleTimer -= elapsedGameTime.Milliseconds;

				if (FireTimer > 0 && (HurtTimer <= 0 || !MonsterFiringCancellable[Species]))
				{	// Fire when not being stunned by hurt
					if (FireTimer < MonsterFireTime[Species] / 2)
					{	// Unload all bullets at once for this species
						foreach (var target in AimTargets.ToArray())
							Fire(target);
						AimTargets.Clear();
					}
					else if (MonsterFireStaggered[Species])
					{	// Unload the next bullet in a staggered sequence for this species
						if (FireTimer / MonsterBullets[Species] % elapsedGameTime.Milliseconds == 0)
						{
							// todo: monster staggered firing
						}
					}
				}
				else if (HurtTimer <= 0)
				{
					// todo: player contact damage check when necessary

					// While moving
					if (MovementTarget != Rectangle.Empty)
					{
						if (CollisionBox.Intersects(MovementTarget))
						{   // Act on reaching target position
							OnReachedMovementTarget();
						}
						else
						{
							// Update position
							if (Math.Abs(Position.X - MovementTarget.X) > Speed)
								Position.X += Speed * (Position.X > MovementTarget.X ? -1 : 1);
							if (Math.Abs(Position.Y - MovementTarget.Y) > Speed)
								Position.Y += Speed * (Position.Y > MovementTarget.Y ? -1 : 1);

							// Update collision box values to sync with position
							CollisionBox.X = (int)Position.X;
							CollisionBox.Y = (int)Position.Y;
							
							// Scale down distant monsters
							if (ScaleWithDistance)
								CollisionBox.Width = MonsterSrcRects[Species].Width * (CollisionBox.Y < _gamePixelDimen.Height / 2
									? 1
									: 2);

							// Tick up the animation timer while moving
							AnimationTimer += elapsedGameTime.Milliseconds;
							AnimationTimer %= EnemyRunFrames * EnemyAnimTimescale;
						}
					}
					// While not moving and neither firing or pausing after firing
					else if (FireTimer <= 0 && IdleTimer <= 0)
					{
						// Fetch a new movement target after firing and idling
						GetNewMovementTarget();
					}
				}
			}

			public override void Draw(SpriteBatch b)
			{
				var whichFrame = 0;

				// Run frames
				if (MovementTarget != Rectangle.Empty)
					whichFrame = 1 + AnimationTimer / EnemyAnimTimescale;

				var scale = ScaleWithDistance && CollisionBox.Y < _gamePixelDimen.Height / 2
					? SpriteScale / 2 
					: SpriteScale;
				
				// Crop the monster to remain in the game bounds for the illusion of a screen limit
				var xOffset = 0;
				var yOffset = 0;
				var width = MonsterSrcRects[Species].Width;
				var height = MonsterSrcRects[Species].Height;
				var flipOffset = false;
				
				if (CollisionBox.X < _gamePixelDimen.X)
				{
					xOffset = Math.Abs(CollisionBox.X - _gamePixelDimen.X) / scale;
					width = MonsterSrcRects[Species].Width - xOffset;
				}
				else if (CollisionBox.X + MonsterSrcRects[Species].Width * scale > _gameEndPixel.X)
				{
					width = MonsterSrcRects[Species].Width - Math.Abs(
						(CollisionBox.X + MonsterSrcRects[Species].Width * scale - _gameEndPixel.X) / scale);
					xOffset = MonsterSrcRects[Species].Width - width;
					flipOffset = true;
				}
				
				// todo: cropping for y-axis

				/*
				if (CollisionBox.Y < _gamePixelDimen.Y)
				{
					height = Math.Min(MonsterSrcRects[Species].Height, Math.Abs(CollisionBox.Y - _gamePixelDimen.Y));
					yOffset = MonsterSrcRects[Species].Height - height;
				}
				else if (CollisionBox.Y > _gameEndPixel.Y - MonsterSrcRects[Species].Height * scale)
				{
					height = Math.Min(MonsterSrcRects[Species].Height, Math.Abs(CollisionBox.Y - _gameEndPixel.Y - MonsterSrcRects[Species].Height));
				}
				*/

				if (CollisionBox.X < _gamePixelDimen.X 
				    || CollisionBox.X + MonsterSrcRects[Species].Width * scale > _gameEndPixel.X)
					Log.D($"Draw {Species}: x={xOffset} y={yOffset} w={width} h={height}");

				// Draw the monster
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)Position.X + (flipOffset ? 0 : xOffset * scale),
						(int)Position.Y + yOffset * scale,
						width * scale,
						height * scale),
					new Rectangle(
						MonsterSrcRects[Species].X + MonsterSrcRects[Species].Width * whichFrame + xOffset,
						MonsterSrcRects[Species].Y + yOffset,
						width,
						height),
					HurtTimer <= 0 
						? Color.White
						: Color.Red,
					0.0f,
					Vector2.Zero,
					SpriteMirror,
					Position.Y / 10000f + 1f / 1000f);
				
				// Draw the player's shadow
				var yOffsetFromFlying = Flying ? TD * scale : 0f;
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)Position.X,
						(int)(Position.Y + MonsterSrcRects[Species].Height * scale - ActorShadowRect.Height * scale
						      + yOffsetFromFlying),
						ActorShadowRect.Width * SpriteScale,
						ActorShadowRect.Height * SpriteScale), 
					ActorShadowRect,
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					(Position.Y - yOffsetFromFlying) / 10000f);

				// todo: draw a flashing crosshair over the bullet target
			}
		}

		public class Powerup : ArcadeObject
		{
			public readonly LootDrops Type;
			public readonly Rectangle TextureRect;
			public float YOffset;
			public long Duration;

			public Powerup(LootDrops type, Rectangle collisionBox) : base(collisionBox)
			{
				// Pick a texture for the powerup drop
				switch (type)
				{ // HARD Y
					case LootDrops.Cake:
						// Decide which cake to show (or makito)
						var x = 0;
						var y = 0;
						var d = Game1.random.NextDouble();
						if (d > 0.05)
						{
							x = (int)(CakesFrames * (d * Math.Floor(10d / (CakesFrames))));
							y = 1;
							Log.D($"Chose cake no.{x}");
						}
						else
						{
							Log.D("Chose makito");
						}

						TextureRect = new Rectangle(TD * x, TD * y, TD, TD);
						break;
					case LootDrops.Life:
						TextureRect = new Rectangle(TD * 12, 0, TD, TD);
						break;
					case LootDrops.Energy:
						TextureRect = new Rectangle(TD * 13, 0, TD, TD);
						break;
					case LootDrops.Time:
						TextureRect = new Rectangle(TD * 14, 0, TD, TD);
						break;
					default:
						return;
				}

				Type = type;
				Duration = LootDurations[type] * 1000;
			}

			/// <summary>
			/// Update dropped loot per tick, removing them as their timer runs down.
			/// </summary>
			public override void Update(TimeSpan elapsedGameTime)
			{
				// Powerup expired
				Duration -= elapsedGameTime.Milliseconds;
				if (Duration <= 0)
				{
					Log.D($"{Type} expired at {Position.X}, {Position.Y}");
					_powerups.Remove(this);
				}

				// Powerup dropping in from spawn
				var endpoint = _player.CollisionBox.Y + _player.CollisionBox.Height - CollisionBox.Height;
				if (Position.Y < endpoint)
					Position.Y = Math.Min(endpoint, Position.Y + 4 * SpriteScale);

				// Update collision box values to sync with position
				CollisionBox.X = (int)Position.X;
				CollisionBox.Y = (int)Position.Y;

				if (!CollisionBox.Intersects(_player.CollisionBox)) return;

				// Powerup collected
				_player.PickupLoot(this);
				_powerups.Remove(this);
			}

			public override void Draw(SpriteBatch b)
			{
				if (Duration <= 2000 && Duration / 200 % 2 != 0) return;
				/*
				var scale = CollisionBox.Y < _gamePixelDimen.Height / 2
					? SpriteScale / 2 
					: SpriteScale;
				*/
				var scale = SpriteScale;
				b.Draw(
					_arcadeTexture,
					new Vector2(
						Position.X,
						Position.Y + YOffset),
					new Rectangle(
						TextureRect.X,
						TextureRect.Y,
						TextureRect.Width,
						TextureRect.Height),
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteScale,
					SpriteEffects.None,
					Position.Y / 10000f + 1f / 1000f);

				// Draw the powerup's shadow
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)Position.X,
						_player.CollisionBox.Y + _player.CollisionBox.Height - LootShadowRect.Height,
						LootShadowRect.Width * scale,
						LootShadowRect.Height * scale), 
					LootShadowRect,
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					Position.Y / 10000f);
			}
		}
		
		public class Bullet : ArcadeObject
		{
			public ArcadeActor Owner;
			public int Power;
			public Vector2 Motion;          // Line of motion between spawn and dest
			public Vector2 MotionCur;       // Current position along vector of motion between spawn and dest
			public float Rotation;          // Angle of vector between spawn and dest
			public float RotationCur;       // Current spin
			public BulletType Type;
			public bool ScaleWithDistance;

			public Vector2 Origin;
			public Vector2 Target;

			public Bullet(BulletType type, int power, ArcadeActor owner, Rectangle collisionBox, Vector2 motion, float rotation, Vector2 target)
				: base(collisionBox)
			{
				Type = type;
				Power = power;
				Owner = owner;

				Motion = motion;
				MotionCur = motion;
				Rotation = rotation;
				RotationCur = type == BulletType.Player ? 0f : rotation;
				ScaleWithDistance = false;

				Origin = new Vector2(collisionBox.Center.X, collisionBox.Center.Y);
				Target = target;

				//Log.D($"Bullet : {Type} :" + $" x = {CollisionBox.X}, y = {CollisionBox.Y}," + $" w = {CollisionBox.Width}, h = {CollisionBox.Height}");
			}

			/// <summary>
			/// Update on-screen bullets per tick, removing as they travel offscreen or hit a target.
			/// </summary>
			public override void Update(TimeSpan elapsedGameTime)
			{
				// Remove offscreen bullets
				if (!CollisionBox.Intersects(_gamePixelDimen))
				{
					//Log.D($"Bullet : {Type} : out of bounds");
					
					if (Owner is Player)
						_playerBullets.Remove(this);
					else
						_enemyBullets.Remove(this);
				}

				if (Owner is Player)
				{   // Damage any monsters colliding with bullets
					for (var j = _enemies.Count - 1; j >= 0; --j)
					{
						if (CollisionBox.Intersects(_enemies[j].CollisionBox))
						{
							Log.D($"Bullet : {Type} : hit {_enemies[j].Species}");

							_playerBullets.Remove(this);
							if (_enemies[j].HurtTimer <= 0)
								_enemies[j].TakeDamage(Power);
						}
					}
				}
				else
				{	// Damage the player
					if (_player.InvincibleTimer <= 0 && _player.RespawnTimer <= 0f)
					{
						if (CollisionBox.Intersects(_player.CollisionBox))
						{
							Log.D($"Bullet : {Type} : hit player");

							_enemyBullets.Remove(this);
							if (CollisionBox.Intersects(_player.CollisionBox))
								_player.TakeDamage(Power);
						}
					}
				}

				// Update bullet positions
				Position += Motion * BulletSpeed[Type];

				// Update collision box values to sync with position
				CollisionBox.X = (int)Position.X;
				CollisionBox.Y = (int)Position.Y;
				
				// Scale down distant bullets
				if (ScaleWithDistance)
					CollisionBox.Width = BulletSize[Type] * (CollisionBox.Y < _gamePixelDimen.Height / 2
						? 1
						: 2);
			}

			public override void Draw(SpriteBatch b)
			{
				var scale = ScaleWithDistance && CollisionBox.Y < _gamePixelDimen.Height / 2
					? SpriteScale / 2 
					: SpriteScale;

				b.Draw(
					_arcadeTexture,
					new Vector2(
						CollisionBox.X,
						CollisionBox.Y),
					new Rectangle(
						ProjectileSrcRects[Type].X + TD * (int)Type,
						ProjectileSrcRects[Type].Y,
						ProjectileSrcRects[Type].Width,
						ProjectileSrcRects[Type].Height),
					Color.White,
					Rotation,
					new Vector2(ProjectileSrcRects[Type].Width / 2, ProjectileSrcRects[Type].Height / 2),
					scale,
					SpriteEffects.None,
					0.9f);
			}
		}
		
		#endregion

		#region Vector operations

		internal class Vector
		{
			public static Vector2 PointAt(Vector2 va, Vector2 vb)
			{
				return vb - va;
			}
			
			public static float RadiansBetween(Vector2 va, Vector2 vb)
			{
				return (float)Math.Atan2(vb.Y - va.Y, vb.X - va.X);
			}
		}

		#endregion
	}

	#region Nice code

	/*
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
