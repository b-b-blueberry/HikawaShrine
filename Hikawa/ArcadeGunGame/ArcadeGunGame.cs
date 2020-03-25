using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
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
			Player,
			Bullet,
			Bomb,
			Energy
		}
		private static readonly Dictionary<BulletType, int> BulletSize = new Dictionary<BulletType, int>
		{
			{BulletType.Player, TD},
			{BulletType.Bullet, TD},
			{BulletType.Bomb, 0},
			{BulletType.Energy, TD}
		};
		private static readonly Dictionary<BulletType, Vector2> BulletSpeed = new Dictionary<BulletType, Vector2>
		{
			{BulletType.Player, new Vector2(4.0f, 4.0f) * SpriteScale},
			{BulletType.Bullet, new Vector2(1.0f, 1.0f) * SpriteScale},
			{BulletType.Bomb, new Vector2(1.0f, 1.0f) * SpriteScale},
			{BulletType.Energy, new Vector2(1.0f, 1.0f) * SpriteScale}
		};
		private static readonly Dictionary<BulletType, float> BulletSpin = new Dictionary<BulletType, float>
		{ // todo: add values in radians
			{BulletType.Player, 0f},
			{BulletType.Bullet, 0f},
			{BulletType.Bomb, 0f},
			{BulletType.Energy, 0f}
		};
		// Loot drops and powerups
		public enum LootDrops
		{
			None,
			Cake,
			Life,
			Energy
		}
		private static readonly Dictionary<LootDrops, int> LootDurations = new Dictionary<LootDrops, int>
		{
			{LootDrops.None, 0},
			{LootDrops.Cake, 11},
			{LootDrops.Life, 9},
			{LootDrops.Energy, 7}
		};
		// Monsters
		public enum MonsterSpecies
		{
			// todo: monsters!
		}

		/* Game attributes */

		// Game values
		private const int GameLivesDefault = 1;
		private const int GameHealthMax = 3;
		private const int GameEnergyMax = 7;
		private const int GameEnergyThresholdLow = 3;
		private const int GameMoveSpeed = 5;
		private const int GameDodgeDelay = 500;
		private const int GameFireDelay = 20;
		private const int GameInvincibleDelay = 5000;
		private const int GameDeathDelay = 3000;
		private const int GameEndDelay = 5000;
		private const int GamePlayerDamage = 1;
		private const int GameEnemyDamage = 1;
		private const int DefaultStageTimeLimit = 90;

		// Score values
		private const int ScoreCake = 10000;
		private const int ScoreBread = 125000;

		/* Sprite attributes */

		private const float PlayerAnimTimescale = 200f;
		private const float UiAnimTimescale = 85f;
		private const int SpriteScale = 2;
		private const int CursorScale = 4;
		private const int TD = 16;
		// boundary dimensions for gameplay, menus, and cutscenes
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

		// Player special power animation
		private const int TimeToSpecialPhase1 = 1000; // stand ahead
		private const int TimeToSpecialPhase2 = TimeToSpecialPhase1 + 1400; // raise compact

		// World special power effects
		private const int TimeToPowerPhase1 = 1500; // activation
		private const int TimeToPowerPhase2 = TimeToPowerPhase1 + 3500; // white glow
		private const int TimeToPowerPhase3 = TimeToPowerPhase2 + 1500; // cooldown

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
		public static readonly Rectangle ProjectileSprite = new Rectangle( // HARD Y
			0, TD * 4, TD, TD);

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
		private const int PlayerBodyRunX = PlayerX;
		private const int PlayerBodyRunFireX = PlayerBodyRunX + PlayerW * PlayerRunFrames;
		private const int PlayerBodySideFireX = PlayerBodyRunFireX + PlayerW * PlayerRunFrames;
		private const int PlayerBodyUpFireX = PlayerBodySideFireX + PlayerW;
		// Split-body arm frames
		private const int PlayerLegsY = PlayerBodyY + PlayerSplitWH;
		private const int PlayerArmsX = PlayerLegsRunX + PlayerW * PlayerRunFrames;
		private const int PlayerArmsY = PlayerLegsY;
		// Special powers
		private const int PowerFxY = TD * 5; // HARD Y

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
		private static int whichPlaythrough;
		private static int _stageTimer;
		private static long _stageMilliseconds;
		private static int _totalTimer;
		private static int _totalScore;
		private static int _totalShotsSuccessful;
		private static int _totalShotsFired;
		private static int _gameOverOption;

		/* HUD graphics */

		private static Texture2D _arcadeTexture;
		private static Color _screenFlashColor;
		private static float _cutsceneBackgroundPosition;
		// Player score
		private static int HudScoreDstX;
		private static int HudScoreDstY;
		// Player life
		private static int HudLifeDstX;
		private static int HudLifeDstY;
		// Player energy
		private static int HudEnergyDstX;
		private static int HudEnergyDstY;
		// Game world and stage
		private static int HudWorldDstX;
		private static int HudWorldDstY;
		// Stage timer
		private static int HudTimeDstX;
		private static int HudTimeDstY;

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
			if (_playMusic)
				Game1.changeMusicTrack("dog_bark", true, Game1.MusicContext.MiniGame);

			// Load arcade game assets
			BlackoutPixel.SetData(new [] { Color.Black });
			_arcadeTexture = Helper.Content.Load<Texture2D>(
				Path.Combine("assets", ModConsts.SpritesDirectory, 
					$"{ModConsts.ArcadeSpritesFile}.png"));
			// Reload assets customised by the arcade game
			// ie. LooseSprites/Cursors
			Helper.Events.GameLoop.UpdateTicked += InvalidateCursorsOnNextTick;

			// Init game statistics
			_totalShotsSuccessful = 0;
			_totalShotsFired = 0;
			_totalTimer = 0;

			// Go to the title screen
			if (IsDebugMode && !ModEntry.Instance.Config.DebugArcadeSkipIntro)
				ResetAndReturnToTitle();
			else
				Reset();
		}

		#region Player input methods

		public void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (_onTitleScreen)
				_cutscenePhase++; // Progress through the start menu cutscene
			if (_cutsceneTimer <= 0 
			    && _player.PowerTimer <= 0 && _player.SpecialTimer <= 0 
			    && _player.RespawnTimer <= 0 && _gameEndTimer <= 0 && _gameRestartTimer <= 0 
			    && _player.FireTimer <= 1)
				_player.Fire(); // Fire lightgun trigger
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
						Log.D(_player.Health < GameHealthMax
							? $"_health : {_player.Health} -> {++_player.Health}"		// Modifies health value
							: $"_health : {_player.Health} == GameHealthMax");
						break;
					case Keys.D2:
						Log.D(_player.Energy < GameEnergyMax
							? $"_energy : {_player.Energy} -> {++_player.Energy}"		// Modifies energy value
							: $"_energy : {_player.Energy} == GameEnergyMax");
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
					if (_player.SpecialTimer <= 0 && _player.Energy >= GameEnergyThresholdLow)
					{
						_player.SpriteMirror = SpriteEffects.None;
						_player.SpecialStart();
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

		public bool overrideFreeMouseMovement() { return true; }

		public void receiveEventPoke(int data) { }

		public string minigameId() { return ModConsts.ArcadeMinigameId; }

		public bool doMainGameUpdates() { return false; }

		public bool forceQuit() { return false; }

		#endregion

		#region Minigame functional methods

		private static void InvalidateCursorsOnNextTick(object sender, UpdateTickedEventArgs e)
		{
			Helper.Events.GameLoop.UpdateTicked -= InvalidateCursorsOnNextTick;
			Helper.Content.InvalidateCache("LooseSprites/Cursors");
		}

		public void changeScreenSize()
		{
			// TODO : include GameX and GameY into position calculations

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
			HudScoreDstX = _gamePixelDimen.X;
			HudScoreDstY = 0;
			// Player life
			HudLifeDstX = _gamePixelDimen.X;
			HudLifeDstY = _gameEndPixel.Y;
			// Player energy
			HudEnergyDstX = _gamePixelDimen.X;
			HudEnergyDstY = _gameEndPixel.Y + HudLifeSprite.Height * SpriteScale;
			// Game world and stage
			HudWorldDstX = _gameEndPixel.X;
			HudWorldDstY = _gameEndPixel.Y;
			// Stage time remaining
			HudTimeDstX = _gameEndPixel.X;
			HudTimeDstY = _gamePixelDimen.Y - HudDigitSprite.Height * SpriteScale;
			/*
			Log.D("_gamePixelDimen:\n"
				  + $"({Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width} - {MapWidthInTiles * TD * SpriteScale}) / 2, "
			      + $"({Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height} - {MapHeightInTiles * TD * SpriteScale}) / 2\n"
			      + $"x = {_gamePixelDimen.X}, y = {_gamePixelDimen.Y}",
				IsDebugMode);
			Log.D("GameEndCoords:\n"
			      + $"{_gamePixelDimen.X} + {MapWidthInTiles * TD} * {SpriteScale}\n"
			      + $"= {_gameEndPixel.X}, {_gameEndPixel.Y}",
				IsDebugMode);
			Log.D("_gamePixelDimen:\n"
				  + $"w = {_gamePixelDimen.Width}, h = {_gamePixelDimen.Height}",
				IsDebugMode);
			*/
		}

		private static void QuitMinigame()
		{
			if (Game1.currentMinigame == null && Game1.IsMusicContextActive(Game1.MusicContext.MiniGame))
				Game1.stopMusicTrack(Game1.MusicContext.MiniGame);
			if (Game1.currentLocation != null
			    && Game1.currentLocation.Name.Equals((object)"Saloon") && Game1.timeOfDay >= 1700)
				Game1.changeMusicTrack("Saloon1");
			Game1.currentMinigame = null;
			Helper.Content.InvalidateCache("LooseSprites/Cursors");
		}

		public void unload()
		{
			_player.ResetLife();
		}

		/// <summary>
		/// Starts running down the clock to return to the title screen after resetting the game state.
		/// </summary>
		public static void GameOver()
		{
			_onGameOver = true;
			_gameRestartTimer = 2000;
		}

		/// <summary>
		/// Completely resets all game actors, objects, players, and world state.
		/// Returns to the title screen.
		/// </summary>
		private static void Reset()
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
			// todo: reduce score with formula
			var reducedScore = 0;
			_totalScore = Math.Min(0, reducedScore);

			// Reset the game world
			ResetWorld();
		}

		/// <summary>
		/// Flags for  to title screen start prompt
		/// </summary>
		public static void ResetAndReturnToTitle()
		{
			Reset();
			_onTitleScreen = true;
		}

		private static void ResetWorld()
		{
			_whichStage = 0;
			_whichWorld = 0;
			whichPlaythrough = 0;
			_onGameOver = false;
		}

		#endregion

		#region Game progression methods

		private void EndCurrentStage()
		{
			_player.MovementDirections.Clear();

			// todo: set cutscenes, begin events, etc
		}

		private void EndCurrentWorld()
		{
			// todo: set cutscenes, begin events, etc
		}

		private static void StartNewStage()
		{
			++_whichStage;
			_stageMap = GetMap(_whichStage);
			// todo: set timer per stage/world/boss
			_stageTimer = DefaultStageTimeLimit;
			_stageMilliseconds = 0;

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

			++whichPlaythrough;
			_whichWorld = -1;
			StartNewWorld();
		}

		public static int[,] GetMap(int wave)
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

		/// <summary>
		/// Creates a new bullet object at a point on-screen for either the player or some monster.
		/// </summary>
		/// <param name="where">Spawn position.</param>
		/// <param name="dest">Target for vector of motion from spawn position.</param>
		/// <param name="damage"></param>
		/// <param name="which">Projectile behaviour, also determines whether player or not.</param>
		private static void SpawnBullet(Vector2 where, Vector2 dest, int damage, BulletType which)
		{
			// Rotation to aim towards target
			var radiansBetween = Vector.RadiansBetween(dest, where);
			radiansBetween -= (float)(Math.PI / 2.0d);
			//Log.D($"RadiansBetween {where}, {dest} = {radiansBetween:0.00}rad", _isDebugMode);

			// Vector of motion
			var motion = Vector.PointAt(where, dest);
			motion.Normalize();
			_player.LastAimVector = motion;
			//Log.D($"Normalised motion = {motion.X:0.00}x, {motion.Y:0.00}y, mirror={_player.SpriteMirror}");

			// Spawn position
			var position = where + motion * (BulletSpeed[which] * 5);
			var collisionBox = new Rectangle(
				(int)position.X,
				(int)position.Y,
				BulletSize[which],
				BulletSize[which]);

			// Add the bullet to the active lists for the respective spawner
			if (which == BulletType.Player)
				_playerBullets.Add(new Bullet(collisionBox, motion, radiansBetween, which, dest));
			else
				_enemyBullets.Add(new Bullet(collisionBox, motion, radiansBetween, which, dest));
		}

		#region Per-tick updates

		private static void UpdateMenus(TimeSpan elapsedGameTime)
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
					{
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
					}
					case 1:
					{
						if (_cutsceneTimer >= TimeToTitlePhase1)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					}
					case 2:
					{
						if (_cutsceneTimer >= TimeToTitlePhase2)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					}
					case 3:
					{
						if (_cutsceneTimer >= TimeToTitlePhase3)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					}
					case 4:
					{
						if (_cutsceneTimer < TimeToTitlePhase4)
							_cutsceneTimer = TimeToTitlePhase4;
						break;
					}
					case 5:
						// End the cutscene and begin the game
						// after the user clicks past the end of intro cutscene (phase 4)
						whichPlaythrough = -1;
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

		public bool tick(GameTime time)
		{
			/* Game Management */

			var elapsedGameTime = time.ElapsedGameTime;
			//Log.D($"_stageMilliseconds: {_stageMilliseconds} | _stageTimer: {_stageTimer}");

			// Per-second checks
			if ((_stageMilliseconds / elapsedGameTime.Milliseconds) % (1000 / elapsedGameTime.Milliseconds) == 0)
			{
				if (!_onTitleScreen && _cutsceneTimer <= 0 && _player.SpecialTimer <= 0 && _player.PowerTimer <= 0)
				{
					// Count up total playthrough timer
					++_totalTimer;

					// Count down stage timer
					if (--_stageTimer <= 0)
					{
						//GameOver(); // todo re-enable game over by timeout
						_stageTimer = DefaultStageTimeLimit;
					}
				}
			}

			// Exit the game
			if (_player.HasPlayerQuit)
			{
				QuitMinigame();
				return true;
			}

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

			// Run down player invincibility
			if (_player.InvincibleTimer > 0)
				_player.InvincibleTimer -= elapsedGameTime.Milliseconds;

			// Run down player lightgun animation
			if (_player.FireTimer > 0)
				--_player.FireTimer;
			else
				_player.LastAimVector = Vector2.Zero;

			// Handle player special trigger
			if (_player.SpecialTimer > 0)
			{
				_player.SpecialTimer += elapsedGameTime.Milliseconds;

				// Progress through player special trigger animation

				// Events 
				if (_player.AnimationPhase == PlayerSpecialFrames)
					// Transfer into the active special power
					_player.SpecialEnd();
				// Timers
				if (_player.AnimationPhase == 0)
				{
					if (_player.SpecialTimer >= TimeToSpecialPhase1)
						++_player.AnimationPhase;
				}
				else if (_player.AnimationPhase == 1)
				{
					if (_player.SpecialTimer >= TimeToSpecialPhase2)
						++_player.AnimationPhase;
				}
			}
			// Handle player power animations and effects
			else if (_player.ActiveSpecialPower != SpecialPower.None)
			{
				_player.PowerTimer += elapsedGameTime.Milliseconds;

				// Progress through player power

				// Events
				if (_player.AnimationPhase == PlayerPowerFrames)
				{
					// Return to usual game flow
					_player.PowerEnd();
				}
				
				// Timers
				if (_player.AnimationPhase == 0)
				{
					// Frequently spawn bullets raining down on the field
					if (_player.PowerTimer % 5 == 0)
					{
						
					}
					// Progress to the next phase
					if (_player.PowerTimer >= TimeToPowerPhase1)
						++_player.AnimationPhase;
				}
				else if (_player.AnimationPhase == 1)
				{
					// Progress to the next phase
					if (_player.PowerTimer >= TimeToPowerPhase2)
						++_player.AnimationPhase;
				}
				else if (_player.AnimationPhase == 2)
				{
					// End the power
					if (_player.PowerTimer >= TimeToPowerPhase3)
						++_player.AnimationPhase;
				}
			}

			// Handle other game sprites
			for (var i = _temporaryAnimatedSprites.Count - 1; i >= 0; --i)
			{
				if (_temporaryAnimatedSprites[i].update(time))
					_temporaryAnimatedSprites.RemoveAt(i);
			}


			/* Game Activity */

			if (!_onTitleScreen && _cutsceneTimer <= 0)
			{
				// Update bullets
				for (var i = _playerBullets.Count - 1; i >= 0; --i)
					_playerBullets[i].Update();
				for (var i = _enemyBullets.Count - 1; i >= 0; --i)
					_enemyBullets[i].Update();

				// Update powerups
				for (var i = _powerups.Count - 1; i > 0; --i)
					_powerups[i].Update();

				// Update enemies
				for (var i = _enemies.Count - 1; i > 0; --i)
					_enemies[i].Update();

				// While the player has agency
				if (_player.SpecialTimer <= 0 && _player.PowerTimer <= 0)
				{
					// Run down the death timer
					if (_player.RespawnTimer > 0.0)
						_player.RespawnTimer -= elapsedGameTime.Milliseconds;
					// Run down the stage timer while alive
					else
						_stageMilliseconds += elapsedGameTime.Milliseconds;

					// Handle player movement
					if (_player.MovementDirections.Count > 0)
					{
						switch (_player.MovementDirections.ElementAt(0))
						{
							case Move.Right:
								_player.SpriteMirror = SpriteEffects.None;
								if (_player.Position.X + _player.CollisionBox.Width < _gameEndPixel.X)
									_player.Position.X += GameMoveSpeed;
								else
									_player.Position.X = _gameEndPixel.X - _player.CollisionBox.Width;
								break;
							case Move.Left:
								_player.SpriteMirror = SpriteEffects.FlipHorizontally;
								if (_player.Position.X > _gamePixelDimen.X)
									_player.Position.X -= GameMoveSpeed;
								else
									_player.Position.X = _gamePixelDimen.X;
								break;
						}
					}

					_player.AnimationTimer += elapsedGameTime.Milliseconds;
					_player.AnimationTimer %= (PlayerRunFrames) * PlayerAnimTimescale;

					_player.CollisionBox.X = (int)_player.Position.X;

					// Handle enemy behaviours
					if (_player.Health > 0)
					{

						// todo

					}
				}
			}

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
			if (!ModEntry.Instance.Config.DebugMode || !_playerBullets.Any()) return;

			// create 1x1 white texture for line drawing
			var t = new Texture2D(Game1.graphics.GraphicsDevice, 2, 2);
			t.SetData(new[] { Color.White, Color.White, Color.White, Color.White });

			var startpoint = _playerBullets[_playerBullets.Count - 1].Start;
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
		/// Renders in-game stage background elements.
		/// </summary>
		private static void DrawBackground(SpriteBatch b)
		{
			switch (_player.ActiveSpecialPower)
			{
				case SpecialPower.Normal:
				case SpecialPower.Megaton:
					if (_player.AnimationPhase == 1)
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
					break;
			}
		}

		/// <summary>
		/// Renders a number digit-by-digit on screen to the target rectangle using the arcade number sprites.
		/// </summary>
		/// <param name="number">Number to draw.</param>
		/// <param name="maxDigits">Number of digits to draw. Will draw leading zeroes if number is shorter.</param>
		/// <param name="where">Target rectangle to draw to, corrected to SpriteScale.</param>
		/// <param name="origin">Offset of x,y coordinates to draw to.</param>
		/// <param name="layerDepth">Occlusion value between 0f and 1f.</param>
		private static void DrawDigits(SpriteBatch b, int number, int maxDigits, 
			Rectangle where, Vector2 origin, float layerDepth)
		{
			var digits = 1;
			var divisor = 1;
			while (number > divisor)
			{
				var index = (number % (divisor * 10)) / divisor;
				b.Draw(
					_arcadeTexture,
					new Rectangle( // Draws in reverse from end of screen
						where.X- HudDigitSprite.Width * SpriteScale * digits,
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
			// todo: Draw enemy health bar to destrect width * health / healthmax
			//			or Draw enemy health bar as icons onscreen (much nicer)

			// Note: draw all HUD elements to depth 1f to show through flashes, effects, objects, etc.

			// todo: draw extra flair around certain hud elements
			// see 1581165827502.jpg of viking with hud

			// Stage time remaining countdown
			DrawDigits(b, _stageTimer, 3,
				new Rectangle(HudTimeDstX, HudTimeDstY, HudDigitSprite.Width, HudDigitSprite.Height),
				Vector2.Zero, 1f);

			for (var i = 0; i < _player.Health; ++i)
			{
				// Player health icons
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						HudLifeDstX + HudLifeSprite.Width * SpriteScale * i,
						HudLifeDstY,
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

			for (var i = 0; i < _player.Energy; ++i)
			{
				// Player energy icons
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						HudEnergyDstX + HudEnergySprite.Width * SpriteScale * i,
						HudEnergyDstY,
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
					+ new Vector2(0.0f, 2 / 3),
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
					DrawTracer(b); // DEBUG

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

			public abstract void Update();
			public abstract void Draw(SpriteBatch b);
		}

		/// <summary>
		/// Root of all custom actors.
		/// Able to live and be killed.
		/// </summary>
		public abstract class ArcadeActor : ArcadeObject
		{
			public int Health;

			public int AnimationPhase;
			public float AnimationTimer;

			protected ArcadeActor() { }

			protected ArcadeActor(int health, Rectangle collisionBox) : base(collisionBox)
			{
				Health = health;
			}

			public virtual bool TakeDamage(int damage)
			{
				// Reduce health and check for death
				Health = Math.Max(0, Health - damage);
				return Health > 0;
			}
			public abstract void BeforeDeath();
			public abstract void Die();
		}

		public class Player : ArcadeActor
		{
			public int Lives;
			public int Energy;
			public SpecialPower ActiveSpecialPower;
			public bool HasPlayerQuit;
			public List<Move> MovementDirections = new List<Move>();
			public Vector2 LastAimVector = Vector2.Zero;

			public int FireTimer;
			public int SpecialTimer;
			public int PowerTimer;
			public int InvincibleTimer;
			public float RespawnTimer;

			public Player()
			{
				Reset();
			}

			public void Reset()
			{
				ResetLife();
				ResetEnergy();
				ResetPosition();
				ResetTimers();
			}

			public void ResetLife()
			{
				Health = GameHealthMax;
				Lives = GameLivesDefault;
			}

			public void ResetEnergy()
			{
				Energy = GameEnergyMax; // todo: return to 0
				ActiveSpecialPower = SpecialPower.None;
			}

			public void ResetPosition()
			{
				SpriteMirror = SpriteEffects.None;
				MovementDirections.Clear();

				// Spawn in the bottom-centre of the playable window
				Position = new Vector2(
					_gamePixelDimen.X + _gamePixelDimen.Width / 2 + PlayerW * SpriteScale / 2,
					_gamePixelDimen.Y + _gamePixelDimen.Height - PlayerFullH * SpriteScale
				);
				// Player bounding box only includes the bottom 2/3rds of the sprite
				CollisionBox = new Rectangle(
					(int)Position.X,
					(int)Position.Y,
					PlayerW * SpriteScale,
					PlayerSplitWH * SpriteScale);

				//Log.D("player:" + $"\nx = {CollisionBox.X}, y = {CollisionBox.Y}, w = {CollisionBox.Width}, h = {CollisionBox.Height}");
			}

			public void ResetTimers()
			{
				AnimationPhase = 0;
				AnimationTimer = 0;
				FireTimer = 0;
				SpecialTimer = 0;
				PowerTimer = 0;
				//InvincibleTimer = 0;
				RespawnTimer = 0.0f;
			}

			/// <summary>
			/// Deducts health from the player.
			/// </summary>
			/// <param name="damage">Amount of health to remove at once.</param>
			/// <returns>Whether the player will survive.</returns>
			public override bool TakeDamage(int damage)
			{
				var survives = base.TakeDamage(damage);
				if (!survives)
					return false;

				// todo: animate player

				// Flash translucent red
				_screenFlashColor = new Color(new Vector4(255, 0, 0, 0.25f));
				_screenFlashTimer = 200;
				// Grant momentary invincibility
				InvincibleTimer = GameInvincibleDelay;

				return true;
			}

			/// <summary>
			/// Pre-death effects.
			/// </summary>
			public override void BeforeDeath()
			{
				// todo: fill this function with pre-death animation timer etc

				Game1.playSound("cowboy_dead");
				Die();
			}

			/// <summary>
			/// Deducts a life from the player and starts a respawn or restart game countdown.
			/// </summary>
			public override void Die()
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

				RespawnTimer *= 3f;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="extra">Inherited from endFunction in TemporaryAnimtedSprite.</param>
			public void GameOverCheck(int extra)
			{
				// todo probably remove this function, this and Die() are messed up entirely

				if (Lives >= 0)
				{
					Respawn(); // work out relationship with Die()
					return;
				}
				GameOver();

				++Game1.currentLocation.currentEvent.CurrentCommand; // ??????
			}

			public void Respawn()
			{
				InvincibleTimer = GameInvincibleDelay;
				Health = GameHealthMax;
			}

			public void AddMovementDirection(Move direction)
			{
				if (MovementDirections.Contains(direction))
					return;
				MovementDirections.Add(direction);
			}

			public void PickupLoot(Powerup loot)
			{
				switch (loot.Type)
				{
					case LootDrops.Cake:
					{
						if (loot.TextureRect.Y == 0)
							_totalScore += ScoreBread; // makito bonus
						_totalScore += ScoreCake + loot.TextureRect.X / CakeSprite.Width * 500;
						break;
					}
					case LootDrops.Life:
					{
						Health = Math.Min(GameHealthMax, Health + 1);
						break;
					}
				}
			}

			public void SpecialStart()
			{
				AnimationPhase = 0;
				SpecialTimer = 1;
			}

			public void SpecialEnd()
			{
				// Energy levels between 0 and the low-threshold will use a light special power
				ActiveSpecialPower = Energy >= GameEnergyMax
					? SpecialPower.Normal
					: SpecialPower.Megaton;

				AnimationPhase = 0;
				SpecialTimer = 0;
			}

			public void PowerEnd()
			{
				ActiveSpecialPower = SpecialPower.None;
				AnimationPhase = 0;
				PowerTimer = 0;
				Energy = 0;
			}

			public void Fire()
			{
				// Position the source around the centre of the player
				var src = new Vector2(
					Position.X + PlayerW / 2 * SpriteScale,
					Position.Y + PlayerSplitWH / 2 * SpriteScale);

				// Position the target on the centre of the cursor
				var dest = new Vector2(
					Helper.Input.GetCursorPosition().ScreenPixels.X + TD / 2 * CursorScale,
					Helper.Input.GetCursorPosition().ScreenPixels.Y + TD / 2 * CursorScale);
				SpawnBullet(src, dest, GamePlayerDamage, BulletType.Player);
				FireTimer = GameFireDelay;

				// Mirror player sprite to face target
				if (MovementDirections.Count == 0)
					SpriteMirror = dest.X < Position.X + CollisionBox.Width / 2
						? SpriteEffects.FlipHorizontally
						: SpriteEffects.None;

				Game1.playSound("Cowboy_gunshot");

				++_totalShotsFired;
			}

			public override void Update() { }

			public override void Draw(SpriteBatch b)
			{
				// Flicker sprite visibility while invincible
				if (InvincibleTimer > 0 || InvincibleTimer / 100 % 2 != 0) return;

				var destRects = new Rectangle[3];
				var srcRects = new Rectangle[3];

				// Draw full body action sprites
				if (SpecialTimer > 0)
				{   // Activated special power
					destRects[0] = new Rectangle(
						(int)Position.X,
						(int)Position.Y,
						CollisionBox.Width,
						PlayerFullH * SpriteScale);
					srcRects[0] = new Rectangle(
						PlayerSpecialX + PlayerW * AnimationPhase,
						PlayerFullY,
						PlayerW,
						PlayerFullH);
				}
				else if (ActiveSpecialPower != SpecialPower.None)
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
						+ PlayerW * AnimationPhase,
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
						if (LastAimVector != Vector2.Zero)
						{
							// Firing arms
							if ((int)Math.Ceiling(LastAimVector.X) == (int)SpriteMirror
							|| MovementDirections.Count > 0 && Math.Abs(LastAimVector.Y) >= 0.9f)
								whichFrame = 4; // Aiming backwards, also aiming upwards while running
							else if (Math.Abs(LastAimVector.Y) >= 0.9f) // Aiming upwards while standing
								whichFrame = 5; // invalid index, therefore no visible arms
							else if (LastAimVector.Y < -0.6f) // Aiming low
								whichFrame = 3;
							else if (LastAimVector.Y < -0.2f) // Aiming level
								whichFrame = 2;
							else if (LastAimVector.Y > 0.2f) // Aiming below
								whichFrame = 1;
							destRects[2] = new Rectangle(
								(int)Position.X,
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
							whichFrame = (int)(AnimationTimer / PlayerAnimTimescale);
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
							if (Math.Abs(LastAimVector.Y) >= 0.9f
								|| (int)Math.Ceiling(LastAimVector.X) == (int)SpriteMirror)
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
						var whichFrame = (int)(AnimationTimer / PlayerAnimTimescale);
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
					var whichFrame = (int)(AnimationTimer / PlayerAnimTimescale);
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
					if (LastAimVector != Vector2.Zero)
					{   // Firing legs
						var whichFrame = 0; // Aiming sideways
						if (Math.Abs(LastAimVector.Y) > 0.9f)
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
							Position.Y / 10000f + i / 1000f + 1f / 1000f);
					}
				}
			}
		}

		public class Monster : ArcadeActor
		{
			private Color _tint = Color.White;
			private Color _flashColor = Color.Red;

			public MonsterSpecies Species;
			public float FlashColorTimer;
			public int TicksSinceLastMovement;

			public Monster(MonsterSpecies species, int health, Rectangle collisionBox) : base(health, collisionBox)
			{
				Species = species;
			}

			/// <summary>
			/// Deducts health from the monster.
			/// </summary>
			/// <param name="damage">Amount of health to remove at once.</param>
			/// <returns>Whether the monster will survive.</returns>
			public override bool TakeDamage(int damage)
			{
				++_totalShotsSuccessful;

				var survives = base.TakeDamage(damage);
				if (!survives)
					return false;

				_flashColor = Color.Red;
				FlashColorTimer = 100f;
				return true;
			}

			public override void BeforeDeath()
			{
				// Throw loot upon dying
				GetLootDrop(new Rectangle(
					CollisionBox.X + CollisionBox.Width / 2,
					CollisionBox.Y + CollisionBox.Height / 2,
					TD,
					TD));

				// todo: literally anything at all
				// animations, counters, something

				Die();
			}

			public override void Die()
			{
				_enemies.Remove(this);
			}

			/// <summary>
			/// Create a new Powerup object at some position.
			/// </summary>
			/// <param name="collisionBox">Spawn position and touch bounds.</param>
			public virtual void GetLootDrop(Rectangle collisionBox)
			{
				var rand = Game1.random.NextDouble();
				var which = LootDrops.None;
				if (rand < 0.05)
					which = LootDrops.Life;
				else if (rand < 0.1)
					which = LootDrops.Energy;
				else if (rand < 0.2)
					which = LootDrops.Cake;
				if (which != LootDrops.None)
					_powerups.Add(new Powerup(which, collisionBox));
			}

			public override void Update() { }

			public override void Draw(SpriteBatch b)
			{
				b.Draw(
					_arcadeTexture,
					new Vector2(
						_gamePixelDimen.X + Position.X,
						_gamePixelDimen.Y + Position.Y),
					new Rectangle(
						TD,
						0,
						16,
						16),
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteScale,
					SpriteEffects.None,
					(float)(Position.Y / 10000.0 + 1.0 / 1000.0));
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
				Type = type;
				Duration = LootDurations[type] * 1000;

				// Pick a texture for the powerup drop
				switch (type)
				{
					case LootDrops.Cake:
					{
						// Decide which cake to show (or makito)
						var x = 0;
						var y = 0;
						var d = Game1.random.NextDouble();
						if (d > 0.05)
						{
							x = (int)(10 * (d * Math.Floor(10d / CakesFrames)));
							y = 1;
							Log.D($"Chose cake #{x}");
						}
						else
						{
							Log.D("Chose MAKITO");
						}

						TextureRect = new Rectangle(TD * x, TD * y, TD, TD);
						break;
					}
					case LootDrops.Life:
					{
						TextureRect = new Rectangle(TD * 12, 0, TD, TD);
						break;
					}
					case LootDrops.Energy:
					{
						TextureRect = new Rectangle(TD * 13, 0, TD, TD);
						break;
					}
				}
			}

			/// <summary>
			/// Update dropped loot per tick, removing them as their timer runs down.
			/// </summary>
			public override void Update()
			{
				// Powerup expired
				if (Duration <= 0)
					_powerups.Remove(this);

				if (!CollisionBox.Intersects(_player.CollisionBox)) return;

				// Powerup collected
				_player.PickupLoot(this);
				_powerups.Remove(this);
			}

			public override void Draw(SpriteBatch b)
			{
				if (Duration <= 2000 && Duration / 200 % 2 != 0)
					return;

				b.Draw(
					_arcadeTexture,
					new Vector2(
						_gamePixelDimen.X + CollisionBox.X, 
						_gamePixelDimen.Y + CollisionBox.Y + YOffset),
					new Rectangle(
						TD,
						0, 
						16, 
						16),
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteScale,
					SpriteEffects.None,
					(float)(CollisionBox.Y / 10000.0 + 1.0 / 1000.0));
			}
		}
		
		public class Bullet : ArcadeObject
		{
			public Vector2 Motion;          // Line of motion between spawn and dest
			public Vector2 MotionCur;       // Current position along vector of motion between spawn and dest
			public float Rotation;          // Angle of vector between spawn and dest
			public float RotationCur;       // Current spin
			public BulletType Type;

			public Vector2 Start;
			public Vector2 Target; // DEBUG - target to draw line towards

			public Bullet(Rectangle collisionBox, Vector2 motion, float rotation, BulletType type, Vector2 target)
				: base(collisionBox)
			{
				Motion = motion;
				Rotation = rotation;
				Type = type;
				
				Motion = motion;

				MotionCur = motion;
				RotationCur = type == BulletType.Player ? 0f : rotation;

				Start = new Vector2(collisionBox.Center.X, collisionBox.Center.Y);
				Target = target;

				//Log.D("bullet:" + $"\nCB x = {CollisionBox.X}, y = {CollisionBox.Y}, w = {CollisionBox.Width}, h = {CollisionBox.Height}");
			}

			/// <summary>
			/// Update on-screen bullets per tick, removing as they travel offscreen or hit a target.
			/// </summary>
			public override void Update()
			{
				// Damage any monsters colliding with bullets
				if (Type == BulletType.Player)
				{
					for (var j = _enemies.Count - 1; j >= 0; --j)
					{
						if (!CollisionBox.Intersects(_enemies[j].CollisionBox)) continue;
						if (_enemies[j].TakeDamage(GamePlayerDamage)) continue;
						_enemies[j].Die();
						_playerBullets.Remove(this);
					}
				}
				else
				{
					// Damage the player
					if (_player.InvincibleTimer <= 0 && _player.RespawnTimer <= 0f)
					{
						if (CollisionBox.Intersects(_player.CollisionBox))
						{
							if (!_player.TakeDamage(GameEnemyDamage))
							{
								_player.BeforeDeath();
								_enemyBullets.Remove(this);
							}
						}
					}
				}

				// Update bullet positions
				Position += Motion * BulletSpeed[Type];
				CollisionBox.X = (int)Position.X;
				CollisionBox.Y = (int)Position.Y;

				// Remove offscreen bullets
				if (!CollisionBox.Intersects(_gamePixelDimen))
					_playerBullets.Remove(this);
			}

			public override void Draw(SpriteBatch b)
			{
				b.Draw(
					_arcadeTexture,
					new Vector2(
						CollisionBox.X,
						CollisionBox.Y),
					new Rectangle(
						ProjectileSprite.X + TD * (int)Type,
						ProjectileSprite.Y,
						ProjectileSprite.Width,
						ProjectileSprite.Height),
					Color.White,
					Rotation,
					new Vector2(ProjectileSprite.Width / 2, ProjectileSprite.Height / 2),
					SpriteScale,
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
