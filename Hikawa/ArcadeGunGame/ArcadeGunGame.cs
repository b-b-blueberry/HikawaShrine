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

			
		/* Game attributes */

		// Game values
		private const int GameLivesDefault = 1;
		private const int GameEnergyThresholdLow = 3;
		private const int GameDodgeDelay = 500;
		private const int GameInvincibleDelay = 3000;
		private const int GameDeathDelay = 3000;
		private const int GameEndDelay = 5000;

		private const int StageTimeMax = 99000;
		private const int StageTimeInitial = 60000;
		private const int StageTimeExtra = 15000;
		private const int StageTimeCritical = 5000;
		private const int StageTimeHudDigits = 2;

		// Score values
		private const int ScoreMax = 99999999;
		private const int ScoreCake = 3000;
		private const int ScoreCakeExtra = 150;
		private const int ScoreBread = 10400;
		
		/* Sprite attributes */

		// Sprite dimensions and size multipliers
		private const int TD = 16;
		private const int SpriteScale = 2;
		private const int CursorScale = 4;
		// Animation rates, lower is faster
		private const int UiAnimTimescale = 85;
		private const int PlayerAnimTimescale = 250;
		private const int EnemyAnimTimescale = 200;
		private const int BulletAnimTimescale = 500;
		private const int PowerupAnimTimescale = 100;
		// Boundary dimensions for gameplay, menus, and cutscenes
		private const int MapWidthInTiles = 20;
		private const int MapHeightInTiles = 18;

		/* Enum values */

		// Game common attributes
		public enum MenuOption
		{
			Retry,
			Quit
		}
		public enum Swatch
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
			Length
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
		private const int TimeToPowerPhaseBeforeActive1 = 400;
		private const int TimeToPowerPhaseBeforeActive2 = TimeToPowerPhaseBeforeActive1 + 400;
		private const int TimeToPowerPhaseActive1 = TimeToPowerPhaseBeforeActive2 + 1000;
		private const int TimeToPowerPhaseActive2 = TimeToPowerPhaseActive1 + 500;
		private const int TimeToPowerPhaseActive3 = TimeToPowerPhaseActive2 + 500;
		private const int TimeToPowerPhaseActive4 = TimeToPowerPhaseActive3 + 250;
		private const int TimeToPowerPhaseAfterActive1 = TimeToPowerPhaseActive4 + 400;
		private const int TimeToPowerPhaseAfterActive2 = TimeToPowerPhaseAfterActive1 + 400;
		private const int TimeToPowerPhaseEnd = TimeToPowerPhaseAfterActive2 + 400;
		private static readonly Dictionary<PowerPhase, int> PowerPhaseDurations = new Dictionary<PowerPhase, int>
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
		// Bullets and projectiles
		public enum BulletType
		{
			None,
			Player, // players
			Gun, // small collision box, fast movement
			Junk, // large collision box, slow movement
			Bomb, // no collision, different arc, explodes on death
			Energy,
			Petal, // menu screen effects
			Bubble,
		}
		private static readonly Dictionary<BulletType, float> BulletSpeed = new Dictionary<BulletType, float>
		{
			{BulletType.None, 0f},
			{BulletType.Player, 3f},
			{BulletType.Gun, 3f},
			{BulletType.Junk, 1.5f},
			{BulletType.Bomb, 2f},
			{BulletType.Energy, 3f},
			{BulletType.Petal, 0.35f},
			{BulletType.Bubble, 1f},
		};
		private static readonly Dictionary<BulletType, float> BulletSpin = new Dictionary<BulletType, float>
		{ // todo: add values in radians
			{BulletType.None, 0f},
			{BulletType.Player, 0f},
			{BulletType.Gun, 0f},
			{BulletType.Junk, (float)(Math.PI / 180f)},
			{BulletType.Bomb, 0f},
			{BulletType.Energy, 0f},
			{BulletType.Petal, 0f},
			{BulletType.Bubble, 0f},
		};
		private static readonly Dictionary<BulletType, int> BulletFireRates = new Dictionary<BulletType, int>
		{
			{BulletType.None, 0},
			{BulletType.Player, 20},
			{BulletType.Gun, 10},
			{BulletType.Junk, 0},
			{BulletType.Bomb, 30},
			{BulletType.Energy, 0},
			{BulletType.Petal, 5},
			{BulletType.Bubble, 40},
		};
		private static readonly Dictionary<BulletType, int> BulletSize = new Dictionary<BulletType, int>
		{
			{BulletType.None, 0},
			{BulletType.Player, TD / 2},
			{BulletType.Gun, TD / 2},
			{BulletType.Junk, TD},
			{BulletType.Bomb, TD},
			{BulletType.Energy, TD},
			{BulletType.Petal, TD},
			{BulletType.Bubble, TD},
		};
		private static readonly Dictionary<BulletType, int> BulletFrames = new Dictionary<BulletType, int>
		{
			{BulletType.None, 0},
			{BulletType.Player, 2},
			{BulletType.Gun, 2},
			{BulletType.Junk, 2},
			{BulletType.Bomb, 2},
			{BulletType.Energy, 2},
			{BulletType.Petal, 3},
			{BulletType.Bubble, 2},
		};
		private static readonly Dictionary<BulletType, int[]> BulletAnimations = new Dictionary<BulletType, int[]>
		{
			{BulletType.None, new []{ 0 }},
			{BulletType.Player, new []{ 0 }},
			{BulletType.Gun, new []{ 0 }},
			{BulletType.Junk, new []{ 0 }},
			{BulletType.Bomb, new []{ 0 }},
			{BulletType.Energy, new []{ 0 }},
			{BulletType.Petal, new []{ 0, 1, 0, 2 }},
			{BulletType.Bubble, new []{ 0, 1 }},
		};
		// Loot drops and powerups
		public enum LootDrops
		{
			None,
			Life,
			Energy,
			Cake,
			Time,
			Megahealth,
			BubbleGun,
		}
		private static readonly Dictionary<LootDrops, double> LootRollGets = new Dictionary<LootDrops, double>
		{
			{LootDrops.None, 0},
			{LootDrops.Life, 0.1d},
			{LootDrops.Energy, 0.2d},
			{LootDrops.Cake, 0.5d},
			{LootDrops.BubbleGun, 0.55d},
			{LootDrops.Time, 0.9d},
			{LootDrops.Megahealth, 1d},
		};
		private static readonly Dictionary<LootDrops, int> LootDurations = new Dictionary<LootDrops, int>
		{
			{LootDrops.None, 0},
			{LootDrops.Life, 4},
			{LootDrops.Energy, 3},
			{LootDrops.Cake, 5},
			{LootDrops.Time, 5},
			{LootDrops.Megahealth, 99999},
			{LootDrops.BubbleGun, 5},
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
		private static readonly Dictionary<MonsterSpecies, float> MonsterSpeed = new Dictionary<MonsterSpecies, float>
		{
			{MonsterSpecies.Mafia, 3f},
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
		
		/* Game object graphics */

		// Cakes and sweets
		private const int CakesFrames = 9;
		public static readonly Rectangle CakeSprite = new Rectangle( // HARD Y
			0, TD * 1, TD, TD);
		// Bullets
		public static readonly int BulletSpriteY = CakeSprite.Y + CakeSprite.Height;
		public static readonly Dictionary<BulletType, Rectangle> BulletSrcRects = new Dictionary<BulletType, Rectangle>
		{ // todo: remove??
			{ BulletType.None, new Rectangle(0, BulletSpriteY, TD, TD) }, 
			{ BulletType.Player, new Rectangle(0, BulletSpriteY, TD, TD) },
			{ BulletType.Gun, new Rectangle(0, BulletSpriteY, TD, TD) },
			{ BulletType.Bomb, new Rectangle(0, BulletSpriteY, TD, TD) },
			{ BulletType.Energy, new Rectangle(0, BulletSpriteY, TD, TD) },
			{ BulletType.Petal, new Rectangle(0, BulletSpriteY, TD, TD) },
		};
		// monsters
		private const int EnemyRunFrames = 3;
		private static readonly Dictionary<MonsterSpecies, Rectangle> MonsterSrcRects = new Dictionary<MonsterSpecies, Rectangle>
		{
			{MonsterSpecies.Mafia, new Rectangle(0, TD * 24, TD * 2, TD * 3)},
		};
		
		/* Text and digit graphics */

		private static readonly Rectangle HudDigitSprite = new Rectangle( // HARD Y
			0, TD * 4, TD, TD);
		private static readonly int HudStringsY = HudDigitSprite.Y + HudDigitSprite.Height;
		private static readonly int HudStringsH = TD;

		/* HUD graphics */
		
		// Crosshair
		public static readonly Rectangle CrosshairDimen = new Rectangle( // HARD Y
			TD * 2, 0, TD, TD);

		// Player life
		private static readonly Rectangle HudLifeSprite = new Rectangle( // HARD Y
			TD * 12, 0, TD, TD);
		// Player energy
		private static readonly Rectangle HudEnergySprite = new Rectangle( // HARD Y
			HudLifeSprite.X + HudLifeSprite.Width, 0, TD, TD);
		// Player portrait
		private static readonly Rectangle HudPortraitSprite = new Rectangle(
			0, HudStringsY + HudStringsH, TD * 2, TD * 2);

		// Player score
		private static readonly Rectangle HudScoreTextSprite = new Rectangle(
			0, HudStringsY, TD * 1, HudStringsH);
		// Stage enemy health
		private const int HudHealthPips = 20;
		private static readonly Rectangle HudHealthPipSprite = new Rectangle( // HARD Y // HARD X
			TD * 12, TD, 6, TD);
		private static readonly Rectangle HudHealthTextSprite = new Rectangle( // WEIRD X
			HudEnergySprite.X + HudEnergySprite.Width, HudStringsY, TD * 3, TD);
		// Stage time
		private static readonly Rectangle HudTimeSprite = new Rectangle( // HARD Y
			HudEnergySprite.X + HudEnergySprite.Width, 0, TD, TD);

		/* Player graphics */

		// Shared attributes
		private const int PlayerW = TD * 2;
		private const int PlayerX = 0;
		// Full-body sprites (body, arms and legs combined)
		private const int PlayerFullH = TD * 3;
		private const int PlayerFullY = TD * 8; // HARD Y
		// Split-body sprites (body, arms or legs individually)
		private const int PlayerSplitWH = TD * 2;

		// todo: resolve special/power animations

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

		/* Cutscene graphics */

		private const float AnimCutsceneBackgroundSpeed = 0.1f;

		#endregion


		//////////////////////////
		#region Gameplay Variables
		//////////////////////////


		/* Minigame meta attributes */

		// Shorthand values
		private static int _left;
		private static int _top;
		private static int _right;
		private static int _bottom;
		private static int _width;
		private static int _height;
		private static Point _centre;

		private static bool _playMusic = true;
		private behaviorAfterMotionPause _behaviorAfterPause;
		public delegate void behaviorAfterMotionPause();

		private static readonly Texture2D FillColours = new Texture2D(
			Game1.graphics.GraphicsDevice, (int)Swatch.Length, 1);

		/* Game actors and objects */

		private static Player _player;
		//private static Player _player2;
		private static List<Bullet> _playerBullets = new List<Bullet>();
		private static List<Bullet> _enemyBullets = new List<Bullet>();
		private static List<Bullet> _extraBullets = new List<Bullet>();
		private static List<Monster> _enemies = new List<Monster>();
		private static List<TemporaryAnimatedSprite> _temporaryAnimatedSprites = new List<TemporaryAnimatedSprite>();
		private static List<Powerup> _powerups = new List<Powerup>();
		private static List<Point>[] _spawnQueue = new List<Point>[4];

		/* Game state */

		// Current state
		private static int _whichStage;
		private static int _whichWorld;
		private static int _stageEnemyHealthGoal;
		private static int _stageEnemyHealth;
		private static int _stageMilliseconds;
		private static int[,] _stageMap = new int[MapHeightInTiles, MapWidthInTiles];
		private static bool _enemyHealthRegenerating;

		// Records and statistics
		private static int _totalTime;
		private static int _totalScore;
		private static int _totalShotsSuccessful;
		private static int _totalShotsFired;
		private static int _totalMonstersBonked;

		// Extras
		private static int _currentMenuOption;

		/* Music cues */

		private static ICue gameMusic;

		/* HUD graphics */

		private static Texture2D _arcadeTexture;
		private static Texture2D _player2Texture;
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
		// Stage health
		private static int _hudGoalDstX;
		private static int _hudGoalDstY;

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
			Log.D("ArcadeGunGame()");
			changeScreenSize();
			
			Game1.changeMusicTrack("none", false, Game1.MusicContext.MiniGame);

			if (ModEntry.Instance.Config.DebugMode && !ModEntry.Instance.Config.DebugArcadeMusic)
				_playMusic = false;

			// Load arcade game assets
			_arcadeTexture = Helper.Content.Load<Texture2D>(
				Path.Combine(ModConsts.AssetsDirectory, ModConsts.SpritesDirectory, $"{ModConsts.ArcadeSpritesFile}.png"));

			// Load fill colours
			var swatch = new Color[(int)Swatch.Length];
			_arcadeTexture.GetData(0, 
				new Rectangle(0, 0, swatch.Length, 1),
				swatch, 0, swatch.Length);
			FillColours.SetData(swatch);

			// Load player 2's modified assets
			LoadPlayer2();

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
			{
				if (_cutscenePhase < 4)
				{
					_cutscenePhase = 4; // Skip the splash animation
				}
				else
				{
					++_cutscenePhase;
				}
			}
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
						_player.PowerStart();
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

		private static void LoadPlayer2()
		{
			Log.D("LoadPlayer2()");
			/* Load texture as a duplicate of the usual arcade game set, bottom cropped out */
			
			var rect = new Rectangle(
				0,
				0,
				_arcadeTexture.Width,
				PlayerLegsY + PlayerSplitWH);

			var pixels = new Color[rect.Width * rect.Height];
			_arcadeTexture.GetData(0, rect, pixels, 0, pixels.Length);
			
			// Swap out copy colours in the player sprites region with player 2's theme
			for (var y = HudPortraitSprite.Y; y < rect.Height; ++y)
			{
				for (var x = 0; x < rect.Width; ++x)
				{
					var i = x + y * rect.Width;
					if (pixels[i].A == 0) continue;
					if (pixels[i] == pixels[(int)Swatch.LightRed])
						pixels[i] = pixels[(int)Swatch.LightGreen];
					else if (pixels[i] == pixels[(int)Swatch.Red])
						pixels[i] = pixels[(int)Swatch.Green];
					else if (pixels[i] == pixels[(int)Swatch.DarkRed])
						pixels[i] = pixels[(int)Swatch.DarkGreen];
				}
			}
			
			// Copy new sprite set to player 2's draw texture
			_player2Texture = new Texture2D(Game1.graphics.GraphicsDevice, rect.Width, rect.Height);
			_player2Texture.SetData(pixels);
		}

		private static void InvalidateCursorsOnNextTick(object sender, UpdateTickedEventArgs e)
		{
			Log.D("InvalidateCursorsOnNextTick()");
			Helper.Events.GameLoop.UpdateTicked -= InvalidateCursorsOnNextTick;
			Helper.Content.InvalidateCache("LooseSprites/Cursors");
		}

		public void changeScreenSize()
		{
			Log.D("");
			/* Determine game edge bounds */

			// this used to be a rect and a vect but it was awful
			_left = (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width - MapWidthInTiles * TD * SpriteScale) / 2;
			_top = (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height - MapHeightInTiles * TD * SpriteScale) / 2;
			_right = _left + MapWidthInTiles * TD * SpriteScale;
			_bottom = _top + MapHeightInTiles * TD * SpriteScale;
			_width = _right - _left;
			_height = _bottom - _top;
			_centre = new Point(_left + _width / 2, _top + _height / 2);

			/* Determine HUD element positions around the game bounds */

			var spacing = 2;
			var above = _top - spacing * SpriteScale;
			var below = _bottom + spacing * SpriteScale;

			// Player score
			_hudScoreDstX = _left;
			_hudScoreDstY = above - HudDigitSprite.Height * SpriteScale;
			// Stage time remaining
			_hudTimeDstX = _right;
			_hudTimeDstY = above - HudDigitSprite.Height * SpriteScale;
			// Stage enemy health remaining
			_hudGoalDstX = _centre.X;
			_hudGoalDstY = above + TD / 2 * SpriteScale;

			// Player portrait
			_hudPortraitDstX = _left;
			_hudPortraitDstY = below;
			// Player life
			_hudLifeDstX = _left + HudPortraitSprite.Width * SpriteScale;
			_hudLifeDstY = below;
			// Player energy
			_hudEnergyDstX = _hudLifeDstX;
			_hudEnergyDstY = below + HudLifeSprite.Height * SpriteScale;

			// Game world and stage
			_hudWorldDstX = _right;
			_hudWorldDstY = below;
			
			Log.D("changeScreenSize()\n"
			+ $"Dimensions: x = {_left:000}, y = {_top:000}, w = {_width:000}, h = {_height:000}\n"
			+ $"Centre:     x = {_centre.X:000}, y = {_centre.Y:000}\n"
			+ $"Endpoint:   x = {_right:000}, y = {_bottom:000}",
				IsDebugMode);
		}

		private static bool QuitMinigame()
		{
			Log.D("QuitMinigame()");
			StopMusic();
			if (Game1.currentLocation != null
			    && Game1.currentLocation.Name.Equals((object)"Saloon") && Game1.timeOfDay >= 1700)
				Game1.changeMusicTrack("Saloon1");
			Game1.currentMinigame = null;
			Helper.Content.InvalidateCache("LooseSprites/Cursors");
			return true;
		}

		public void unload()
		{
			Log.D("unload()");
			_player.Reset();
			Game1.stopMusicTrack(Game1.MusicContext.MiniGame);
		}

		/// <summary>
		/// Starts running down the clock to return to the title screen after resetting the game state.
		/// </summary>
		private static void GameOver()
		{
			Log.D("GameOver()");
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
			Log.D("ResetAndReturnToTitle()");
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
			Log.D("ResetGame()");
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
			ResetPlaythrough();
		}

		private static void CleanUpStage()
		{
			Log.D("CleanUpStage()");
			_powerups.Clear();
			_enemies.Clear();
			_playerBullets.Clear();
			_enemyBullets.Clear();
			_extraBullets.Clear();
		}

		#endregion

		#region Minigame progression methods
		
		private static void ResetPlaythrough()
		{
			Log.D("ResetPlaythrough()");
			_whichStage = 0;
			_whichWorld = 0;
			_onGameOver = false;
		}

		private static void EndCurrentStage()
		{
			Log.D("EndCurrentStage()");
			_player.MovementDirections.Clear();

			// todo: set cutscenes, begin events, etc
			// todo: add stage time to score
			// todo: purge remaining monsters and bullets
			// todo: consume remaining powerups one-by-one
		}

		private static void EndCurrentWorld()
		{
			Log.D("EndCurrentWorld()");
			// todo: set cutscenes, begin events, etc
		}

		private static void PlayMusic(ICue cue, string id)
		{
			Log.D($"PlayMusic(ICue cue, string id : {id}, _playMusic: {_playMusic})");
			if (!_playMusic) return;

			cue = Game1.soundBank.GetCue(id);
			cue.Play();
			Game1.musicPlayerVolume = Game1.options.musicVolumeLevel;
			Game1.musicCategory.SetVolume(Game1.musicPlayerVolume);
		}

		private static void StopMusic() {
			if (gameMusic != null && gameMusic.IsPlaying)
				gameMusic.Stop(AudioStopOptions.Immediate);
			if (Game1.IsMusicContextActive(Game1.MusicContext.MiniGame))
				Game1.stopMusicTrack(Game1.MusicContext.MiniGame);
		}

		private static void StartNewStage()
		{
			Log.D("StartNewStage()");
			++_whichStage;
			_stageMap = GetMap(_whichStage);

			switch (_whichStage)
			{
				default:
					// todo: set health and timer per stage/world/boss

					_stageEnemyHealth = _stageEnemyHealthGoal = 30;
					_stageMilliseconds = StageTimeInitial;
					break;
			}

			//Log.D($"Current play/world/stage: {whichPlaythrough}/{_whichWorld}/{_whichStage} with {_stageTimer}s");
		}

		private static void StartNewWorld()
		{
			Log.D("StartNewWorld()");
			++_whichWorld;
			_whichStage = -1;
			StartNewStage();
		}

		private static void StartNewPlaythrough()
		{
			Log.D("StartNewPlaythrough()");
			CleanUpStage();
			ResetPlaythrough();
			_onTitleScreen = false;
			_cutsceneTimer = 0;
			_cutscenePhase = 0;

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
			if (where.X < _left)
				where.X = _left;
			if (where.X > _right - TD * SpriteScale)
				where.X = _right - TD * SpriteScale;
			if (where.Y > _player.Position.Y)
				where.Y = _player.Position.Y;

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
		private static void SpawnBullet(BulletType which, Vector2 where, Vector2 dest, int power, ArcadeActor who, bool rotate)
		{
			// Rotation to aim towards target
			var radiansBetween = Vector.RadiansBetween(dest, where);
			radiansBetween -= (float)(Math.PI / 2.0d);
			//Log.D($"RadiansBetween {where}, {dest} = {radiansBetween:0.00}rad", _isDebugMode);

			// Vector of motion
			var motion = Vector.PointAt(where, dest);
			motion.Normalize();
			_player.LastAimMotion = motion;
			var speed = BulletSpeed[which];
			//Log.D($"Normalised motion = {motion.X:0.00}x, {motion.Y:0.00}y, mirror={_player.SpriteMirror}");

			// Spawn position
			var position = where + motion * (BulletSpeed[which] * 5 * SpriteScale);
			var collisionBox = new Rectangle(
				(int)position.X,
				(int)position.Y,
				BulletSize[which],
				BulletSize[which]);

			// Add the bullet to the active lists for the respective spawner
			var bullet = new Bullet(which, power, who, collisionBox, motion, radiansBetween, dest, speed, rotate);
			if (who is Player)
				_playerBullets.Add(bullet);
			else if (who is Monster)
				_enemyBullets.Add(bullet);
			else
				_extraBullets.Add(bullet);
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
							_cutsceneBackgroundPosition += _width / UiAnimTimescale;
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

		public void PerSecondUpdate()
		{
			if (!_onTitleScreen)
			{
				// Gameplay actions
				if (_cutsceneTimer <= 0 && _player.PowerTimer <= 0)
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
							Game1.random.Next(_left + TD * SpriteScale, _right - TD * SpriteScale),
							Game1.random.Next(_top + TD * SpriteScale, _centre.Y));
						SpawnPowerup(LootDrops.Cake, where);
					}
					*/

					// todo: remove DEBUG monster spawning
					if (!_enemies.Any())
					{
						var which = MonsterSpecies.Mafia;
						var xpos = Game1.random.NextDouble() < 0.5d 
							? _left - MonsterSrcRects[which].Width * SpriteScale
							: _right;
						var where = new Vector2(xpos, _centre.Y);
						//Log.D($"Spawning {which} at {where.ToString()}, " + $"{(where.X < _centre.X ? "left" : "right")} side.");
						SpawnMonster(which, where);
					}
				}
			}
			else
			{
				// Menu actions
				if (_cutsceneTimer >= TimeToTitlePhase2)
				{
					// Spawn petals
					var which = BulletType.Petal;
					var xpos = Game1.random.Next(_left - TD * SpriteScale * 3, _right - TD * SpriteScale);
					var ypos = _top + 5;
					//Log.D($"Spawn {which} at {xpos}, {ypos} thanks");
					SpawnBullet(which, 
						new Vector2(xpos, ypos), 
						new Vector2(xpos + Game1.random.Next(TD * 3, TD * 8), _bottom), 
						0, null, false);
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
			if ((!_onTitleScreen && (_stageMilliseconds / elapsedGameTime.Milliseconds) % (1000 / elapsedGameTime.Milliseconds) == 0) 
			    || (_onTitleScreen && (_cutsceneTimer / elapsedGameTime.Milliseconds) % (1000 / elapsedGameTime.Milliseconds) == 0))
				PerSecondUpdate();

			// Per-quarter-second updates
			if ((_stageMilliseconds / elapsedGameTime.Milliseconds) % (1000 / elapsedGameTime.Milliseconds)
			    == 250 / elapsedGameTime.Milliseconds)
			{
				if (_player.IsHealthRegenerating)
					_player.AddHealth(1);
				if (_player.IsEnergyDepleting)
					_player.RemoveEnergy(1);
			}

			// Per-millisecond updates
			UpdateMenus(elapsedGameTime);
			UpdateTimers(elapsedGameTime);
			foreach (var bullet in _extraBullets.ToArray())
				bullet.Update(elapsedGameTime);
			for (var i = _temporaryAnimatedSprites.Count - 1; i >= 0; --i)
				if (_temporaryAnimatedSprites[i].update(time))
					_temporaryAnimatedSprites.RemoveAt(i);
			if (!_onTitleScreen)
				UpdateGame(elapsedGameTime);

			return false;
		}

		#endregion

		#region Draw methods

		private static void DrawBread(SpriteBatch b){
			// debug makito
			// very important
			b.Draw(
				_arcadeTexture, 
				new Rectangle(
					_width,
					_height,
					TD * SpriteScale,
					TD * SpriteScale),
				new Rectangle(0, 0, TD, TD),
				Color.White,
				0.0f,
				new Vector2(TD / 2, TD / 2),
				_player.SpriteMirror,
				1f);
			// very important
			// debug makito
		}

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
			// Self-note:
			// Draw all HUD elements to depth 1f to show through flashes, effects, objects, etc.

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
					HudTimeSprite.Width * SpriteScale, 
					HudTimeSprite.Height * SpriteScale),
				new Rectangle(
					HudTimeSprite.X,
					HudTimeSprite.Y,
					HudTimeSprite.Width,
					HudTimeSprite.Height),
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
			
			// Stage enemy health
			// Label
			b.Draw(
				_arcadeTexture,
				new Rectangle(
					_hudGoalDstX,
					_hudGoalDstY,
					HudHealthTextSprite.Width * SpriteScale,
					HudHealthTextSprite.Height * SpriteScale),
				HudHealthTextSprite,
				Color.White,
				0.0f,
				new Vector2 (HudHealthTextSprite.Width / 2, HudHealthTextSprite.Height / 2),
				SpriteEffects.None,
				1f);
			// Frame inner
			b.Draw(
				FillColours,
				new Rectangle(
					(_hudGoalDstX - HudHealthPipSprite.Width * SpriteScale * HudHealthPips / 2) - 1 * SpriteScale,
					_hudGoalDstY,
					(HudHealthPipSprite.Width * HudHealthPips + 2) * SpriteScale,
					HudHealthPipSprite.Height * SpriteScale),
				new Rectangle((int)Swatch.LightBlue, 0, 1, 1),
				Color.White,
				0.0f,
				Vector2.Zero,
				SpriteEffects.None,
				1f - 1f / 10000f - 1f / 10000f);
			// Frame outer
			b.Draw(
				FillColours,
				new Rectangle(
					(_hudGoalDstX - HudHealthPipSprite.Width * SpriteScale * HudHealthPips / 2) - 2 * SpriteScale,
					(_hudGoalDstY) - 1 * SpriteScale,
					(HudHealthPipSprite.Width * HudHealthPips + 4) * SpriteScale,
					HudHealthPipSprite.Height * SpriteScale + 4),
				new Rectangle((int)Swatch.Black, 0, 1, 1),
				Color.White,
				0.0f,
				Vector2.Zero,
				SpriteEffects.None,
				1f - 1f / 10000f - 1f / 10000f - 1f / 10000f);

			var healthPercentage = (float)_stageEnemyHealth / _stageEnemyHealthGoal;
			for (var i = 0; i < HudHealthPips; ++i)
			{ 
				// Health icons, with lost health greyed out
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						_hudGoalDstX - HudHealthPipSprite.Width * SpriteScale * HudHealthPips / 2
						+ HudHealthPipSprite.Width * SpriteScale * i ,
						_hudGoalDstY,
						HudHealthPipSprite.Width * SpriteScale,
						HudHealthPipSprite.Height * SpriteScale),
					HudHealthPipSprite,
					i < HudHealthPips * healthPercentage
						? Color.White 
						: Color.DarkSlateBlue,
					0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f - 1f / 10000f);
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
							FillColours,
							new Rectangle(
								_left,
								_top,
								_width,
								_height),
							new Rectangle((int)Swatch.Black, 0, 1, 1),
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
						FillColours,
						new Rectangle(
							_left,
							_top,
							_width,
							_height),
						new Rectangle((int)Swatch.Brown, 0, 1, 1),
						Color.White,
						0.0f,
						Vector2.Zero,
						SpriteEffects.None,
						0f);
					break;
			}
		}

		private static void DrawGame(SpriteBatch b) {
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
					FillColours,
					new Rectangle(
						_left,
						_top,
						_width,
						_height),
					new Rectangle((int)Swatch.Black, 0, 1, 1),
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
								_left + _width / 2 + TD * 2 + (int)_cutsceneBackgroundPosition,
								_top + _height / 2 - TD * 4,
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
							1f);
					}
				}

				if (_cutscenePhase >= 1)
				{
					// Draw the coloured title banner on black

					// Red V/
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_left + _width / 2 + TD * 2,
							_top + _height / 2 - TD * 4,
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
						1f);
				}

				if (_cutscenePhase >= 2)
				{
					// Draw the coloured title banner with all title screen text

					// コードネームは
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_left + _width / 2 - TD / 2 * 6 - TitleCodenameSprite.Width,
							_top + _height / 2 - TD * 6 - TitleCodenameSprite.Height,
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
						1f);
				}

				if (_cutscenePhase >= 3)
				{
					// セーラー
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_left + _width / 2 - TD * 3 - TitleSailorSprite.Width,
							_top + _height / 2 - TD * 3,
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
						1f);
				}

				if (_cutscenePhase >= 4)
				{
					// Display flashing 'fire to start' text and signature text

					// © テレビ望月・東映動画 / © BLUEBERRY 1996
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							_left + _width / 2,
							_top + _height - TD * 5 - TitleSignatureSprite.Height,
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
						// 1P START
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								_left + _width / 2,
								_top + _height / 20 * 14,
								Title1PStartSprite.Width * SpriteScale,
								Title1PStartSprite.Height * SpriteScale),
							new Rectangle(
								Title1PStartSprite.X + (true ? 0 : Title1PStartSprite.Width),
								Title1PStartSprite.Y,
								Title1PStartSprite.Width,
								Title1PStartSprite.Height),
							Color.White,
							0.0f,
							new Vector2(Title1PStartSprite.Width / 2, Title1PStartSprite.Height / 2),
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
						_left,
						_top,
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
					new Vector2(_left, _top) 
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
					new Vector2(_left, _top) 
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
					new Vector2(_left, _top) 
					+ new Vector2(6f, 7f) * TD 
					+ new Vector2(1f, 0.0f),
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				var text1 = Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11917");
				if (_currentMenuOption == 0)
					text1 = "> " + text1;

				var text2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11919");
				if (_currentMenuOption == 1)
					text2 = "> " + text2;

				if (_gameRestartTimer <= 0 || _gameRestartTimer / 500 % 2 == 0)
				{
					b.DrawString(
						Game1.smallFont,
						text1,
						new Vector2(_left, _top) 
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
					new Vector2(_left, _top) 
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
						_left,
						_top,
						_width,
						_height),
					Game1.staminaRect.Bounds,
					_screenFlashColor,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f - 1f / 10000f);
			}
			
			DrawMenus(b);
			if (!_onTitleScreen)
				DrawGame(b);
			foreach (var bullet in _extraBullets.ToArray())
				bullet.Draw(b);

			b.End();
		}

		/// <summary>
		/// Generates a new draw rectangle for sprites moving in or out of the playable boundary for the game.
		/// Note: This crops the left side. Sprite will need correction by mirroring when moving over the right/bottom side.
		/// Note: You'll need the FlipOffset value too for that.
		/// </summary>
		/// <param name="srcRect">Area to read sprite from in asset file.</param>
		/// <param name="destRect">Area to draw sprite to on-screen.</param>
		/// <param name="scale">Whether to halve the sprite scale for a sense of distance.</param>
		/// <returns>Rectangle containing adjusted width and height dimensions for the sprite,
		/// and the X and Y offsets as a difference between new and old width values.</returns>
		internal static Rectangle GetSpriteDimensionsVisibleAtGameBounds(Rectangle srcRect, Rectangle destRect, int scale) {
			// Sprite draw rect offsets from original x,y for the whole sprite
			var xOffset = 0;
			var yOffset = 0;
			// Sprite draw rect dimensions for the part of the sprite visible within the game bounds
			var width = srcRect.Width;
			var height = srcRect.Height;
			
			// Crop out the area of the sprite sitting outside of the game bounds:
			// Left and rightwards
			if (destRect.X < _left)
			{
				xOffset = Math.Abs(destRect.X - _left) / scale;
				width = srcRect.Width - xOffset;
			}
			else if (destRect.X + srcRect.Width * scale > _right)
			{
				width = srcRect.Width - Math.Abs((destRect.X + srcRect.Width * scale - _right) / scale);
				xOffset = srcRect.Width - width;
			}
			// Above and below
			if (destRect.Y < _top)
			{
				yOffset = Math.Abs(destRect.Y - _top) / scale;
				height = srcRect.Height - yOffset;
			}
			else if (destRect.Y + srcRect.Height * scale > _bottom)
			{
				height = srcRect.Height - Math.Abs((destRect.Y + srcRect.Height * scale - _bottom) / scale);
				yOffset = srcRect.Height - height;
			}

			/*
			if (destRect.X < _left 
			    || destRect.X + srcRect.Width * scale > _right
			    || destRect.Y < _top
			    || destRect.Y + srcRect.Height * scale > _bottom)
				Log.D($"Draw: x={xOffset} y={yOffset} w={width} h={height} "
				      + $"left={destRect.X < _left} right={destRect.X + srcRect.Width * scale > _right} "
				      + $"top={destRect.Y < _top} bottom={destRect.Y + srcRect.Height * scale > _bottom}");
			*/

			return new Rectangle(xOffset, yOffset, width, height);
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
			public bool ScaleWithDistance;

			public float Speed;
			public float SpeedMax;
			
			//public int AnimationPhase;
			public int AnimationTimer;

			protected ArcadeObject() { }
			
			protected ArcadeObject(Rectangle collisionBox, float speed = 0)
			{
				CollisionBox = collisionBox;
				Position = new Vector2(CollisionBox.X, CollisionBox.Y);

				Speed = SpeedMax = speed;

				//Log.D($"Object : x = {CollisionBox.X}, y = {CollisionBox.Y}," + $" w = {CollisionBox.Width}, h = {CollisionBox.Height}");
			}

			public abstract void Update(TimeSpan elapsedGameTime);
			public abstract void Draw(SpriteBatch b);
		}

		public abstract class ArcadeInteractable : ArcadeObject
		{
			public Rectangle TextureRect;

			protected ArcadeInteractable() { }

			protected ArcadeInteractable(Rectangle collisionBox, float speed = 0) : base(collisionBox, speed) { }
		}

		/// <summary>
		/// Root of all custom actors.
		/// Able to live and be killed.
		/// </summary>
		public abstract class ArcadeActor : ArcadeObject
		{
			public int Health;
			public int HealthMax;
			public int Power;
			public BulletType ActiveBulletType;

			public int FireTimer;
			public int HurtTimer;
			public int InvisibleTimer;
			public int InvincibleTimer;

			protected ArcadeActor() { }

			protected ArcadeActor(Rectangle collisionBox, int health, int healthMax, int speed) 
				: base(collisionBox, speed)
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
			public int WeaponTimer;
			
			public bool IsPlayerOne;
			public bool HasPlayerQuit;
			public bool IsHealthRegenerating;
			public bool IsEnergyDepleting;
			public List<Move> MovementDirections = new List<Move>();
			public Vector2 LastAimMotion = Vector2.Zero;
			
			public int RespawnTimer;

			public Player()
			{
				IsPlayerOne = true;
				SetStats();
				Reset();
			}

			private void SetStats()
			{
				ActiveBulletType = BulletType.Player;
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
				ActiveBulletType = BulletType.Player;
			}

			private void ResetPosition()
			{
				SpriteMirror = SpriteEffects.None;
				MovementDirections.Clear();

				// Spawn in the bottom-centre of the playable window
				Position = new Vector2(
					_left + _width / 2 + PlayerW * SpriteScale / 2,
					_top + _height - PlayerFullH * SpriteScale - TD * SpriteScale / 2
				);
				// Player bounding box only includes the bottom 2/3rds of the sprite
				CollisionBox = new Rectangle(
					(int)Position.X,
					(int)Position.Y + (PlayerFullH - PlayerSplitWH) * SpriteScale,
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
					_screenFlashTimer = 250;
					HurtTimer = 1250;
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
						new Vector2(_left, _top), 
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
						new Vector2(_left, _top),
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

			internal void AddHealth(int howMuch)
			{
				Game1.playSound("powerup");
				Health = Math.Min(HealthMax, Health + Math.Abs(howMuch));
				if (Health == HealthMax)
					IsHealthRegenerating = false;
			}
			
			internal void AddEnergy(int howMuch)
			{
				Game1.playSound("powerup");
				Energy = Math.Min(EnergyMax, Energy + Math.Abs(howMuch));
			}

			internal void RemoveEnergy(int howMuch)
			{
				Energy = Math.Max(0, Energy - Math.Abs(howMuch));
				if (Energy == 0)
					IsEnergyDepleting = false;
			}

			internal void SwapBulletType(BulletType which = BulletType.Player)
			{
				Game1.playSound("cowboy_gunload");
				ActiveBulletType = which;
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
						AddHealth(1);
						break;
					case LootDrops.Energy:
						AddEnergy(1);
						break;
					case LootDrops.Time:
						Game1.playSound("reward");
						_stageMilliseconds = Math.Min(StageTimeMax, _stageMilliseconds + StageTimeExtra);
						break;
					case LootDrops.Megahealth:
						Game1.playSound("reward");
						IsHealthRegenerating = true;
						break;
				}
			}

			internal void PowerStart()
			{
				PowerBeforeActive();
			}

			/// <summary>
			/// Player pressed hotkey to use special power, starts up an animation before kicking in.
			/// </summary>
			private void PowerBeforeActive()
			{
				ActivePowerPhase = PowerPhase.BeforeActive1;
			}

			/// <summary>
			/// Power after-use before-active animation has ended, so start playing out the power's effects.
			/// </summary>
			private void PowerActive()
			{
				// Energy levels between 0 and the low-threshold will use a light special power
				ActiveSpecialPower = Energy >= EnergyMax
					? SpecialPower.Normal
					: SpecialPower.Megaton;
				
				ActivePowerPhase = PowerPhase.Active1;
			}

			/// <summary>
			/// Power's effects have finished playing out, play a wind-down animation and resolve the effects.
			/// </summary>
			private void PowerAfterActive()
			{
				ActiveSpecialPower = SpecialPower.None;
				ActivePowerPhase = PowerPhase.AfterActive1;
			}

			private void PowerEnd()
			{
				ActivePowerPhase = PowerPhase.None;
			}

			internal override void Fire(Vector2 target)
			{
				// Position the source around the centre of the player
				var src = new Vector2(
					CollisionBox.Center.X,
					CollisionBox.Y);

				SpawnBullet(ActiveBulletType, src, target, Power, this, false);
				FireTimer = BulletFireRates[ActiveBulletType];

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
				if (HurtTimer <= 0)
				{ }
				else
					HurtTimer -= elapsedGameTime.Milliseconds;

				// todo: relegate outside player update routines to this method
				// Run down player invincibility
				if (InvincibleTimer <= 0)
				{ }
				else
					InvincibleTimer -= elapsedGameTime.Milliseconds;

				// Run down custom weapons
				if (ActiveBulletType == BulletType.Player)
				{ }
				else if (WeaponTimer > 0)
					WeaponTimer -= elapsedGameTime.Milliseconds;
				else
					SwapBulletType();

				// Run down player lightgun animation
				if (FireTimer > 0)
					--FireTimer;
				else
					LastAimMotion = Vector2.Zero;
			
				/* Player special powers */
				
				// Move through the power animations and effects
				if (ActiveSpecialPower != SpecialPower.None)
				{
					// Advance phases
					PowerTimer += elapsedGameTime.Milliseconds;
					if (PowerTimer >= PowerPhaseDurations[ActivePowerPhase])
					{
						++ActivePowerPhase;
					}

					// Power phases
					if (PowerTimer < PowerPhaseDurations[ActivePowerPhase])
					{ }
					else
					{
						switch (ActivePowerPhase)
						{
							case PowerPhase.BeforeActive1:
								// Start power effects
								PowerActive();
								break;
							case PowerPhase.Active4:
								// End power effects
								PowerAfterActive();
								break;
							case PowerPhase.AfterActive2:
								// Return to usual game flow
								PowerEnd();
								break;
						}
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
								if (Position.X + CollisionBox.Width < _right)
									Position.X += Speed;
								else
									Position.X = _right - CollisionBox.Width;
								break;
							case Move.Left:
								SpriteMirror = SpriteEffects.FlipHorizontally;
								if (Position.X > _left)
									Position.X -= Speed;
								else
									Position.X = _left;
								break;
						}
					}

					AnimationTimer += elapsedGameTime.Milliseconds;
					AnimationTimer %= PlayerRunFrames * PlayerAnimTimescale;

					// Update collision box values to sync with position
					CollisionBox.X = (int)Position.X;
				}
			}
			
		private void DrawPlayerHud(SpriteBatch b) {
			// Flipped HUD element positions for player 2
			var xPosForPlayer = 0;

			// Player portrait
			var whichPortrait = 0; // Default
			var whichBackdrop = (int)Swatch.Blue;

			if (Health == 0 && _stageMilliseconds % 1000 < 400)
			{
				whichPortrait = 7; // Out 2
				whichBackdrop = (int) Swatch.Blue;
			}
			else if (Health == 0)
			{
				whichPortrait = 6; // Out 1
				whichBackdrop = (int) Swatch.Blue;
			}
			else if (ActivePowerPhase == PowerPhase.AfterActive2)
			{
				whichPortrait = 5; // Power end 2
				whichBackdrop = (int) Swatch.LightBlue;
			}
			else if (ActivePowerPhase == PowerPhase.AfterActive1)
			{
				whichPortrait = 4; // Power end 1
				whichBackdrop = (int) Swatch.LightBlue;
			}
			else if (ActivePowerPhase == PowerPhase.BeforeActive1
			         || ActivePowerPhase == PowerPhase.BeforeActive2)
			{
				whichPortrait = 3; // Power start
				whichBackdrop = (int) Swatch.LightBlue;
			}
			else if (HurtTimer > 0)
			{
				whichPortrait = 2; // Hurt
				whichBackdrop = (int) Swatch.LightRed;
			}
			else if (RespawnTimer > 0)
			{
				whichPortrait = 1; // Respawn and end-of-stage pose
				whichBackdrop = (int) Swatch.LightBlue;
			}
			
			xPosForPlayer = IsPlayerOne 
				? _hudPortraitDstX 
				: _right - HudPortraitSprite.Width * SpriteScale;
				
			// Frame
			b.Draw(
				FillColours,
				new Rectangle(
					xPosForPlayer,
					_hudPortraitDstY,
					HudPortraitSprite.Width * SpriteScale,
					HudPortraitSprite.Height * SpriteScale),
				new Rectangle((int)Swatch.White, 0, 1, 1),
				Color.White,
				0.0f,
				Vector2.Zero,
				SpriteEffects.None,
				1f - 1f / 10000f - 1f / 10000f);
			// Backdrop
			b.Draw(
				FillColours,
				new Rectangle(
					(xPosForPlayer) + 1 * SpriteScale,
					(_hudPortraitDstY) + 1 * SpriteScale,
					(HudPortraitSprite.Width - 2) * SpriteScale,
					(HudPortraitSprite.Height - 2) * SpriteScale),
				new Rectangle(whichBackdrop, 0, 1, 1),
				Color.White,
				0.0f,
				Vector2.Zero,
				SpriteEffects.None,
				1f - 1f / 10000f);
			// Character
			b.Draw(
				IsPlayerOne 
					? _arcadeTexture 
					: _player2Texture,
				new Rectangle(
					xPosForPlayer,
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
				IsPlayerOne ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
				1f);

			xPosForPlayer = IsPlayerOne
				? _hudLifeDstX
				: _right - HudPortraitSprite.Width * SpriteScale - HudLifeSprite.Width * SpriteScale;
			for (var i = 0; i < HealthMax; ++i)
			{
				// Player health icons, lost health greyed out
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						xPosForPlayer + HudLifeSprite.Width * SpriteScale * i 
						* (IsPlayerOne ? 1 : -1),
						_hudLifeDstY,
						HudLifeSprite.Width * SpriteScale,
						HudLifeSprite.Height * SpriteScale),
					HudLifeSprite,
					i < Health 
					 && (Health > 1 || Health == 1 && _stageMilliseconds % 400 < 200) 
						? Color.White 
						: Color.DarkSlateBlue,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f);
			}
			
			xPosForPlayer = IsPlayerOne
				? _hudEnergyDstX
				: _right - HudPortraitSprite.Width * SpriteScale - HudEnergySprite.Width * SpriteScale;
			for (var i = 0; i < EnergyMax; ++i)
			{
				// Player energy icons, inactive ones greyed out
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						xPosForPlayer + HudEnergySprite.Width * SpriteScale * i
						* (IsPlayerOne ? 1 : -1),
						_hudEnergyDstY,
						HudEnergySprite.Width * SpriteScale,
						HudEnergySprite.Height * SpriteScale),
					HudEnergySprite,
					i < Energy 
						? Color.White
						: Color.DarkSlateBlue,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f);
			}
		}

			public override void Draw(SpriteBatch b)
			{
				// Draw HUD elements
				DrawPlayerHud(b);

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
							IsPlayerOne 
								? _arcadeTexture 
								: _player2Texture,
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
					IsPlayerOne 
						? _arcadeTexture 
						: _player2Texture,
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
			public Rectangle MovementTarget;
			public List<Vector2> AimTargets = new List<Vector2>();

			public int IdleTimer;

			public Monster(MonsterSpecies species, Rectangle collisionBox, int health, int healthMax = -1, int speed = 0) 
				: base(collisionBox, health, healthMax, speed)
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
			/// Deducts health from the monster, dying at 0.
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

				SpawnBullet(type, src, where, Power, this, false);
			}

			internal void GetNewAimTargets()
			{
				AimTargets.Clear();
				if (MonsterBullets[Species] == 1)
				{
					var target = new Vector2(0f, _player.CollisionBox.Center.Y);

					// Fire as close to the player as possible within the aim bounds for this monster
					if (_player.CollisionBox.X < CollisionBox.Center.X - MonsterAimWidth[Species])
						target.X = _player.CollisionBox.Center.X;
					else if (_player.CollisionBox.Center.X > CollisionBox.Center.X + MonsterAimWidth[Species])
						target.X = CollisionBox.Center.X + MonsterAimWidth[Species];
					else
						target.X = _player.CollisionBox.Center.X;

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
					xpos = Game1.random.Next(_width / 8, _width / 2);

					// Initial target moves a short distance into the screen
					if (Position.X <= _left)
					{
						// Spawn from left side
						xpos = _left + xpos;
					}
					else if (Position.X + CollisionBox.Width >= _right)
					{
						// Spawn from right side
						xpos = _right - xpos;
					}
					
					// Repeat targets swap sides
					else if (Position.X < _centre.X)
					{
						xpos = _right - xpos;
					}
					else
					{
						xpos = _left + xpos;
					}

					// Avoid stutter-stepping
					if (Math.Abs(Position.X - xpos) < CollisionBox.Width * 2)
					{
						xpos += (int)(Position.X - xpos);
					}
				}

				SpriteMirror = Position.X < _centre.X 
					? SpriteEffects.None
					: SpriteEffects.FlipHorizontally;
				MovementTarget = new Rectangle(xpos, (int)Position.Y, CollisionBox.Width, CollisionBox.Height);

				Log.D($"{Species} moving at {CollisionBox.Center.ToString()} => {MovementTarget.Center.ToString()}, " + $"{(Position.X < _centre.X ? "left" : "right")} side.");
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
			/// Rolls for a chance to create a new Powerup object at some position.
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
				      + $"time={LootRollGets[LootDrops.Time] * rate:0.0000} " 
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
				//Log.D($"{Species} died");
				Game1.playSound("Cowboy_monsterDie");
				_enemies.Remove(this);

				// Subtract its health from the enemy power level
				if (_stageEnemyHealth > 0)
				{
					_stageEnemyHealth -= HealthMax;
					if (_stageEnemyHealth <= 0)
					{
						EndCurrentStage();
					}
				}
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
								CollisionBox.Width = MonsterSrcRects[Species].Width * (CollisionBox.Y < _height / 2
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

				var scale = ScaleWithDistance && CollisionBox.Y < _height / 2
					? SpriteScale / 2 
					: SpriteScale;
				
				// Crop the monster to remain in the game bounds for the illusion of a screen limit
				var clipRect = GetSpriteDimensionsVisibleAtGameBounds(MonsterSrcRects[Species], CollisionBox, scale);
				var flipOffset = clipRect.X > 0 && CollisionBox.X > _centre.X 
				                  || clipRect.Y > 0 && CollisionBox.Y > _centre.Y;

				// Draw the monster
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)Position.X + (flipOffset ? 0 : clipRect.X * scale),
						(int)Position.Y + (flipOffset ? 0 : clipRect.Y * scale),
						clipRect.Width * scale,
						clipRect.Height * scale),
					new Rectangle(
						MonsterSrcRects[Species].X + MonsterSrcRects[Species].Width * whichFrame + clipRect.X,
						MonsterSrcRects[Species].Y + clipRect.Y,
						clipRect.Width,
						clipRect.Height),
					HurtTimer <= 0 
						? Color.White
						: Color.Red,
					0.0f,
					Vector2.Zero,
					SpriteMirror,
					Position.Y / 10000f + 1f / 1000f);
				
				// Draw the monster's shadow
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

		public class Powerup : ArcadeInteractable
		{
			public readonly LootDrops Type;
			public float YOffset;
			public long Duration;

			public Powerup(LootDrops type, Rectangle collisionBox) : base(collisionBox)
			{
				// Pick a texture for the powerup drop
				switch (type)
				{ // HARD Y
					case LootDrops.Cake:
						// Decide which cake to show (or makito)
						var d = Game1.random.NextDouble();
						if (d > 0.05)
						{
							var x = (int)(CakesFrames * (d * Math.Floor(10d / (CakesFrames))));
							TextureRect = new Rectangle(TD * x, TD, TD, TD);
							Log.D($"Chose cake no.{x}");
						}
						else
						{
							TextureRect = new Rectangle(0, FillColours.Height, TD, TD - FillColours.Height);
							Log.D("Chose makito");
						}
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
					case LootDrops.Megahealth:
						TextureRect = new Rectangle(TD * 3, 0, TD, TD);
						break;
					case LootDrops.None:
					default:
						throw new NotImplementedException($"Powerup: {type} not implemented :(");
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
				if (Duration <= 0)
				{
					Log.D($"{Type} expired at {Position.X}, {Position.Y}");
					_powerups.Remove(this);
				}

				// Powerup dropping in from spawn
				var endpoint = _player.CollisionBox.Y + _player.CollisionBox.Height - CollisionBox.Height;
				if (Position.Y < endpoint)
					Position.Y = Math.Min(endpoint, Position.Y + 4 * SpriteScale);
				else
					Duration -= elapsedGameTime.Milliseconds; // Run down duration from the time it reaches the ground

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
				
				var scale = ScaleWithDistance && CollisionBox.Y < _height / 2
					? SpriteScale / 2 
					: SpriteScale;
				
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
					SpriteMirror,
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
		
		public class Bullet : ArcadeInteractable
		{
			public ArcadeActor Owner;
			public int Power;
			public Vector2 Motion;          // Line of motion between spawn and dest
			public Vector2 MotionCur;       // Current position along vector of motion between spawn and dest
			public float Rotation;          // Angle of vector between spawn and dest
			public float RotationCur;       // Current spin

			public BulletType Type;

			public Vector2 Origin;
			public Vector2 Target;

			public Bullet(BulletType type, int power, ArcadeActor owner, Rectangle collisionBox, Vector2 motion, float rotation, Vector2 target, float speed, bool rotate)
				: base(collisionBox, speed)
			{
				Type = type;
				Power = power;
				Owner = owner;
				
				var xOffset = 0;
				switch (type)
				{
					case BulletType.Player:
						xOffset = 0;
						break;
					case BulletType.Gun:
						xOffset = TD;
						break;
					case BulletType.Junk:
						xOffset = TD * 2 + (int)(BulletSize[type] * BulletFrames[type] / Game1.random.NextDouble());
						break;
					case BulletType.Petal:
						SpriteMirror = Game1.random.NextDouble() < 0.75
							? SpriteEffects.None
							: SpriteEffects.FlipHorizontally;
						xOffset = TD * 6;
						break;
				}
				TextureRect = new Rectangle(xOffset, BulletSpriteY, BulletSize[type], BulletSize[type]);

				Motion = motion;
				MotionCur = motion;
				Rotation = rotate ? rotation : 0f;
				RotationCur = Rotation;

				Origin = new Vector2(collisionBox.Center.X, collisionBox.Center.Y);
				Target = target;

				SpriteMirror = (Origin.X < Target.X ? SpriteEffects.FlipHorizontally : 0)
				               | (Origin.Y < Target.Y ? SpriteEffects.FlipVertically : 0);

				//Log.D($"Bullet  : {Type} :" + $" x = {CollisionBox.X}, y = {CollisionBox.Y}," + $" w = {CollisionBox.Width}, h = {CollisionBox.Height}");
				//Log.D($"TexRect : {Type} :" + $" x = {TextureRect.X}, y = {TextureRect.Y}," + $" w = {TextureRect.Width}, h = {TextureRect.Height}");
				//Log.D($"Firing trajectory: {Origin} => {Target}" + $"\nmotion x={Motion.X:0.000} y={Motion.Y:0.000}");
			}

			/// <summary>
			/// Update on-screen bullets per tick, removing as they travel offscreen or hit a target.
			/// </summary>
			public override void Update(TimeSpan elapsedGameTime)
			{
				// Animate sprite
				if (BulletAnimations[Type].Length > 1)
				{
					AnimationTimer += elapsedGameTime.Milliseconds;
					AnimationTimer %= BulletAnimations[Type].Length * BulletAnimTimescale;
				}

				// Remove offscreen bullets
				if (!(CollisionBox.X > _left && CollisionBox.X < _right && CollisionBox.Y > _top && CollisionBox.Y < _bottom))
				{
					//Log.D($"Bullet : {Type} : out of bounds");
					
					if (Owner is Player)
						_playerBullets.Remove(this);
					else if (Owner is Monster)
						_enemyBullets.Remove(this);
					else if (_extraBullets.Contains(this)) // Let petals float around out of bounds for a while
						if (!(CollisionBox.Y < _top && Target.Y > _top)
						&& !(CollisionBox.X < _left && Target.X > _left))
							_extraBullets.Remove(this);
				}

				// Contact checks
				if (Owner is Player)
				{   // Damage any monsters colliding with bullets
					for (var j = _enemies.Count - 1; j >= 0; --j)
					{
						if (CollisionBox.Intersects(_enemies[j].CollisionBox))
						{
							Log.D($"Bullet : {Type} : hit {_enemies[j].Species}");

							Game1.playSound("cowboy_monsterhit");

							_playerBullets.Remove(this);
							if (_enemies[j].HurtTimer <= 0)
								_enemies[j].TakeDamage(Power);
						}
					}
				}
				else if (Owner is Monster)
				{	// Damage the player
					if (_player.InvincibleTimer <= 0 && _player.RespawnTimer <= 0f)
					{
						if (CollisionBox.Intersects(_player.CollisionBox))
						{
							Log.D($"Bullet : {Type} : hit player");

							Game1.playSound("breakingGlass");

							_enemyBullets.Remove(this);
							if (CollisionBox.Intersects(_player.CollisionBox))
								_player.TakeDamage(Power);
						}
					}
				}

				// Update bullet positions
				Position += Motion * BulletSpeed[Type] * SpriteScale;

				// Update collision box values to sync with position
				CollisionBox.X = (int)Position.X;
				CollisionBox.Y = (int)Position.Y;
				
				// Scale down distant bullets
				if (ScaleWithDistance)
					CollisionBox.Width = BulletSize[Type] * (CollisionBox.Y < _height / 2
						? SpriteScale / 2
						: SpriteScale);
			}

			public override void Draw(SpriteBatch b)
			{
				var scale = ScaleWithDistance && CollisionBox.Y < _height / 2
					? SpriteScale / 2 
					: SpriteScale;
				var whichFrame = BulletAnimations[Type][AnimationTimer / BulletAnimTimescale];

				var colorTint = Color.White;
				if (!_onTitleScreen)
				{}
				// Petal colours
				else if (Math.Abs(_left - Origin.X) % 10 > 4)
					colorTint = Color.LightPink;
				else if (Math.Abs(_left - Origin.X) % 10 > 2)
					colorTint = Color.PaleVioletRed;

				// Crop the bullet to remain in the game bounds for the illusion of a screen limit
				var clipRect = GetSpriteDimensionsVisibleAtGameBounds(TextureRect, CollisionBox, scale);
				var flipOffset = clipRect.X > 0 && CollisionBox.X > _centre.X 
				                 || clipRect.Y > 0 && CollisionBox.Y > _centre.Y;
				
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)Position.X + (flipOffset ? 0 : clipRect.X * scale),
						(int)Position.Y + (flipOffset ? 0 : clipRect.Y * scale),
						clipRect.Width * scale,
						clipRect.Height * scale),
					new Rectangle(
						TextureRect.X + TextureRect.Width * whichFrame + clipRect.X,
						TextureRect.Y + clipRect.Y,
						clipRect.Width,
						clipRect.Height),
					colorTint,
					Rotation,
					Vector2.Zero,
					SpriteMirror,
					Position.Y / 10000f);
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
