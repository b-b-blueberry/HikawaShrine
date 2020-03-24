using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Netcode;

using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Minigames;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using xTile.Dimensions;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa.LightGunGame
{
	internal class LightGunGame : IMinigame
	{
		private readonly IModHelper _helper;
		private readonly bool _isDebugMode;

		/* Constant Values */
		
		// program attributes
		private const float PlayerAnimTimescale = 200f;
		private const float UiAnimTimescale = 30f;
		private const int SpriteScale = 2;
		private const int CursorScale = 4;
		private const int TD = 16;

		#region Aesthetics and interface

		// cutscene timers
		private const int TimerTitlePhase0 = 800;   // blank space after light shaft
		private const int TimerTitlePhase1 = TimerTitlePhase0 + 400;    // red V/
		private const int TimerTitlePhase2 = TimerTitlePhase1 + 400;    // コードネームは
		private const int TimerTitlePhase3 = TimerTitlePhase2 + 400;    // セーラー
		private const int TimerTitlePhase4 = TimerTitlePhase3 + 400;    // © BLUEBERRY 1996
		private const int TimerTitlePhase5 = TimerTitlePhase4 + 400;    // fire to start

		private const int TimerSpecialPhase1 = 1000; // stand ahead
		private const int TimerSpecialPhase2 = TimerSpecialPhase1 + 1400; // raise compact

		private const int TimerPowerPhase1 = 1500;	// activate
		private const int TimerPowerPhase2 = TimerPowerPhase1 + 3500;	// white glow
		private const int TimerPowerPhase3 = TimerPowerPhase2 + 1500;	// cooldown

		#endregion

		// asset attributes

		// gameplay content
		private enum Move
		{
			None,
			Right,
			Left
		}
		private static readonly int[] PowerAnims = {
			0,
			0, 
			1,
			1,
			0
		};
		private enum SpecialPower
		{
			Normal,
			Megaton,
			Sulphur,
			Incense,
			None,
		}
		private enum LightGunAimHeight {
			High,
			Mid,
			Low
		}
		public enum ProjectileType
		{
			Player,
			Debris,
			Bullet,
			Energy
		}
		public enum MenuOptions
		{
			Retry,
			Quit
		}
		public enum LootDrops
		{
			None,
			Cake,
			Life,
			Energy
		}
		public enum LootDurations
		{
			None = 0,
			Cake = 11,
			Life = 9,
			Energy = 7
		}

		// hud graphics
		public const int CrosshairW = TD * 1;
		public const int CrosshairH = TD * 1;
		public const int CrosshairX = TD * 2;
		public const int CrosshairY = TD * 0;

		private const int MapW = 20;
		private const int MapH = MapW;
		private const int HudW = TD * 3;
		private const int HudH = TD;
		private const int GameW = TD * MapW;
		private const int GameH = TD * MapH;
		private const int GameX = 0;
		private const int GameY = 0;

		#region Aesthetics and interface

		/* Miscellaneous object graphics */

		// cakes and sweets
		private const int CakesW = TD;
		private const int CakesH = CakesW;
		private const int CakesX = 0;
		private const int CakesY = TD * 1; // HARD Y
		private const int CakesFrames = 9;
		// hud elements
		private const int HudElemH = TD * 3;
		private const int HudElemX = 0;
		private const int HudElemY = 0; // HARD Y
		// projectiles
		private const int ProjectilesH = TD * 1;
		private const int ProjectilesX = 0;
		private const int ProjectilesY = TD * 4; // HARD Y
		private const int ProjectilesVariants = 2;
		private const int PlayerFiringArmsFrames = 3;

		/* Special power graphics */

		private const int PowerFxY = TD * 5; // HARD Y

		// text string graphics
		private const int StringsH = TD * 1;

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

		/* Title screen graphics */

		// Fire to start
		// above full sailor frames
		private const int TitleStartW = TD * 6;
		private const int TitleStartH = TD * 1;
		private const int TitleStartY = TD * 7; // HARD Y
		// コードネームは
		// beneath split sailor frames
		private const int TitleCodenameW = TD * 4;
		private const int TitleCodenameH = TD * 2;
		private const int TitleCodenameX = TD * 1;
		private const int TitleCodenameY = PlayerLegsY + PlayerSplitWH;
		// セーラー
		// beneath codename text
		private const int TitleSailorW = TD * 7;
		private const int TitleSailorH = TD * 5;
		private const int TitleSailorX = 0;
		private const int TitleSailorY = TitleCodenameY + TitleCodenameH;
		// © テレビ望月・東映動画 / © BLUEBERRY 1996
		// beneath sailor text
		private const int TitleSignatureW = TD * 8;
		private const int TitleSignatureH = TD * 2;
		private const int TitleSignatureX = 0;
		private const int TitleSignatureY = TitleSailorY + TitleSailorH;
		// Red V/
		// beneath split sailor frames, beside sailor text
		private const int TitleRedVW = TD * 5;
		private const int TitleRedVH = TD * 8;
		private const int TitleRedVX = TitleSailorW;
		private const int TitleRedVY = PlayerLegsY + PlayerSplitWH;
		// Masking frame for light-shine effect on intro
		// beneath split sailor frames, beside white V/
		private const int TitleBlackoutX = TitleRedVX + 2 * TitleRedVW;
		private const int TitleBlackoutY = PlayerLegsY + PlayerSplitWH;
		private const int TitleBlackoutW = TD;
		private const int TitleBlackoutH = TD / 2;
		private const int TitleMaskW = TD * 6;
		private const int TitleMaskH = TD * 1;
		private const int TitleMaskX = TitleBlackoutX + TD;
		private const int TitleMaskY = TitleBlackoutY;

		#endregion

		// game attributes
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
		private const int GameLootDuration = 8000;
		private const int GamePlayerDamage = 1;
		private const int GameEnemyDamage = 1;

		private const int PowerupHealth = 0;
		private const int PowerupLives = 2;

		private const int PowerupHealthAmount = 1;
		private const int PowerupLivesAmount = 1;

		private const float AnimCutsceneBackgroundSpeed = 0.1f;

		public static readonly Vector2[] GameProjSpeed = {
				new Vector2(4.0f, 4.0f) * SpriteScale, 
				new Vector2(1.0f, 1.0f) * SpriteScale, 
				new Vector2(1.0f, 1.0f) * SpriteScale, 
				new Vector2(1.0f, 1.0f) * SpriteScale
		};
		public static readonly float[] GameProjRotDelta = {
				0.0f,
				0.0f,
				0.0f,
				0.0f
		};

		/* Gameplay Variables */

		// program
		private static Vector2 _gameStartCoords;
		private static Vector2 _gameEndCoords;
		private int _gameWidth;
		private int _gameHeight;

		// active game actors
		public Vector2 LastPlayerAimVector = Vector2.Zero;
		public List<Bullet> PlayerBullets = new List<Bullet>();
		private static List<Bullet> _enemyBullets = new List<Bullet>();
		private static List<Monster> _enemies = new List<Monster>();
		private static List<TemporaryAnimatedSprite> _temporaryAnimatedSprites = new List<TemporaryAnimatedSprite>();
		private static List<Move> _playerMovementDirections = new List<Move>();
		private static List<Powerup> _powerups = new List<Powerup>();
		private List<Point>[] _spawnQueue = new List<Point>[4];
		private int[,] _stageMap = new int[MapH, MapW];

		// player attributes
		private int _health;
		private int _lives;
		private int _energy;
		private int _whichStage;
		private int _whichWorld;
		private int _whichGame;
		private int _stageTimer;
		private int _totalTimer;
		private int _shotsSuccessful;
		private int _shotsFired;
		private int _score;
		private int _gameOverOption;
		private SpecialPower _activeSpecialPower;
		public Vector2 PlayerPosition;
		public Rectangle PlayerBoundingBox;

		// player state
		private SpriteEffects _mirrorPlayerSprite;
		private bool _hasPlayerQuit;
		
		// ui elements
		private static Texture2D _arcadeTexture;
		private static Color _screenFlashColor;
		private float _cutsceneBackgroundPosition;

		// timers

		// midgame
		private static float _playerAnimationTimer;
		private static int _playerAnimationPhase;
		private static int _playerFireTimer;
		private static int _playerSpecialTimer;
		private static int _playerPowerTimer;
		private static int _playerInvincibleTimer;
		private static int _screenFlashTimer;
		private static float _respawnTimer;
		// postgame lose
		private static int _gameRestartTimer;
		private static int _gameEndTimer;
		// postgame win
		private static int _cutsceneTimer;
		private static int _cutscenePhase;
		// meta
		private static bool _onStartMenu;
		private static bool _onGameOver;
		private static bool _onWorldComplete;
		private static bool _onGameComplete;

		private static bool _playMusic = true;

		private behaviorAfterMotionPause _behaviorAfterPause;
		public delegate void behaviorAfterMotionPause();

		public LightGunGame()
		{
			_helper = ModEntry.Instance.Helper;
			_isDebugMode = ModEntry.Instance.Config.DebugMode;

			changeScreenSize();

			if (ModEntry.Instance.Config.DebugMode && !ModEntry.Instance.Config.DebugArcadeMusic)
				_playMusic = false;
			if (_playMusic)
				Game1.changeMusicTrack("dog_bark", true, Game1.MusicContext.MiniGame);

			// Load arcade game assets
			_arcadeTexture = _helper.Content.Load<Texture2D>(
				Path.Combine("assets", ModConsts.SpritesDirectory, 
					$"{ModConsts.ArcadeSpritesFile}.png"));
			// Reload assets customised by the arcade game
			// ie. LooseSprites/Cursors
			_helper.Events.GameLoop.UpdateTicked += InvalidateCursorsOnNextTick;

			// Init game statistics
			_shotsSuccessful = 0;
			_shotsFired = 0;
			_totalTimer = 0;
			Reset();
		}

		private void InvalidateCursorsOnNextTick(object sender, UpdateTickedEventArgs e)
		{
			_helper.Events.GameLoop.UpdateTicked -= InvalidateCursorsOnNextTick;
			_helper.Content.InvalidateCache("LooseSprites/Cursors");
		}

		public void unload()
		{
			_health = GameHealthMax;
			_lives = GameLivesDefault;
		}

		public void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (_onStartMenu)
				_cutscenePhase++; // Progress through the start menu cutscene
			if (_cutsceneTimer <= 0 
			    && _playerPowerTimer <= 0 && _playerSpecialTimer <= 0 
			    && _respawnTimer <= 0 && _gameEndTimer <= 0 && _gameRestartTimer <= 0 
			    && _playerFireTimer <= 1)
				PlayerFire(); // Fire lightgun trigger
		}

		public void leftClickHeld(int x, int y)
		{
			receiveLeftClick(x, y);
		}

		public void receiveRightClick(int x, int y, bool playSound = true)
		{
		}

		public void releaseLeftClick(int x, int y)
		{
		}

		public void releaseRightClick(int x, int y)
		{
		}

		public void receiveKeyPress(Keys k)
		{	
			var flag = false;
			if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k)
			    && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, Keys.Left))
			{
				// Move left
				AddPlayerMovementDirection(Move.Left);
				flag = true;
			}
			if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k)
			    && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
			{
				// Move right
				AddPlayerMovementDirection(Move.Right);
				flag = true;
			}
			if (flag)
				return;
			
			if (_isDebugMode && ModEntry.Instance.Config.DebugArcadeCheats) {
				switch (k)
				{
					case Keys.D1:
						Log.D(_health < GameHealthMax
							? $"_health : {_health} -> {++_health}"		// Modifies health value
							: $"_health : {_health} == GameHealthMax");
						break;
					case Keys.D2:
						Log.D(_energy < GameEnergyMax
							? $"_energy : {_energy} -> {++_energy}"		// Modifies energy value
							: $"_energy : {_energy} == GameEnergyMax");
						break;
					case Keys.D3:
						Log.D($"_lives : {_lives} -> {_lives + 1}");
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
					if (_playerSpecialTimer <= 0 && _energy >= GameEnergyThresholdLow)
					{
						_mirrorPlayerSprite = SpriteEffects.None;
						PlayerSpecialStart();
					}
					break;
				case Keys.Escape:
					// End minigame
					QuitMinigame();
					break;
				case Keys.A:
					// Move left
					AddPlayerMovementDirection(Move.Left);
					_playerAnimationTimer = 0;
					break;
				case Keys.D:
					// Move right
					AddPlayerMovementDirection(Move.Right);
					_playerAnimationTimer = 0;
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
					if (_playerMovementDirections.Contains(Move.Right))
					{
						_playerMovementDirections.Remove(Move.Right);
					}
					flag = true;
				}
				if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k)
				    && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
				{
					if (_playerMovementDirections.Contains(Move.Left))
					{
						_playerMovementDirections.Remove(Move.Left);
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
				if (!_playerMovementDirections.Contains(Move.Left))
					return;
				_playerMovementDirections.Remove(Move.Left);
			}
			else if (k == Keys.D)
			{
				// Move right
				if (!_playerMovementDirections.Contains(Move.Right))
					return;
				_playerMovementDirections.Remove(Move.Right);
			}
		}

		public bool overrideFreeMouseMovement()
		{
			return true;
		}

		public void receiveEventPoke(int data)
		{
		}

		public string minigameId()
		{
			return ModConsts.ArcadeMinigameId;
		}

		public bool doMainGameUpdates()
		{
			return false;
		}

		public void changeScreenSize()
		{
			// TODO : include GameX and GameY into position calculations

			_gameStartCoords = new Vector2(
				(float)(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width - GameW * SpriteScale) / 2,
				(float)(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height - GameH * SpriteScale) / 2);
			_gameEndCoords = new Vector2(
				_gameStartCoords.X + GameX * SpriteScale + GameW * SpriteScale,
				_gameStartCoords.Y + GameY * SpriteScale + GameH * SpriteScale);
			_gameWidth = (int)(_gameEndCoords.X - _gameStartCoords.X);
			_gameHeight = (int)(_gameEndCoords.Y - _gameStartCoords.Y);

			Log.D("mGameStartCoords: "
				+ $"({Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width} - {GameW * SpriteScale}) / 2,\n"
				+ $"({Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height} - {GameH * SpriteScale}) / 2,\n"
				+ $"= {_gameStartCoords.X}, {_gameStartCoords.Y}",
				_isDebugMode);

			Log.D($"mGameEndCoords: {_gameStartCoords.X}, {_gameStartCoords.Y}",
				_isDebugMode);
		}

		private void QuitMinigame()
		{
			if (Game1.currentMinigame == null && Game1.IsMusicContextActive(Game1.MusicContext.MiniGame))
				Game1.stopMusicTrack(Game1.MusicContext.MiniGame);
			if (Game1.currentLocation != null
			    && Game1.currentLocation.Name.Equals((object)"Saloon") && Game1.timeOfDay >= 1700)
				Game1.changeMusicTrack("Saloon1");
			unload();
			Game1.currentMinigame = null;
			_helper.Content.InvalidateCache("LooseSprites/Cursors");
		}

		public bool forceQuit()
		{
			return false;
		}

		private void Reset()
		{
			_enemyBullets.Clear();
			_enemies.Clear();
			_temporaryAnimatedSprites.Clear();

			PlayerPosition = new Vector2(
					_gameStartCoords.X + GameX + GameW * SpriteScale / 2,
					_gameStartCoords.Y + GameY + GameH * SpriteScale - PlayerFullH * SpriteScale
			);

			// Player bounding box only includes the bottom 2/3rds of the sprite
			PlayerBoundingBox.X = (int)PlayerPosition.X;
			PlayerBoundingBox.Y = (int)PlayerPosition.Y;
			PlayerBoundingBox.Width = PlayerW * SpriteScale;
			PlayerBoundingBox.Height = PlayerSplitWH * SpriteScale;

			_playerAnimationPhase = 0;
			_playerAnimationTimer = 0;
			_playerSpecialTimer = 0;
			_playerFireTimer = 0;

			_cutsceneTimer = 0;
			_cutscenePhase = 0;
			_cutsceneBackgroundPosition = 0f;

			// todo: reduce score with formula
			var reducedScore = 0;
			_score = Math.Min(0, reducedScore);

			_health = GameHealthMax;
			_energy = 0;
			_activeSpecialPower = SpecialPower.None;

			_whichStage = 0;
			_whichWorld = 0;

			_respawnTimer = 0.0f;
			_onGameOver = false;

			if (_isDebugMode && !ModEntry.Instance.Config.DebugArcadeSkipIntro)
				_onStartMenu = true;
		}

		private void AddPlayerMovementDirection(Move direction)
		{
			if (_playerMovementDirections.Contains(direction))
				return;
			_playerMovementDirections.Add(direction);
		}

		private void PlayerPowerup(int which)
		{
			switch (which)
			{
				case PowerupHealth:
					_health = Math.Min(GameHealthMax, _health + PowerupHealthAmount);

					// todo spawn a new temporary sprite at the powerup object coordinates

					break;
			}
		}

		private void PlayerSpecialStart()
		{
			_playerAnimationPhase = 0;
			_playerSpecialTimer = 1;
		}
		
		private void PlayerSpecialEnd()
		{
			// Energy levels between 0 and the low-threshold will use a light special power
			_activeSpecialPower = _energy >= GameEnergyMax 
				? SpecialPower.Normal 
				: SpecialPower.Megaton;

			_playerAnimationPhase = 0;
			_playerSpecialTimer = 0;
		}

		private void PlayerPowerEnd()
		{
			_activeSpecialPower = SpecialPower.None;
			_playerAnimationPhase = 0;
			_playerPowerTimer = 0;
			_energy = 0;
		}

		private void PlayerFire()
		{
			// Position the source around the centre of the player
			var src = new Vector2(
				PlayerPosition.X + (PlayerW / 2 * SpriteScale),
				PlayerPosition.Y + (PlayerSplitWH / 2 * SpriteScale));

			// Position the target on the centre of the cursor
			var dest = new Vector2(
				_helper.Input.GetCursorPosition().ScreenPixels.X + TD / 2 * CursorScale,
				_helper.Input.GetCursorPosition().ScreenPixels.Y + TD / 2 * CursorScale);
			SpawnBullet(src, dest, GamePlayerDamage, ProjectileType.Player);
			_playerFireTimer = GameFireDelay;

			// Mirror player sprite to face target
			if (_playerMovementDirections.Count == 0) 
				_mirrorPlayerSprite = dest.X < PlayerPosition.X + PlayerBoundingBox.Width / 2
					? SpriteEffects.FlipHorizontally 
					: SpriteEffects.None;

			Game1.playSound("Cowboy_gunshot");

			++_shotsFired;
		}

		private bool PlayerTakeDamage(int damage)
		{
			// todo: animate player

			_health = Math.Max(0, _health - damage);
			if (_health <= 0)
				return true;
			
			_screenFlashColor = new Color(new Vector4(255, 0, 0, 0.25f));
			_screenFlashTimer = 200;

			return false;
		}

		private void PlayerBeforeDeath()
		{
			// todo: fill this function with pre-death animation timer etc

			Game1.playSound("cowboy_dead");

			PlayerDie();
		}

		private void PlayerDie()
		{
			// todo: understand wtf this function does

			--_lives;
			_respawnTimer = GameDeathDelay;
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
					_gameStartCoords, 
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

			if (_lives >= 0)
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
					_gameStartCoords,
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
				endFunction = PlayerGameOverCheck
			});

			_respawnTimer *= 3f;
		}

		private void PlayerGameOverCheck(int extra)
		{
			if (_lives >= 0)
			{
				PlayerRespawn();
				return;
			}
			_onGameOver = true;
			_enemies.Clear();
			QuitMinigame();
			++Game1.currentLocation.currentEvent.CurrentCommand;
		}

		private void PlayerRespawn()
		{
			_playerInvincibleTimer = GameInvincibleDelay;
			_health = GameHealthMax;
		}

		private void EndCurrentStage()
		{
			_playerMovementDirections.Clear();

			// todo: set cutscenes, begin events, etc
		}

		private void EndCurrentWorld()
		{
			// todo: set cutscenes, begin events, etc
		}

		// Begin the next stage within a world
		private void StartNewStage()
		{
			++_whichStage;
			_stageMap = GetMap(_whichStage);
		}

		// Begin the next world
		private void StartNewWorld()
		{
			++_whichWorld;
		}

		// New Game Plus
		private void StartNewGame()
		{
			_gameRestartTimer = 2000;
			++_whichGame;
		}

		public int[,] GetMap(int wave)
		{
			var map = new int[MapH, MapW];
			for (var i = 0; i < MapH; ++i)
			{
				for (var j = 0; j < MapW; ++j)
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
					for (var i = 0; i < MapH; ++i)
					{
						for (var j = 0; j < MapW; ++j)
						{
							if (map[i, j] == 0 || map[i, j] == 1
							                   || (map[i, j] == 2 || map[i, j] == 5))
								map[i, j] = 3;
						}
					}
					break;
				case 0:
					for (var i = 0; i < MapH; ++i)
					{
						for (var j = 0; j < MapW; ++j)
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

		/// <summary>
		/// Creates a new bullet object at a point on-screen for either the player or some monster.
		/// </summary>
		/// <param name="where">Spawn position.</param>
		/// <param name="dest">Target for vector of motion from spawn position.</param>
		/// <param name="damage"></param>
		/// <param name="which">Projectile behaviour, also determines whether player or not.</param>
		private void SpawnBullet(Vector2 where, Vector2 dest, int damage, ProjectileType which)
		{
			// Rotation to aim towards target
			var radiansBetween = Vector.RadiansBetween(dest, where);
			radiansBetween -= (float)(Math.PI / 2.0d);
			//Log.D($"RadiansBetween {where}, {dest} = {radiansBetween:0.00}rad", _isDebugMode);

			// Vector of motion
			var motion = Vector.PointAt(where, dest);
			motion.Normalize();
			LastPlayerAimVector = motion;
			Log.D($"Normalised motion = {motion.X:0.00}x, {motion.Y:0.00}y, mirror={_mirrorPlayerSprite}",
				_isDebugMode);

			// Spawn position
			var position = where + motion * (GameProjSpeed[(int)which] * 5);

			// Add the bullet to the active lists for the respective spawner
			if (which == ProjectileType.Player)
				PlayerBullets.Add(new Bullet(position, motion, radiansBetween, which, dest));
			else
				_enemyBullets.Add(new Bullet(position, motion, radiansBetween, which, dest));
		}

		#region Per-tick updates

		/// <summary>
		/// Update on-screen bullets, removing as they travel offscreen.
		/// </summary>
		/// <param name="time"></param>
		private void UpdateBullets(GameTime time)
		{
			// Handle player bullets
			for (var i = PlayerBullets.Count - 1; i >= 0; --i)
			{
				// Damage monsters
				for (var j = _enemies.Count - 1; j >= 0; --j)
				{
					if (_enemies[j].Position.Intersects(
						new Rectangle(
							(int)PlayerBullets[i].Position.X, 
							(int)PlayerBullets[i].Position.Y,
							TD * SpriteScale, 
							TD * SpriteScale)))
					{
						if (!_enemies[j].TakeDamage(GamePlayerDamage))
						{
							_enemies[j].Die();
							_enemies.RemoveAt(j);
							PlayerBullets.RemoveAt(i);
						}
					}
					break;
				}

				// Update bullet positions
				PlayerBullets[i].Position += PlayerBullets[i].Motion 
				                             * GameProjSpeed[(int)PlayerBullets[i].Type];

				// Remove offscreen bullets
				if (PlayerBullets[i].Position.X <= _gameStartCoords.X 
				    || PlayerBullets[i].Position.X >= _gameEndCoords.X 
				    || PlayerBullets[i].Position.Y <= _gameStartCoords.Y 
				    || PlayerBullets[i].Position.Y >= _gameEndCoords.Y)
				{
					PlayerBullets.RemoveAt(i);
				}
			}

			// Handle enemy bullets
			for (var i = _enemyBullets.Count - 1; i >= 0; --i)
			{
				// Damage the player
				if (_playerInvincibleTimer <= 0 && _respawnTimer <= 0.0)
				{
					if (_playerInvincibleTimer <= 0 && _respawnTimer <= 0.0
					&& PlayerBoundingBox.Intersects(
						new Rectangle(
							(int)_enemyBullets[i].Position.X,
							(int)_enemyBullets[i].Position.Y, 
							TD * SpriteScale,
							TD * SpriteScale))) {
						if (!PlayerTakeDamage(GameEnemyDamage))
						{
							PlayerBeforeDeath();
							_enemyBullets.RemoveAt(i);
						}
					}
					break;
				}

				// Update bullet positions
				_enemyBullets[i].RotationCur += GameProjRotDelta[i];
				_enemyBullets[i].Position += _enemyBullets[i].Motion 
				                             * GameProjSpeed[(int)_enemyBullets[i].Type];

				// Remove offscreen bullets
				if (_enemyBullets[i].Position.X <= _gameStartCoords.X 
				    || _enemyBullets[i].Position.X >= _gameEndCoords.X 
				    || _enemyBullets[i].Position.Y <= _gameStartCoords.Y
				    || _enemyBullets[i].Position.Y >= _gameEndCoords.Y)
				{
					_enemyBullets.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Update on-screen powerups, removing as their timer runs down.
		/// </summary>
		/// <param name="time"></param>
		private void UpdatePowerups(GameTime time)
		{
			for (var i = _powerups.Count - 1; i > 0; --i)
			{
				if (_powerups[i].Duration <= 0)
					_powerups.RemoveAt(i);
			}
		}

		private void UpdateMenus(GameTime time, TimeSpan elapsedGameTime)
		{
			// Sit on the start game screen until clicked away
			if (_onGameOver || _onStartMenu)
			{
				// Progress through a small intro sequence
				if (_cutsceneBackgroundPosition >= GameW * 3)
					_cutsceneTimer += elapsedGameTime.Milliseconds;
				switch (_cutscenePhase)
				{
					case 0:
					{
						if (_cutsceneBackgroundPosition < GameW * 3)
							_cutsceneBackgroundPosition += GameW / UiAnimTimescale; // Move the lightshaft texture across the screen
						if (_cutsceneTimer >= TimerTitlePhase0)
						{
							++_cutscenePhase; // Start showing all the title screen elements after it's held on blank for a bit
							Game1.playSound("wand");
							Log.D($"phase={_cutscenePhase} | timer={_cutsceneTimer}");
						}
						break;
					}
					case 1:
					{
						if (_cutsceneTimer >= TimerTitlePhase1)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					}
					case 2:
					{
						if (_cutsceneTimer >= TimerTitlePhase3)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					}
					case 3:
					{
						if (_cutsceneTimer >= TimerTitlePhase4)
						{
							++_cutscenePhase;
							Game1.playSound("drumkit6");
						}
						break;
					}
					case 4:
					{
						if (_cutsceneTimer < TimerTitlePhase5)
						{
							_cutsceneTimer = TimerTitlePhase5;
						}
						break;
					}
					case 5:
						// End the cutscene and begin the game
						// after the user clicks past the end of intro cutscene (phase 3)
						_cutsceneTimer = 0;
						_cutscenePhase = 0;
						_onStartMenu = false;
						Game1.playSound("cowboy_gunload");
						Log.D($"phase={_cutscenePhase} | timer={_cutsceneTimer}");
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

			// Exit the game
			if (_hasPlayerQuit)
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
					Game1.currentMinigame = new LightGunGame();
				}
			}

			// Run through screen effects
			if (_screenFlashTimer > 0)
			{
				_screenFlashTimer -= elapsedGameTime.Milliseconds;
			}

			// Run down player invincibility
			if (_playerInvincibleTimer > 0)
			{
				_playerInvincibleTimer -= elapsedGameTime.Milliseconds;
			}

			// Run down player lightgun animation
			if (_playerFireTimer > 0)
			{
				--_playerFireTimer;
			}
			else
			{
				LastPlayerAimVector = Vector2.Zero;
			}

			// Handle player special trigger
			if (_playerSpecialTimer > 0)
			{
				_playerSpecialTimer += elapsedGameTime.Milliseconds;

				// Progress through player special trigger animation

				// Events 
				if (_playerAnimationPhase == PlayerSpecialFrames)
				{
					// Transfer into the active special power
					PlayerSpecialEnd();
				}

				// Timers
				if (_playerAnimationPhase == 0)
				{
					if (_playerSpecialTimer >= TimerSpecialPhase1)
						++_playerAnimationPhase;
				}
				else if (_playerAnimationPhase == 1)
				{
					if (_playerSpecialTimer >= TimerSpecialPhase2)
						++_playerAnimationPhase;
				}
			}
			// Handle player power animations and effects
			else if (_activeSpecialPower != SpecialPower.None)
			{
				_playerPowerTimer += elapsedGameTime.Milliseconds;

				// Progress through player power

				// Events
				if (_playerAnimationPhase == PlayerPowerFrames)
				{
					// Return to usual game flow
					PlayerPowerEnd();
				}
				
				// Timers
				if (_playerAnimationPhase == 0)
				{
					// Frequently spawn bullets raining down on the field
					if (_playerPowerTimer % 5 == 0)
					{
						
					}
					// Progress to the next phase
					if (_playerPowerTimer >= TimerPowerPhase1)
						++_playerAnimationPhase;
				}
				else if (_playerAnimationPhase == 1)
				{
					// Progress to the next phase
					if (_playerPowerTimer >= TimerPowerPhase2)
						++_playerAnimationPhase;
				}
				else if (_playerAnimationPhase == 2)
				{
					// End the power
					if (_playerPowerTimer >= TimerPowerPhase3)
						++_playerAnimationPhase;
				}
			}

			// Handle other game sprites
			for (var i = _temporaryAnimatedSprites.Count - 1; i >= 0; --i)
			{
				if (_temporaryAnimatedSprites[i].update(time))
					_temporaryAnimatedSprites.RemoveAt(i);
			}
			

			/* Game Activity */


			// While the game offers player agency
			if (!_onStartMenu && _cutsceneTimer <= 0)
			{
				// Update onscreen powerups
				UpdatePowerups(time);

				// Update bullet data
				UpdateBullets(time);

				if (_playerSpecialTimer <= 0 && _playerPowerTimer <= 0)
				{
					// While the game is engaging the player
					if (_cutsceneTimer <= 0)
					{
						// Run down the death timer
						if (_respawnTimer > 0.0)
						{
							_respawnTimer -= elapsedGameTime.Milliseconds;
						}

						// Handle player movement
						if (_playerMovementDirections.Count > 0)
						{
							switch (_playerMovementDirections.ElementAt(0))
							{
								case Move.Right:
									_mirrorPlayerSprite = SpriteEffects.None;
									if (PlayerPosition.X + PlayerBoundingBox.Width < _gameEndCoords.X)
										PlayerPosition.X += GameMoveSpeed;
									else
										PlayerPosition.X = _gameEndCoords.X - PlayerBoundingBox.Width;
									break;
								case Move.Left:
									_mirrorPlayerSprite = SpriteEffects.FlipHorizontally;
									if (PlayerPosition.X > _gameStartCoords.X)
										PlayerPosition.X -= GameMoveSpeed;
									else
										PlayerPosition.X = _gameStartCoords.X;
									break;
							}
						}

						_playerAnimationTimer += elapsedGameTime.Milliseconds;
						_playerAnimationTimer %= (PlayerRunFrames) * PlayerAnimTimescale;

						PlayerBoundingBox.X = (int)PlayerPosition.X;
					}

					// Handle enemy behaviours
					if (_health > 0)
					{

						// todo

					}
				}
			}

			UpdateMenus(time, elapsedGameTime);

			return false;
		}

		private void DrawPlayer(SpriteBatch b)
		{
			// Flicker sprite visibility while invincible
			if (_playerInvincibleTimer > 0 || _playerInvincibleTimer / 100 % 2 != 0) return;

			var destRects = new Rectangle[3];
			var srcRects = new Rectangle[3];

			// Draw full body action sprites
			if (_playerSpecialTimer > 0)
			{   // Activated special power
				destRects[0] = new Rectangle(
					(int)PlayerPosition.X,
					(int)PlayerPosition.Y,
					PlayerBoundingBox.Width,
					PlayerFullH * SpriteScale);
				srcRects[0] = new Rectangle(
					PlayerSpecialX + PlayerW * _playerAnimationPhase,
					PlayerFullY,
					PlayerW,
					PlayerFullH);
			}
			else if (_activeSpecialPower != SpecialPower.None)
			{   // Player used a special power
				// Draw power effects by type
				if (_activeSpecialPower == SpecialPower.Normal)
				{   // Player used Venus Love Shower / THRESHOLD_LOW === POWER_NORMAL
					// . . . .
				}
				// Draw full body sprite by phase
				destRects[0] = new Rectangle(
					(int)PlayerPosition.X,
					(int)PlayerPosition.Y,
					PlayerBoundingBox.Width,
					PlayerFullH * SpriteScale);
				srcRects[0] = new Rectangle(
					PlayerPowerX
					+ PlayerW * PowerAnims[(int)_activeSpecialPower]
					+ PlayerW * _playerAnimationPhase,
					PlayerFullY,
					PlayerW,
					PlayerFullH);
			}
			else if (_respawnTimer > 0)
			{   // Player dying
				// . .. . .
			}
			// Draw full body idle sprite
			else if (_playerFireTimer <= 0 && _playerMovementDirections.Count == 0)
			{   // Standing idle
				destRects[0] = new Rectangle(
					(int)PlayerPosition.X,
					(int)PlayerPosition.Y,
					PlayerBoundingBox.Width,
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
				if (_playerFireTimer > 0)
				{
					var whichFrame = 0; // authors note: it was quicker to swap the level and below sprites 
					// than to fix my stupid broken logic
					if (LastPlayerAimVector != Vector2.Zero)
					{
						// Firing arms
						if ((int)Math.Ceiling(LastPlayerAimVector.X) == (int)_mirrorPlayerSprite
						|| _playerMovementDirections.Count > 0 && Math.Abs(LastPlayerAimVector.Y) >= 0.9f)
							whichFrame = 4; // Aiming backwards, also aiming upwards while running
						else if (Math.Abs(LastPlayerAimVector.Y) >= 0.9f) // Aiming upwards while standing
							whichFrame = 5; // invalid index, therefore no visible arms
						else if (LastPlayerAimVector.Y < -0.6f) // Aiming low
							whichFrame = 3;
						else if (LastPlayerAimVector.Y < -0.2f) // Aiming level
							whichFrame = 2;
						else if (LastPlayerAimVector.Y > 0.2f) // Aiming below
							whichFrame = 1;
						destRects[2] = new Rectangle(
							(int) PlayerPosition.X,
							(int) PlayerPosition.Y,
							PlayerBoundingBox.Width,
							PlayerBoundingBox.Height);
						srcRects[2] = new Rectangle(
							PlayerArmsX + PlayerW * whichFrame,
							PlayerArmsY,
							PlayerW,
							PlayerSplitWH);
					}
					if (_playerMovementDirections.Count > 0)
					{   // Firing running torso
						whichFrame = (int)(_playerAnimationTimer / PlayerAnimTimescale);
						destRects[0] = new Rectangle(
							(int)PlayerPosition.X,
							(int)PlayerPosition.Y,
							PlayerBoundingBox.Width,
							PlayerBoundingBox.Height);
						srcRects[0] = new Rectangle(
							PlayerBodyRunFireX + PlayerW * whichFrame,
							PlayerBodyY,
							PlayerW,
							PlayerSplitWH);
					}
					else
					{	// Firing standing torso
						whichFrame = PlayerBodySideFireX; // Aiming sideways
						if (Math.Abs(LastPlayerAimVector.Y) >= 0.9f
						    || (int)Math.Ceiling(LastPlayerAimVector.X) == (int)_mirrorPlayerSprite)
							whichFrame = PlayerBodyUpFireX; // Aiming upwards
						destRects[0] = new Rectangle(
							(int)PlayerPosition.X,
							(int)PlayerPosition.Y,
							PlayerBoundingBox.Width,
							PlayerBoundingBox.Height);
						srcRects[0] = new Rectangle(
							whichFrame,
							PlayerBodyY,
							PlayerW,
							PlayerSplitWH);
					}
				}
				else
				{   // Running torso
					var whichFrame = (int)(_playerAnimationTimer / PlayerAnimTimescale);
					destRects[0] = new Rectangle(
						(int)PlayerPosition.X,
						(int)PlayerPosition.Y,
						PlayerBoundingBox.Width,
						PlayerBoundingBox.Height);
					srcRects[0] = new Rectangle(
						PlayerBodyRunX + PlayerW * whichFrame,
						PlayerBodyY,
						PlayerW,
						PlayerSplitWH);
				}
			}

			// Draw appropriate sprite legs
			if (_playerMovementDirections.Count > 0)
			{   // Running
				var whichFrame = (int)(_playerAnimationTimer / PlayerAnimTimescale);
				destRects[1] = new Rectangle(
					(int)PlayerPosition.X,
					(int)PlayerPosition.Y + (PlayerFullH - PlayerSplitWH) * SpriteScale,
					PlayerBoundingBox.Width,
					PlayerBoundingBox.Height);
				srcRects[1] = new Rectangle(
					PlayerLegsRunX + (PlayerW * whichFrame),
					PlayerLegsY,
					PlayerW,
					PlayerSplitWH);
			}
			else if (_playerFireTimer > 0)
			{   // Standing and firing
				if (LastPlayerAimVector != Vector2.Zero)
				{   // Firing legs
					var whichFrame = 0; // Aiming sideways
					if (Math.Abs(LastPlayerAimVector.Y) > 0.9f)
						whichFrame = 1; // Aiming upwards
					destRects[1] = new Rectangle(
						(int)PlayerPosition.X,
						(int)PlayerPosition.Y + (PlayerFullH - PlayerSplitWH) * SpriteScale,
						PlayerBoundingBox.Width,
						PlayerBoundingBox.Height);
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
						_mirrorPlayerSprite,
						PlayerPosition.Y / 10000f + i / 1000f + 1f / 1000f);
				}
			}
		}

		/// <summary>
		/// DEBUG: draw line from start to end of bullet target trail
		/// </summary>
		private void DrawTracer(SpriteBatch b)
		{
			if (!ModEntry.Instance.Config.DebugMode || !PlayerBullets.Any()) return;

			// create 1x1 white texture for line drawing
			var t = new Texture2D(Game1.graphics.GraphicsDevice, 2, 2);
			t.SetData(new[] { Color.White, Color.White, Color.White, Color.White });

			var startpoint = PlayerBullets[PlayerBullets.Count - 1].Start;
			var endpoint = PlayerBullets[PlayerBullets.Count - 1].Target;
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

		private void DrawBackground(SpriteBatch b)
		{
			switch (_activeSpecialPower)
			{
				case SpecialPower.Normal:
				case SpecialPower.Megaton:
					if (_playerAnimationPhase == 1)
					{
						// Draw a black overlay
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								(int)_gameStartCoords.X,
								(int)_gameStartCoords.Y,
								_gameWidth,
								_gameHeight),
							new Rectangle(
								TitleMaskX,
								TitleMaskY,
								TD,
								TD),
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
						_arcadeTexture,
						new Rectangle(
							(int)_gameStartCoords.X,
							(int)_gameStartCoords.Y,
							_gameWidth,
							_gameHeight),
						new Rectangle(
							TitleMaskX,
							TitleMaskY,
							TD,
							TD),
						Color.White,
						0.0f,
						Vector2.Zero,
						SpriteEffects.None,
						0f);
					break;
			}
		}

		private void DrawHud(SpriteBatch b)
		{
			// todo: Draw health bar to destrect width * health / healthmax
			//			or Draw health bar as icons onscreen (much nicer)
			// todo: Draw ammo container
			// todo: drawstring stage time countdown

			// todo: Draw health bar objects
			// todo: Draw energy bar objects
			// todo: Draw life and counter
		}

		private void DrawMenus(SpriteBatch b)
		{
			// Display Start menu
			if (_onStartMenu)
			{
				// Render each phase of the intro
				/*
				Log.D($"draw = "
				      + $"{(int)_gameStartCoords.X - _gameWidth * 2 + (int)_cutsceneBackgroundPosition}"
				      + $"\nwidth = {_gameWidth * 3} | cur = {_cutsceneBackgroundPosition} "
				      + $"| end = {GameW * 3} | phase={_cutscenePhase} | timer={_cutsceneTimer}");
				*/
				// Blackout backdrop
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)_gameStartCoords.X,
						(int)_gameStartCoords.Y,
						_gameWidth,
						_gameHeight),
					new Rectangle(
						TitleMaskX,
						TitleMaskY + TitleBlackoutH,
						TitleBlackoutW,
						TitleBlackoutH),
					Color.White,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					0f);

				// Draw 'horizontal letterboxing' with blackout to hide the lightshaft mask flyover
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)_gameStartCoords.X - _gameWidth * 3,
						(int)_gameStartCoords.Y,
						_gameWidth * 3,
						_gameHeight),
					new Rectangle(
						TitleBlackoutX,
						TitleBlackoutY,
						TitleBlackoutW,
						TitleBlackoutH),
					Color.Black,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f);
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						(int)_gameEndCoords.X,
						(int)_gameStartCoords.Y,
						_gameWidth * 3,
						_gameHeight),
					new Rectangle(
						TitleBlackoutX,
						TitleBlackoutY,
						TitleBlackoutW,
						TitleBlackoutH),
					Color.Black,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f);

				if (_cutscenePhase == 0)
				{
					// Draw the white title banner silhouetted on black

					// White V/
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							(int)_gameStartCoords.X + _gameWidth / 2 + TD * 2,
							(int)_gameStartCoords.Y + _gameHeight / 2 - TD * 4,
							TitleRedVW * SpriteScale,
							TitleRedVH * SpriteScale),
						new Rectangle(
							TitleRedVX + TitleRedVW,
							TitleRedVY,
							TitleRedVW,
							TitleRedVH),
						Color.White,
						0.0f,
						new Vector2(0, TitleRedVH / 2),
						SpriteEffects.None,
						0.5f);

					// Pan a vertical letterbox across the screen to simulate light gleam

					// Black frame
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							(int)_gameStartCoords.X - _gameWidth * 2 + (int)_cutsceneBackgroundPosition,
							(int)_gameStartCoords.Y,
							_gameWidth * 3,
							_gameHeight),
						new Rectangle(
							TitleMaskX,
							TitleMaskY,
							TitleMaskW,
							TitleMaskH),
						Color.White,
						0.0f,
						Vector2.Zero,
						SpriteEffects.None,
						1f - 1f / 10000f);
				}

				if (_cutscenePhase >= 1)
				{
					// Draw the coloured title banner on black

					// Red V/
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							(int)_gameStartCoords.X + _gameWidth / 2 + TD * 2,
							(int)_gameStartCoords.Y + _gameHeight / 2 - TD * 4,
							TitleRedVW * SpriteScale,
							TitleRedVH * SpriteScale),
						new Rectangle(
							TitleRedVX,
							TitleRedVY,
							TitleRedVW,
							TitleRedVH),
						Color.White,
						0.0f,
						new Vector2(0, TitleRedVH / 2),
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
							(int)_gameStartCoords.X + _gameWidth / 2 - TD / 2 * 6 - TitleCodenameW,
							(int)_gameStartCoords.Y + _gameHeight / 2 - TD * 6 - TitleCodenameH,
							TitleCodenameW * SpriteScale,
							TitleCodenameH * SpriteScale),
						new Rectangle(
							TitleCodenameX,
							TitleCodenameY,
							TitleCodenameW,
							TitleCodenameH),
						Color.White,
						0.0f,
						new Vector2(0, TitleCodenameH / 2),
						SpriteEffects.None,
						1.0f);
				}

				if (_cutscenePhase >= 3)
				{
					// セーラー
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							(int)_gameStartCoords.X + _gameWidth / 2 - TD * 3 - TitleSailorW,
							(int)_gameStartCoords.Y + _gameHeight / 2 - TD * 3,
							TitleSailorW * SpriteScale,
							TitleSailorH * SpriteScale),
						new Rectangle(
							TitleSailorX,
							TitleSailorY,
							TitleSailorW,
							TitleSailorH),
						Color.White,
						0.0f,
						new Vector2(0, TitleSailorH / 2),
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
							(int)_gameStartCoords.X + _gameWidth / 2,
							(int)_gameStartCoords.Y + _gameHeight - TD * 5 - TitleSignatureH,
							TitleSignatureW * SpriteScale,
							TitleSignatureH * SpriteScale),
						new Rectangle(
							TitleSignatureX,
							TitleSignatureY,
							TitleSignatureW,
							TitleSignatureH),
						Color.White,
						0.0f,
						new Vector2(TitleSignatureW / 2, TitleSignatureH / 2),
						SpriteEffects.None,
						1f);

					if (_cutsceneTimer >= TimerTitlePhase5 && (_cutsceneTimer / 500) % 2 == 0)
					{
						// "Fire to start"
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								(int)_gameStartCoords.X + _gameWidth / 2,
								(int)_gameStartCoords.Y + _gameHeight / 20 * 14,
								TitleStartW * SpriteScale,
								TitleStartH * SpriteScale),
							new Rectangle(
								0,
								TitleStartY,
								TitleStartW,
								TitleStartH),
							Color.White,
							0.0f,
							new Vector2(TitleStartW / 2, TitleStartH / 2),
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
						(int)_gameStartCoords.X,
						(int)_gameStartCoords.Y,
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
					_gameStartCoords + new Vector2(6f, 7f) * TD,
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					_gameStartCoords + new Vector2(6f, 7f) * TD + new Vector2(-1f, 0.0f),
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					_gameStartCoords + new Vector2(6f, 7f) * TD + new Vector2(1f, 0.0f),
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
						_gameStartCoords + new Vector2(6f, 9f) * TD,
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
					_gameStartCoords + new Vector2(6f, 9f) * TD + new Vector2(0.0f, (2 / 3)),
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
						(int)_gameStartCoords.X,
						(int)_gameStartCoords.Y,
						_gameWidth,
						_gameHeight),
					Game1.staminaRect.Bounds,
					_screenFlashColor,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f);
			}

			// Draw the active game
			if (!_onStartMenu && _respawnTimer <= 0.0)
			{
				// debug makito
				// very important
				b.Draw(
						_arcadeTexture, 
						new Rectangle(
							_gameWidth,
							_gameHeight,
							TD * SpriteScale,
							TD * SpriteScale),
						new Rectangle(0, 0, TD, TD),
						Color.White,
						0.0f,
						new Vector2(TD / 2, TD / 2),
						_mirrorPlayerSprite,
						1f);
				// very important
				// debug makito

				// Draw game elements
				DrawBackground(b);
				DrawPlayer(b);
				DrawHud(b);
				DrawTracer(b); // DEBUG

				// Draw game objects
				foreach (var bullet in PlayerBullets)
					bullet.Draw(b);
				foreach (var bullet in _enemyBullets)
					bullet.Draw(b);
				foreach (var temporarySprite in _temporaryAnimatedSprites)
					temporarySprite.draw(b, true);
				foreach (var monster in _enemies)
					monster.Draw(b);
			}

			// Draw menus and cutscenes
			DrawMenus(b);

			b.End();
		}

		#endregion
		
		#region Objects

		public class Powerup
		{
			public readonly LootDrops Which;
			public readonly Point Where;
			public readonly Rectangle TextureRect;
			public float YOffset;
			public int Duration;

			public Powerup(LootDrops which, Point where)
			{
				Which = which;
				Where = where;
				Duration = (int)(LootDurations)which * 1000;

				// Pick a texture for the powerup drop
				switch (which)
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

			public void Draw(SpriteBatch b)
			{
				if (Duration <= 2000 && Duration / 200 % 2 != 0)
					return;

				b.Draw(
					_arcadeTexture,
					_gameStartCoords + new Vector2(Where.X, Where.Y + YOffset),
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
					(float)(Where.Y / 10000.0 + 1.0 / 1000.0));
			}
		}
		
		public class Bullet
		{
			public Vector2 Position;        // Current position on screen
			public Vector2 Motion;          // Line of motion between spawn and dest
			public Vector2 MotionCur;       // Current position along vector of motion between spawn and dest
			public float Rotation;          // Angle of vector between spawn and dest
			public float RotationCur;       // Angle of vector between spawn and dest
			public ProjectileType Type;		// Which projectile, ie. light/heavy/bullet/lightgun

			public Vector2 Start;
			public Vector2 Target; // debug - target to draw line towards

			public Bullet(Vector2 position, Vector2 motion, float rotation, ProjectileType type, Vector2 target)
			{
				Position = position;
				Motion = motion;
				Rotation = rotation;
				Type = type;
				
				Motion = motion;

				MotionCur = motion;
				RotationCur = type == ProjectileType.Player ? 0f : rotation;

				Start = position;
				Target = target;
			}

			public void Draw(SpriteBatch b)
			{
				b.Draw(
					_arcadeTexture,
					new Vector2(
						Position.X,
						Position.Y),
					new Rectangle(
						ProjectilesX + TD * (int)Type,
						ProjectilesY,
						TD,
						TD),
					Color.White,
					Rotation,
					new Vector2(TD / 2, TD / 2),
					SpriteScale,
					SpriteEffects.None,
					0.9f);
			}
		}
		
		public class Monster
		{
			private Color _tint = Color.White;
			private Color _flashColor = Color.Red;

			public int Health;
			public int Type;
			public Rectangle Position;
			public float FlashColorTimer;
			public int TicksSinceLastMovement;

			public Monster(int which, int health, int speed, Point position)
			{
				Type = which;
				Health = health;
				Position = new Rectangle(position.X, position.Y, TD, TD);
			}

			public Monster(int which, Point position)
			{
				Type = which;
				Position = new Rectangle(position.X, position.Y, TD, TD);
				switch (Type)
				{
					// todo: enemy spawn parameters
				}
			}

			public virtual void Draw(SpriteBatch b)
			{
				b.Draw(
					_arcadeTexture,
					_gameStartCoords + new Vector2(Position.X, Position.Y),
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

			public virtual bool TakeDamage(int damage)
			{
				Health = Math.Max(0, Health - damage);
				if (Health <= 0)
					return false;
				_flashColor = Color.Red;
				FlashColorTimer = 100f;
				return true;
			}

			public virtual void Die()
			{
				// todo: literally anything at all
				// drop rates if health low, whatever

				GetLootDrop(new Point(
					Position.X + Position.Width / 2,
					Position.Y + Position.Height / 2));
			}

			/// <summary>
			/// Create a new Powerup object at some position.
			/// </summary>
			/// <param name="where">Spawn position.</param>
			public virtual void GetLootDrop(Point where)
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
					_powerups.Add(new Powerup(which, where));
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
		if ((double) Utility.distance((float) this.playerBoundingBox.Center.X, (float) (AbigailGame.powerups[index].position.X + AbigailGame.TD / 2), (float) this.playerBoundingBox.Center.Y, (float) (AbigailGame.powerups[index].position.Y + AbigailGame.TD / 2)) <= (double) (AbigailGame.TD + 3) && (AbigailGame.powerups[index].position.X < AbigailGame.TD || AbigailGame.powerups[index].position.X >= 16 * AbigailGame.TD - AbigailGame.TD || (AbigailGame.powerups[index].position.Y < AbigailGame.TD || AbigailGame.powerups[index].position.Y >= 16 * AbigailGame.TD - AbigailGame.TD)))
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
