using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Netcode;

using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Minigames;

using StardewModdingAPI;

namespace HikawaShrine.LightGunGame
{
	internal class LightGunGame : IMinigame
	{
		/* Constant Values */
		
		// program attributes
		private const float PlayerAnimTimescale = 300f;
		private const float UiAnimTimescale = 120f;
		private const int SpriteScale = 2;
		private const int TileW = 16;
		private const int MoveIdle = 0;
		private const int MoveRight = 1;
		private const int MoveLeft = 2;

		// cutscene timers
		private const int TimerTitlePhase1 = 800;	// red V
		private const int TimerTitlePhase2 = 1200;	// codename
		private const int TimerTitlePhase3 = 1600;	// sailor
		private const int TimerTitlePhase4 = 2000;	// arcade game
		private const int TimerTitlePhase5 = 2400;	// blueberry 1991-1996
		private const int TimerTitlePhase6 = 2800;   // fire to start

		private const int TimerSpecialPhase1 = 1000; // stand ahead
		private const int TimerSpecialPhase2 = 2400; // raise compact

		private const int TimerPowerPhase1 = 1500;	// activate
		private const int TimerPowerPhase2 = 5000;	// white glow
		private const int TimerPowerPhase3 = 6500;	// cooldown

		// asset attributes

		// gameplay content
		private enum SpecialPower {
			Normal,
			Megaton,
			Sulphur,
			Incense,
			None,
		}
		
		private static readonly int[] PowerAnims = {
			0,
			0, 
			1,
			1,
			0
		};

		private enum LightGunAimHeight {
			High,
			Mid,
			Low
		}
		
		// hud graphics
		public const int CrosshairW = TileW * 1;
		public const int CrosshairH = TileW * 1;
		public const int CrosshairX = TileW * 2;
		public const int CrosshairY = TileW * 0;

		private const int MapW = 20;
		private const int MapH = MapW;
		private const int HudW = TileW * 3;
		private const int HudH = TileW;
		private const int GameW = TileW * MapW;
		private const int GameH = TileW * MapH;
		private const int GameX = 0;
		private const int GameY = 0;

		private const int StripeBannerW = TileW * 1;	// note: banner is composed of 2 parts, each of width STRIPE_BANNER_WIDTH
		private const int StripeBannerH = TileW * 2;
		private const int StripeBannerX = TitleFrameX;
		private const int StripeBannerY = TitleFrameY + TitleFrameH;

		private const int AssetsX = 0;
		private const int AssetsY = 0;

		// miscellaneous object graphics
		private const int MiscH = TileW * 1;
		private const int MiscX = AssetsX;
		private const int MiscY = AssetsY;

		// hud element graphics
		private const int HudElemH = TileW * 3;
		private const int HudElemX = AssetsX;
		private const int HudElemY = AssetsY + MiscH;

		// projectile graphics
		private const int ProjectilesH = TileW * 1;
		private const int ProjectilesX = AssetsX;
		private const int ProjectilesY = HudElemY + HudElemH;
		private const int ProjectilesVariants = 2;

		private const int PlayerBulletX = TileW * 13;
		private const int PlayerBulletFrames = 2;

		// power graphics
		private const int PowerFxH = TileW * 2;
		private const int PowerFxX = AssetsX;
		private const int PowerFxY = ProjectilesY + ProjectilesH;

		// text string graphics
		private const int StringsH = TileW * 1;
		private const int StringsX = AssetsX;
		private const int StringsY = PowerFxY + PowerFxH;

		// player graphics
		private const int PlayerW = TileW * 2;
		private const int PlayerFullH = TileW * 3;
		private const int PlayerSplitH = TileW * 2;
		private const int PlayerX = AssetsX;
		private const int PlayerFullY = StringsY + StringsH;
		private const int PlayerSplitY = PlayerFullY + PlayerFullH;

		private const int PlayerIdleFrames = 1;
		private const int PlayerPoseFrames = 3;
		private const int PlayerPoseX = PlayerX + PlayerW * PlayerIdleFrames;
		private const int PlayerSpecialFrames = 2;
		private const int PlayerSpecialX = PlayerPoseX + PlayerW * PlayerPoseFrames;
		private const int PlayerPowerX = PlayerSpecialX + PlayerW * PlayerSpecialFrames;
		private const int PlayerPowerFrames = 3;

		private const int PlayerRunFrames = 4;
		private const int PlayerLegsIdleX = PlayerX;
		private const int PlayerLegsRunX = PlayerLegsIdleX + PlayerW * PlayerIdleFrames;
		
		private const int PlayerBodyRunX = PlayerLegsRunX + PlayerW * PlayerRunFrames;
		private const int PlayerBodyFireX = PlayerBodyRunX + PlayerW * PlayerRunFrames;
		private const int PlayerArmsFireX = PlayerBodyFireX + PlayerW * PlayerRunFrames;

		// title screen graphics
		private const int TitleTextW = PlayerSplitY + PlayerSplitH;
		private const int TitleTextX = AssetsX;

		private const int TitleCodenameH = TileW * 2;
		private const int TitleCodenameY = TitleTextW;

		private const int TitleSailorH = TileW * 5;
		private const int TitleSailorY = TitleCodenameY + TitleCodenameH;

		private const int TitleArcadeH = TileW * 1;
		private const int TitleArcadeY = TitleSailorY + TitleSailorH;

		private const int TitleSignatureW = TileW * 5;
		private const int TitleSignatureH = TileW * 1;
		private const int TitleSignatureY = TitleArcadeY + TitleArcadeH;

		private const int TitleRedVW = TileW * 5;
		private const int TitleRedVH = TileW * 8;
		private const int TitleRedVX = TitleTextW + TitleTextX;
		private const int TitleRedVY = TitleCodenameY;

		private const int TitleFrameW = TileW * 7;
		private const int TitleFrameH = TileW * 1;
		private const int TitleFrameX = TitleRedVX + 2 * TitleRedVW;
		private const int TitleFrameY = TitleCodenameY;
		private const int TitleStartW = TileW * 6;
		private const int TitleStartH = TileW * 1;

		// world map graphics
		private const int MapTilesX = AssetsX;
		private const int MapTilesY = TitleSignatureY + TitleSignatureH;

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

		public enum ProjectileType
		{
			Lightgun,
			Debris,
			Bullet,
			Energy
		}
		public enum MenuOptions
		{
			Retry,
			Quit
		}

		/* Gameplay Variables */

		// program
		private static Vector2 _gameStartCoords;
		private static Vector2 _gameEndCoords;
		private behaviorAfterMotionPause _behaviorAfterPause;

		// active game actors
		public List<Bullet> PlayerBullets = new List<Bullet>();
		private static List<Bullet> _enemyBullets = new List<Bullet>();
		private static List<Monster> _enemies = new List<Monster>();
		private static List<TemporaryAnimatedSprite> _temporaryAnimatedSprites = new List<TemporaryAnimatedSprite>();
		private static List<int> _playerMovementDirections = new List<int>();
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

		public static int TileSize => TileW;

		public LightGunGame()
		{
			_arcadeTexture = ModEntry.Instance.Helper.Content.Load<Texture2D>(
				Path.Combine(Const.AssetsPath, Const.MapsPath, Const.ArcadeSpritesFile + ".png"));

			// Reload assets customised by the arcade game
			// ie. LooseSprites/Cursors
			ModEntry.Instance.Helper.Content.AssetEditors.Add(new Editors.ArcadeAssetEditor());

			_shotsSuccessful = 0;
			_shotsFired = 0;
			_totalTimer = 0;

			reset();
		}

		public void unload()
		{
			_health = GameHealthMax;
			_lives = GameLivesDefault;
		}

		public void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (_onStartMenu)
			{	// Progress through the start menu cutscene
				_cutscenePhase++;
			}
			else
			{
				if (_cutsceneTimer <= 0 && _playerPowerTimer <= 0 && _playerSpecialTimer <= 0 && _respawnTimer <= 0 && _gameEndTimer <= 0 && _gameRestartTimer <= 0 && _playerFireTimer <= 1)
					// Fire lightgun trigger
					playerFire();
			}
		}

		public void leftClickHeld(int x, int y)
		{
			if (_cutsceneTimer <= 0 && _playerPowerTimer <= 0 && _playerSpecialTimer <= 0 && _respawnTimer <= 0 && _gameEndTimer <= 0 && _gameRestartTimer <= 0 && _playerFireTimer <= 1)
				// Fire lightgun trigger
				playerFire();
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
				addPlayerMovementDirection(MoveLeft);
				flag = true;
			}
			if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k)
			    && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
			{
				// Move right
				addPlayerMovementDirection(MoveRight);
				flag = true;
			}
			if (flag)
				return;
			
			if (ModEntry.Instance.Config.DebugMode && ModEntry.Instance.Config.DebugArcadeCheats) {
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
						endCurrentStage();
						break;
					case Keys.OemCloseBrackets:
						Log.D($"_whichWorld : {_whichWorld} -> {_whichWorld + 1}");
						endCurrentWorld();
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
						playerSpecialStart();
					}
					break;
				case Keys.Escape:
					// End minigame
					quitMinigame();
					break;
				case Keys.A:
					// Move left
					addPlayerMovementDirection(MoveLeft);
					_playerAnimationTimer = 0;
					break;
				case Keys.D:
					// Move right
					addPlayerMovementDirection(MoveRight);
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
					if (_playerMovementDirections.Contains(MoveRight))
					{
						_playerMovementDirections.Remove(MoveRight);
					}
					flag = true;
				}
				if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k)
				    && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
				{
					if (_playerMovementDirections.Contains(MoveLeft))
					{
						_playerMovementDirections.Remove(MoveLeft);
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
				if (!_playerMovementDirections.Contains(MoveLeft))
					return;
				_playerMovementDirections.Remove(MoveLeft);
			}
			else if (k == Keys.D)
			{
				// Move right
				if (!_playerMovementDirections.Contains(MoveRight))
					return;
				_playerMovementDirections.Remove(MoveRight);
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
			return Const.ArcadeMinigameName;
		}

		public bool doMainGameUpdates()
		{
			return false;
		}

		public void changeScreenSize()
		{
			// TODO : include GameX and GameY into position calculations

			_gameStartCoords = new Vector2(
				(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width - GameW * SpriteScale) / 2,
				(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height - GameH * SpriteScale) / 2);
					
			_gameEndCoords = new Vector2(
				_gameStartCoords.X + GameX * SpriteScale + GameW * SpriteScale,
				_gameStartCoords.Y + GameY * SpriteScale + GameH * SpriteScale);

			Log.D("mGameStartCoords: "
				+ $"({Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width} - {GameW * SpriteScale}) / 2,\n"
				+ $"({Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height} - {GameH * SpriteScale}) / 2,\n"
				+ $"= {_gameStartCoords.X}, {_gameStartCoords.Y}",
				ModEntry.Instance.Config.DebugMode);

			Log.D($"mGameEndCoords: {_gameStartCoords.X}, {_gameStartCoords.Y}",
				ModEntry.Instance.Config.DebugMode);
		}

		private void quitMinigame()
		{
			if (Game1.currentLocation != null && Game1.currentLocation.Name.Equals((object)"Saloon") && Game1.timeOfDay >= 1700)
				Game1.changeMusicTrack("Saloon1");
			unload();
			Game1.currentMinigame = null;
		}

		public bool forceQuit()
		{
			return false;
		}

		private void reset()
		{
			changeScreenSize();

			_enemyBullets.Clear();
			_enemies.Clear();
			_temporaryAnimatedSprites.Clear();

			PlayerPosition = new Vector2(
					_gameStartCoords.X + GameX + (GameW * SpriteScale / 2),
					_gameStartCoords.Y + GameY + (GameH * SpriteScale) - (PlayerFullH * SpriteScale)
			);

			// Player bounding box only includes the bottom 2/3rds of the sprite.
			PlayerBoundingBox.X = (int)PlayerPosition.X;
			PlayerBoundingBox.Y = (int)PlayerPosition.Y;
			PlayerBoundingBox.Width = PlayerW * SpriteScale;
			PlayerBoundingBox.Height = PlayerSplitH * SpriteScale;

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

			if (ModEntry.Instance.Config.DebugMode && !ModEntry.Instance.Config.DebugArcadeSkipIntro)
				_onStartMenu = true;
		}

		private void addPlayerMovementDirection(int direction)
		{
			if (_playerMovementDirections.Contains(direction))
				return;
			_playerMovementDirections.Add(direction);
		}

		private void playerPowerup(int which)
		{
			switch (which)
			{
				case PowerupHealth:
					_health = Math.Min(GameHealthMax, _health + PowerupHealthAmount);

					// todo spawn a new temporary sprite at the powerup object coordinates

					break;
			}
		}

		private void playerSpecialStart()
		{
			_playerAnimationPhase = 0;
			_playerSpecialTimer = 1;
		}
		
		private void playerSpecialEnd()
		{
			// Energy levels between 0 and the low-threshold will use a light special power.
			_activeSpecialPower = _energy >= GameEnergyMax ? SpecialPower.Normal : SpecialPower.Megaton;

			_playerAnimationPhase = 0;
			_playerSpecialTimer = 0;
		}

		private void playerPowerEnd()
		{
			_activeSpecialPower = SpecialPower.None;
			_playerAnimationPhase = 0;
			_playerPowerTimer = 0;
			_energy = 0;
		}

		private void playerFire()
		{
			// Position the source around the centre of the player
			var src = new Vector2(
				PlayerPosition.X + (PlayerW / 2 * SpriteScale),
				PlayerPosition.Y + (PlayerSplitH / 2 * SpriteScale));

			// Position the target on the centre of the cursor
			var dest = new Vector2(
				ModEntry.Instance.Helper.Input.GetCursorPosition().ScreenPixels.X + (TileW / 2 * SpriteScale),
				ModEntry.Instance.Helper.Input.GetCursorPosition().ScreenPixels.Y + (TileW / 2 * SpriteScale));
			spawnBullets(src, dest, GamePlayerDamage, ProjectileType.Lightgun);
			_playerFireTimer = GameFireDelay;

			// Mirror player sprite to face target
			if (_playerMovementDirections.Count == 0) _mirrorPlayerSprite = dest.X < PlayerPosition.X
				? SpriteEffects.FlipHorizontally
				: SpriteEffects.None;

			++_shotsFired;
		}

		private bool playerTakeDamage(int damage)
		{
			// todo: animate player

			_health = Math.Max(0, _health - damage);
			if (_health <= 0)
				return true;
			
			_screenFlashColor = new Color(new Vector4(255, 0, 0, 0.25f));
			_screenFlashTimer = 200;

			return false;
		}

		private void playerBeforeDeath()
		{
			// todo: fill this function with pre-death animation timer etc

			playerDie();
		}

		private void playerDie()
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
				endFunction = playerGameOverCheck
			});

			_respawnTimer *= 3f;
		}

		private void playerGameOverCheck(int extra)
		{
			if (_lives >= 0)
			{
				playerRespawn();
				return;
			}
			_onGameOver = true;
			_enemies.Clear();
			quitMinigame();
			++Game1.currentLocation.currentEvent.CurrentCommand;
		}

		private void playerRespawn()
		{
			_playerInvincibleTimer = GameInvincibleDelay;
			_health = GameHealthMax;
		}

		private void endCurrentStage()
		{
			_playerMovementDirections.Clear();

			// todo: set cutscenes, begin events, etc
		}

		private void endCurrentWorld()
		{
			// todo: set cutscenes, begin events, etc
		}

		// Begin the next stage within a world
		private void startNewStage()
		{
			++_whichStage;
			_stageMap = getMap(_whichStage);
		}

		// Begin the next world
		private void startNewWorld()
		{
			++_whichWorld;
		}

		// New Game Plus
		private void startNewGame()
		{
			_gameRestartTimer = 2000;
			++_whichGame;
		}

		private void spawnBullets(Vector2 spawn, Vector2 dest, int damage, ProjectileType type)
		{
			// Rotation
			var radiansBetween = Vector.RadiansBetween(dest, spawn);
			radiansBetween -= (float)(Math.PI / 2.0d);
			Log.D($"RadiansBetween {spawn}, {dest} = {radiansBetween:0.00}rad",
				ModEntry.Instance.Config.DebugMode);

			// Vector of motion
			var motion = Vector.PointAt(spawn, dest);
			motion.Normalize();

			// Spawn position
			var position = spawn + motion * (GameProjSpeed[(int)type] * 5);

			// Add the bullet to the active lists for the respective spawner
			if (type == ProjectileType.Lightgun)
				PlayerBullets.Add(new Bullet(position, motion, radiansBetween, type));
			else
				_enemyBullets.Add(new Bullet(position, motion, radiansBetween, type));
		}

		private void updateBullets(GameTime time)
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
							TileW * SpriteScale, 
							TileW * SpriteScale)))
					{
						if (!_enemies[j].takeDamage(GamePlayerDamage))
						{
							_enemies[j].die();
							_enemies.RemoveAt(j);
							PlayerBullets.RemoveAt(i);
						}
					}
					break;
				}

				// Update bullet positions
				PlayerBullets[i].Position += PlayerBullets[i].Motion * GameProjSpeed[(int)PlayerBullets[i].Type];

				// Remove offscreen bullets
				if (PlayerBullets[i].Position.X <= _gameStartCoords.X || PlayerBullets[i].Position.X >= _gameEndCoords.X
				|| PlayerBullets[i].Position.Y <= _gameStartCoords.Y || PlayerBullets[i].Position.Y >= _gameEndCoords.Y)
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
							TileW * SpriteScale,
							TileW * SpriteScale))) {
						if (!playerTakeDamage(GameEnemyDamage))
						{
							playerDie();
							_enemyBullets.RemoveAt(i);
						}
					}
					break;
				}

				// Update bullet positions
				_enemyBullets[i].RotationCur += GameProjRotDelta[i];
				_enemyBullets[i].Position += _enemyBullets[i].Motion * GameProjSpeed[(int)_enemyBullets[i].Type];

				// Remove offscreen bullets
				if (_enemyBullets[i].Position.X <= _gameStartCoords.X || _enemyBullets[i].Position.X >= _gameEndCoords.X
				|| _enemyBullets[i].Position.Y <= _gameStartCoords.Y || _enemyBullets[i].Position.Y >= _gameEndCoords.Y)
				{
					_enemyBullets.RemoveAt(i);
				}
			}
		}

		public int[,] getMap(int wave)
		{
			var map = new int[MapH, MapW];
			for (var i = 0; i < MapH; ++i)
			{
				for (var j = 0; j < MapW; ++j)
					map[i, j] = i != 0 && i != 15 && (j != 0 && j != 15) || (i > 6 && i < 10 || j > 6 && j < 10)
						? (i == 0 || i == 15 || (j == 0 || j == 15) 
							? (Game1.random.NextDouble() < 0.15 ? 1 : 0) 
							: (i == 1 || i == 14 || (j == 1 || j == 14) 
								? 2 
								: (Game1.random.NextDouble() < 0.1? 4 : 3))) 
						: 5;
			}
			switch (wave)
			{
				case -1:
					for (var i = 0; i < MapH; ++i)
					{
						for (var j = 0; j < MapW; ++j)
						{
							if (map[i, j] == 0 || map[i, j] == 1 || (map[i, j] == 2 || map[i, j] == 5))
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

		public bool tick(GameTime time)
		{
			/* Game Management */

			TimeSpan elapsedGameTime;

			// Exit the game
			if (_hasPlayerQuit)
			{
				quitMinigame();
				return true;
			}

			// Run through the restart game timer
			if (_gameRestartTimer > 0)
			{
				elapsedGameTime = time.ElapsedGameTime;
				_gameRestartTimer -= time.ElapsedGameTime.Milliseconds;
				if (_gameRestartTimer <= 0)
				{
					unload();
					Game1.currentMinigame = new LightGunGame();
				}
			}

			// Run through screen effects
			if (_screenFlashTimer > 0)
			{
				elapsedGameTime = time.ElapsedGameTime;
				_screenFlashTimer -= time.ElapsedGameTime.Milliseconds;
			}

			// Run down player invincibility
			if (_playerInvincibleTimer > 0)
			{
				elapsedGameTime = time.ElapsedGameTime;
				_playerInvincibleTimer -= time.ElapsedGameTime.Milliseconds;
			}

			// Run down player lightgun animation
			if (_playerFireTimer > 0)
			{
				--_playerFireTimer;
			}

			// Handle player special trigger
			if (_playerSpecialTimer > 0)
			{
				elapsedGameTime = time.ElapsedGameTime;
				_playerSpecialTimer += time.ElapsedGameTime.Milliseconds;

				// Progress through player special trigger animation

				// Events 
				if (_playerAnimationPhase == PlayerSpecialFrames)
				{
					// Transfer into the active special power
					playerSpecialEnd();
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
				elapsedGameTime = time.ElapsedGameTime;
				_playerPowerTimer += time.ElapsedGameTime.Milliseconds;

				// Progress through player power

				// Events
				if (_playerAnimationPhase == PlayerPowerFrames)
				{
					// Return to usual game flow
					playerPowerEnd();
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
				// Update bullet data
				updateBullets(time);

				if (_playerSpecialTimer <= 0 && _playerPowerTimer <= 0)
				{
					// While the game is engaging the player
					if (_cutsceneTimer <= 0)
					{
						// Run down the death timer
						if (_respawnTimer > 0.0)
						{
							elapsedGameTime = time.ElapsedGameTime;
							_respawnTimer -= elapsedGameTime.Milliseconds;
						}

						// Handle player movement
						if (_playerMovementDirections.Count > 0)
						{
							switch (_playerMovementDirections.ElementAt(0))
							{
								case MoveRight:
									_mirrorPlayerSprite = SpriteEffects.None;
									if (PlayerPosition.X + PlayerBoundingBox.Width < _gameEndCoords.X)
										PlayerPosition.X += GameMoveSpeed;
									else
										PlayerPosition.X = _gameEndCoords.X - PlayerBoundingBox.Width;
									break;
								case MoveLeft:
									_mirrorPlayerSprite = SpriteEffects.FlipHorizontally;
									if (PlayerPosition.X > _gameStartCoords.X)
										PlayerPosition.X -= GameMoveSpeed;
									else
										PlayerPosition.X = _gameStartCoords.X;
									break;
							}
						}

						elapsedGameTime = time.ElapsedGameTime;
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

			// Sit on the start game screen until prompted elsewhere
			if (_onGameOver || _onStartMenu)
			{
				elapsedGameTime = time.ElapsedGameTime;

				// Cutscene phase 0 on the title screen progresses by click or by screen event
				if (_cutscenePhase >= 1)
				{
					_cutsceneTimer += elapsedGameTime.Milliseconds;
				}

				// Progress through a small intro sequence

				switch (_cutscenePhase)
				{
					// Events
					case 0:
					{
						_cutsceneBackgroundPosition += GameW / UiAnimTimescale;
						if (_cutsceneBackgroundPosition >= GameW * 4 / 3)
						{
							++_cutscenePhase;
						}

						break;
					}
					// Timers
					case 1:
					{
						if (_cutsceneTimer >= TimerTitlePhase1)
							++_cutscenePhase;
						break;
					}
					case 2 when _cutsceneTimer < TimerTitlePhase1:
						_cutsceneTimer = TimerTitlePhase1;
						break;
					case 2:
					{
						if (_cutsceneTimer >= TimerTitlePhase5)
							++_cutscenePhase;
						break;
					}
					case 3:
					{
						if (_cutsceneTimer < TimerTitlePhase5)
							_cutsceneTimer = TimerTitlePhase5;
						break;
					}
					case 4:
						// End the cutscene and begin the game after the user clicks past the end of intro cutscene (phase 3)
						_cutsceneTimer = 0;
						_cutscenePhase = 0;
						_onStartMenu = false;
						break;
				}

			}

			// Run through the end of world cutscene
			else if (_onWorldComplete)
			{
				elapsedGameTime = time.ElapsedGameTime;
				var delta = elapsedGameTime.Milliseconds * (double)AnimCutsceneBackgroundSpeed;
				_cutsceneBackgroundPosition = (float)(_cutsceneBackgroundPosition + delta) % 96f;
			}

			return false;
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
						(int)(_gameEndCoords.X - _gameStartCoords.X),
						(int)(_gameEndCoords.Y - _gameStartCoords.Y)),
					Game1.staminaRect.Bounds,
					_screenFlashColor,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					1f);
			}

			// todo: draw health bar to destrect width * health / healthmax
			//			or draw health bar as icons onscreen (much nicer)
			// todo: draw ammo container
			// todo: drawstring stage time countdown
			
			// Draw the active game
			if (!_onStartMenu && _respawnTimer <= 0.0)
			{
				// debug makito
				// very important
				b.Draw(
						_arcadeTexture, 
						new Rectangle(
							(int)(_gameEndCoords.X - _gameStartCoords.X),
							(int)(_gameEndCoords.Y - _gameStartCoords.Y),
							TileSize * SpriteScale,
							TileSize * SpriteScale),
						new Rectangle(
							0,
							0,
							TileSize,
							TileSize),
						Color.White,
						0.0f,
						new Vector2(
							TileSize / 2,
							TileSize / 2),
						_mirrorPlayerSprite,
						1.0f);
				// very important
				// debug makito

				// Draw the player
				if (_playerInvincibleTimer <= 0 && _playerInvincibleTimer / 100 % 2 == 0)
				{
					var destRects = new Rectangle[3];
					var srcRects = new Rectangle[3];

					// Draw full body action sprites
					if (_playerSpecialTimer > 0)
					{	// Activated special power
						destRects[0] = new Rectangle(
							(int)PlayerPosition.X,
							(int)PlayerPosition.Y,
							PlayerBoundingBox.Width,
							PlayerFullH * SpriteScale);
						srcRects[0] = new Rectangle(
							PlayerSpecialX + (PlayerW * _playerAnimationPhase),
							PlayerFullY,
							PlayerW,
							PlayerFullH);
					}
					else if (_activeSpecialPower != SpecialPower.None)
					{	// Player used a special power
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
							PlayerPowerX + (PlayerW * PowerAnims[(int)_activeSpecialPower]) + (PlayerW * _playerAnimationPhase),
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
							if (_playerMovementDirections.Count > 0)
							{   // Firing running torso
								var frame = (int)(_playerAnimationTimer / PlayerAnimTimescale);
								destRects[0] = new Rectangle(
									(int)PlayerPosition.X,
									(int)PlayerPosition.Y,
									PlayerBoundingBox.Width,
									PlayerBoundingBox.Height);
								srcRects[0] = new Rectangle(
									PlayerBodyFireX + (PlayerW * frame),
									PlayerSplitY,
									PlayerW,
									PlayerSplitH);
							}
							else
							{	// Firing standing torso
								destRects[0] = new Rectangle(
									(int)PlayerPosition.X,
									(int)PlayerPosition.Y,
									PlayerBoundingBox.Width,
									PlayerBoundingBox.Height);
								srcRects[0] = new Rectangle(
									PlayerBodyFireX,
									PlayerSplitY,
									PlayerW,
									PlayerSplitH);
							}
							if (PlayerBullets.Count > 0)
							{   // Firing arms
								var radiansBetween = Vector.RadiansBetween(
									ModEntry.Instance.Helper.Input.GetCursorPosition().AbsolutePixels, PlayerPosition);
								radiansBetween -= (float)(Math.PI / 2.0d);
								var frame = (int)Math.Min(PlayerBulletFrames, Math.Abs(radiansBetween));
								//Log.D($"Rad: {radiansBetween} | Frame: {frame}", ModEntry.Instance.Config.DebugMode);
								destRects[2] = new Rectangle(
									(int)PlayerPosition.X,
									(int)PlayerPosition.Y,
									PlayerBoundingBox.Width,
									PlayerBoundingBox.Height);
								srcRects[2] = new Rectangle(
									PlayerArmsFireX + PlayerW * frame,
									PlayerSplitY,
									PlayerW,
									PlayerSplitH);
							}
						}
						else
						{   // Running torso
							var frame = (int)(_playerAnimationTimer / PlayerAnimTimescale);
							destRects[0] = new Rectangle(
								(int)PlayerPosition.X,
								(int)PlayerPosition.Y,
								PlayerBoundingBox.Width,
								PlayerBoundingBox.Height);
							srcRects[0] = new Rectangle(
								PlayerBodyRunX + (PlayerW * frame),
								PlayerSplitY,
								PlayerW,
								PlayerSplitH);
						}
					}

					// Draw appropriate sprite legs
					if (_playerMovementDirections.Count > 0)
					{   // Running
						var frame = (int)(_playerAnimationTimer / PlayerAnimTimescale);
						destRects[1] = new Rectangle(
							(int)PlayerPosition.X,
							(int)PlayerPosition.Y + (PlayerFullH - PlayerSplitH) * SpriteScale,
							PlayerBoundingBox.Width,
							PlayerBoundingBox.Height);
						srcRects[1] = new Rectangle(
							PlayerLegsRunX + (PlayerW * frame),
							PlayerSplitY,
							PlayerW,
							PlayerSplitH);
					}
					else if (_playerFireTimer > 0)
					{   // Standing and firing
						destRects[1] = new Rectangle(
							(int)PlayerPosition.X,
							(int)PlayerPosition.Y + (PlayerFullH - PlayerSplitH) * SpriteScale,
							PlayerBoundingBox.Width,
							PlayerBoundingBox.Height);
						srcRects[1] = new Rectangle(
							PlayerX,
							PlayerSplitY,
							PlayerW,
							PlayerSplitH);
					}

					// Draw the player from each component sprite
					for(var i = 2; i >= 0; --i)
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
								(float)(PlayerPosition.Y / 10000.0 + i / 1000.0 + 1.0 / 1000.0));
						}
					}
				}

				// Draw player bullets
				foreach (var playerBullet in PlayerBullets)
				{
					b.Draw(
						_arcadeTexture,
						new Vector2(
							playerBullet.Position.X,
							playerBullet.Position.Y),
						new Rectangle(
							ProjectilesX + (TileSize * (int)playerBullet.Type),
							ProjectilesY,
							TileSize,
							TileSize),
						Color.White,
						playerBullet.Rotation,
						new Vector2(
							TileSize / 2,
							TileSize / 2),
						SpriteScale,
						SpriteEffects.None,
						0.9f);
				}

				// Draw enemy bullets
				foreach (var enemyBullet in _enemyBullets)
				{
					b.Draw(
						_arcadeTexture,
						new Vector2(
							enemyBullet.Position.X,
							enemyBullet.Position.Y),
						new Rectangle(
							ProjectilesX + (TileSize * (int)enemyBullet.Type),
							ProjectilesY,
							TileSize,
							TileSize),
						Color.White,
						enemyBullet.Rotation,
						Vector2.Zero,
						SpriteScale,
						SpriteEffects.None,
						0.9f);
				}

				// Draw all the stuff
				foreach (var temporarySprite in _temporaryAnimatedSprites)
					temporarySprite.draw(b, true);

				// Draw enemies
				foreach (var monster in _enemies)
					monster.draw(b);

				// Draw the background
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
									(int)(_gameEndCoords.X - _gameStartCoords.X),
									(int)(_gameEndCoords.Y - _gameStartCoords.Y)),
								new Rectangle(
									TitleFrameX,
									TitleFrameY,
									TileSize,
									TileSize),
								Color.Black,
								0.0f,
								Vector2.Zero,
								SpriteEffects.None,
								0.0f);
						}
						else
						{
							goto case SpecialPower.None;
						}
						break;
					case SpecialPower.None:
					case SpecialPower.Sulphur:
					case SpecialPower.Incense:
						// Draw the game map
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								(int)_gameStartCoords.X,
								(int)_gameStartCoords.Y,
								(int)(_gameEndCoords.X - _gameStartCoords.X),
								(int)(_gameEndCoords.Y - _gameStartCoords.Y)),
							new Rectangle(
								TitleFrameX,
								TitleFrameY,
								TileSize,
								TileSize),
							Color.White,
							0.0f,
							Vector2.Zero,
							SpriteEffects.None,
							0.0f);
						break;
				}


				/* Render the HUD */


				// todo: draw health bar objects
				// todo: draw energy bar objects
				// todo: draw life and counter
			}


			/* Render Cutscenes & Menus */


			// Display Start menu
			if (_onStartMenu)
			{
				// Render each phase of the intro

				// Black backdrop
				b.Draw(
					_arcadeTexture,
					new Rectangle(
						0,
						0,
						Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width,
						Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height),
					new Rectangle(
						TitleFrameX,
						TitleFrameY,
						TileSize,
						TileSize),
					Color.Black,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					0.0f);

				if (_cutscenePhase == 0)
				{
					// Draw the white title banner silhouetted on black

					// White V
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 3,
							Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 2,
							TitleRedVW * SpriteScale,
							TitleRedVH * SpriteScale),
						new Rectangle(
							TitleRedVX + TitleRedVW,
							TitleRedVY,
							TitleRedVW,
							TitleRedVH),
						Color.White,
						0.0f,
						new Vector2(
							TitleRedVW / 2,
							TitleRedVH / 2),
						SpriteEffects.None,
						0.5f);

					// Pan a vertical letterbox across the screen to simulate light gleam

					// Black frame
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							-(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width * (TitleFrameW / 3 * 2)) + ((int)_cutsceneBackgroundPosition),
							0,
							Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width * TitleFrameW,
							Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height),
						new Rectangle(
							TitleFrameX,
							TitleFrameY,
							TitleFrameW,
							TitleFrameH),
						Color.White,
						0.0f,
						Vector2.Zero,
						SpriteEffects.None,
						1.0f);
				}

				if (_cutscenePhase >= 1)
				{
					// Draw the coloured title banner on black

					// Game title banner
					b.Draw(
						_arcadeTexture,
						new Rectangle(
							Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 3,
							Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 2,
							TitleRedVW * SpriteScale,
							TitleRedVH * SpriteScale),
						new Rectangle(
							TitleRedVX,
							TitleRedVY,
							TitleRedVW,
							TitleRedVH),
						Color.White,
						0.0f,
						new Vector2(
							TitleRedVW / 2,
							TitleRedVH / 2),
						SpriteEffects.None,
						0.1f);
				}

				if (_cutscenePhase >= 2)
				{
					// Draw the coloured title banner with all title screen text

					if (_cutsceneTimer >= TimerTitlePhase2)
					{
						// "Codename"
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 2,
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 1,
								TitleTextW * SpriteScale,
								TitleCodenameH * SpriteScale),
							new Rectangle(
								TitleTextX,
								TitleCodenameY,
								TitleTextW,
								TitleCodenameH),
							Color.White,
							0.0f,
							new Vector2(
								TitleTextW / 2,
								TitleCodenameH / 2),
							SpriteEffects.None,
							1.0f);
					}

					if (_cutsceneTimer >= TimerTitlePhase3)
					{
						// "Sailor"
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 2,
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 2,
								TitleTextW * SpriteScale,
								TitleSailorH * SpriteScale),
							new Rectangle(
								TitleTextX,
								TitleSailorY,
								TitleTextW,
								TitleSailorH),
							Color.White,
							0.0f,
							new Vector2(
								TitleTextW / 2,
								TitleSailorH / 2),
							SpriteEffects.None,
							0.9f);
					}

					if (_cutsceneTimer >= TimerTitlePhase4)
					{
						// "Arcade Game"
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 2,
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 2,
								TitleTextW * SpriteScale,
								TitleArcadeH * SpriteScale),
							new Rectangle(
								TitleTextX,
								TitleArcadeY,
								TitleTextW,
								TitleArcadeH),
							Color.White,
							0.0f,
							new Vector2(
								TitleTextW / 2,
								TitleArcadeH / 2),
							SpriteEffects.None,
							0.8f);
					}

				}

				if (_cutscenePhase >= 3)
				{
					// Display flashing 'fire to start' text and signature text

					if (_cutsceneTimer >= TimerTitlePhase5)
					{
						// "Blueberry 1991-1996"
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 2,
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 10 * 9,
								TitleSignatureW * SpriteScale,
								TitleSignatureH * SpriteScale),
							new Rectangle(
								TitleTextX,
								TitleSignatureY,
								TitleSignatureW,
								TitleSignatureH),
							Color.White,
							0.0f,
							new Vector2(
								TitleSignatureW / 2,
								TitleSignatureH / 2),
							SpriteEffects.None,
							1.0f);
					}

					if (_cutsceneTimer >= TimerTitlePhase6 && (_cutsceneTimer / 500) % 2 == 0)
					{
						// "Fire to start"
						b.Draw(
							_arcadeTexture,
							new Rectangle(
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 2,
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 4,
								TitleStartW * SpriteScale,
								TitleStartH * SpriteScale),
							new Rectangle(
								0,
								TileSize,
								TitleStartW,
								TitleStartH),
							Color.White,
							0.0f,
							new Vector2(
								TitleStartW / 2,
								TitleStartH / 2),
							SpriteEffects.None,
							1.0f);
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
						16 * TileSize,
						16 * TileSize),
					Game1.staminaRect.Bounds,
					Color.Black,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					0.0001f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					_gameStartCoords + new Vector2(6f, 7f) * TileSize,
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					_gameStartCoords + new Vector2(6f, 7f) * TileSize + new Vector2(-1f, 0.0f),
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					_gameStartCoords + new Vector2(6f, 7f) * TileSize + new Vector2(1f, 0.0f),
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
						_gameStartCoords + new Vector2(6f, 9f) * TileSize,
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
					_gameStartCoords + new Vector2(6f, 9f) * TileSize + new Vector2(0.0f, (2 / 3)),
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

			b.End();
		}
		
		public delegate void behaviorAfterMotionPause();

		public class Powerup
		{
			public int Which;
			public Point Position;
			public int Duration;
			public float YOffset;

			public Powerup(int which, Point position, int duration)
			{
				Which = which;
				Position = position;
				Duration = duration;
			}

			public void draw(SpriteBatch b)
			{
				if (Duration <= 2000 && Duration / 200 % 2 != 0)
					return;
				b.Draw(
					_arcadeTexture,
					_gameStartCoords + new Vector2(Position.X, Position.Y + YOffset),
					new Rectangle(
						TileW,
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
		
		public class Bullet
		{
			public Vector2 Position;        // Current position on screen
			public Vector2 Motion;          // Line of motion between spawn and dest
			public Vector2 MotionCur;       // Current position along vector of motion between spawn and dest
			public float Rotation;          // Angle of vector between spawn and dest
			public float RotationCur;       // Angle of vector between spawn and dest
			public ProjectileType Type;		// Which projectile, ie. light/heavy/bullet/lightgun

			public Bullet(Vector2 position, Vector2 motion, float rotation, ProjectileType type)
			{
				Position = position;
				Motion = motion;
				Rotation = rotation;
				Type = type;
				
				Motion = motion;

				MotionCur = motion;
				RotationCur = rotation;
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
				Position = new Rectangle(position.X, position.Y, TileSize, TileSize);
			}

			public Monster(int which, Point position)
			{
				Type = which;
				Position = new Rectangle(position.X, position.Y, TileSize, TileSize);
				switch (Type)
				{
					// todo: enemy spawn parameters
				}
			}

			public virtual void draw(SpriteBatch b)
			{
			}

			public virtual bool takeDamage(int damage)
			{
				Health = Math.Max(0, Health - damage);
				if (Health <= 0)
					return false;
				_flashColor = Color.Red;
				FlashColorTimer = 100f;
				return true;
			}

			public virtual void die()
			{
				// todo: literally anything at all
				// drop rates if health low, whatever
			}

			public virtual int getLootDrop()
			{
				return 0;
			}
		}

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
	}

	/* nice code */

	/*
		if ((double) Utility.distance((float) this.playerBoundingBox.Center.X, (float) (AbigailGame.powerups[index].position.X + AbigailGame.TileSize / 2), (float) this.playerBoundingBox.Center.Y, (float) (AbigailGame.powerups[index].position.Y + AbigailGame.TileSize / 2)) <= (double) (AbigailGame.TileSize + 3) && (AbigailGame.powerups[index].position.X < AbigailGame.TileSize || AbigailGame.powerups[index].position.X >= 16 * AbigailGame.TileSize - AbigailGame.TileSize || (AbigailGame.powerups[index].position.Y < AbigailGame.TileSize || AbigailGame.powerups[index].position.Y >= 16 * AbigailGame.TileSize - AbigailGame.TileSize)))
		{
		if (AbigailGame.powerups[index].position.X + AbigailGame.TileSize / 2 < this.playerBoundingBox.Center.X)
			++AbigailGame.powerups[index].position.X;
		if (AbigailGame.powerups[index].position.X + AbigailGame.TileSize / 2 > this.playerBoundingBox.Center.X)
			--AbigailGame.powerups[index].position.X;
		if (AbigailGame.powerups[index].position.Y + AbigailGame.TileSize / 2 < this.playerBoundingBox.Center.Y)
			++AbigailGame.powerups[index].position.Y;
		if (AbigailGame.powerups[index].position.Y + AbigailGame.TileSize / 2 > this.playerBoundingBox.Center.Y)
			--AbigailGame.powerups[index].position.Y;
		}
	*/
}
