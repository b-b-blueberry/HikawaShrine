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
		internal static Config mConfig = Hikawa.SHelper.ReadConfig<Config>();

		/* Constant Values */

		// debug
		private bool debugCheats = mConfig.DebugArcadeCheats;
		private bool debugSkipIntro = mConfig.DebugArcadeSkipIntro;

		// program attributes
		private const float PLAYER_ANIM_TIMESCALE = 300f;
		private const float UI_ANIM_TIMESCALE = 120f;
		private const int SPRITE_SCALE = 2;
		private const int TILE_W = 16;
		private const int MOVE_IDLE = 0;
		private const int MOVE_RIGHT = 1;
		private const int MOVE_LEFT = 2;

		// cutscene timers
		private const int TIMER_TITLE_PHASE_1 = 800;	// red V
		private const int TIMER_TITLE_PHASE_2 = 1200;	// codename
		private const int TIMER_TITLE_PHASE_3 = 1600;	// sailor
		private const int TIMER_TITLE_PHASE_4 = 2000;	// arcade game
		private const int TIMER_TITLE_PHASE_5 = 2400;	// blueberry 1991-1996
		private const int TIMER_TITLE_PHASE_6 = 2800;   // fire to start

		private const int TIMER_SPECIAL_PHASE_1 = 1000; // stand ahead
		private const int TIMER_SPECIAL_PHASE_2 = 2400; // raise compact

		private const int TIMER_POWER_PHASE_1 = 1500;	// activate
		private const int TIMER_POWER_PHASE_2 = 5000;	// white glow
		private const int TIMER_POWER_PHASE_3 = 6500;	// cooldown

		// asset attributes

		// gameplay content
		private enum SpecialPower {
			NORMAL,
			MEGATON,
			SULPHUR,
			INCENSE,
			NONE,
		}
		
		private static readonly int[] POWER_ANIMS = {
			0,
			0, 
			1,
			1,
			0
		};

		private enum LightGunAimHeight {
			HIGH,
			MID,
			LOW
		}
		
		// hud graphics
		public const int CROSSHAIR_W = TILE_W * 1;
		public const int CROSSHAIR_H = TILE_W * 1;
		public const int CROSSHAIR_X = TILE_W * 2;
		public const int CROSSHAIR_Y = TILE_W * 0;

		private const int MAP_W = 20;
		private const int MAP_H = MAP_W;
		private const int HUD_W = TILE_W * 3;
		private const int HUD_H = TILE_W;
		private const int GAME_W = TILE_W * MAP_W;
		private const int GAME_H = TILE_W * MAP_H;
		private const int GAME_X = 0;
		private const int GAME_Y = 0;

		private const int STRIPE_BANNER_W = TILE_W * 1;	// note: banner is composed of 2 parts, each of width STRIPE_BANNER_WIDTH
		private const int STRIPE_BANNER_H = TILE_W * 2;
		private const int STRIPE_BANNER_X = TITLE_FRAME_X;
		private const int STRIPE_BANNER_Y = TITLE_FRAME_Y + TITLE_FRAME_H;

		private const int ASSETS_X = 0;
		private const int ASSETS_Y = 0;

		// miscellaneous object graphics
		private const int MISC_H = TILE_W * 1;
		private const int MISC_X = ASSETS_X;
		private const int MISC_Y = ASSETS_Y;

		// hud element graphics
		private const int HUDELEM_H = TILE_W * 3;
		private const int HUDELEM_X = ASSETS_X;
		private const int HUDELEM_Y = ASSETS_Y + MISC_H;

		// projectile graphics
		private const int PROJECTILES_H = TILE_W * 1;
		private const int PROJECTILES_X = ASSETS_X;
		private const int PROJECTILES_Y = HUDELEM_Y + HUDELEM_H;
		private const int PROJECTILES_VARIANTS = 2;

		private const int PLAYER_BULLET_X = TILE_W * 13;
		private const int PLAYER_BULLET_FRAMES = 2;

		// power graphics
		private const int POWERFX_H = TILE_W * 2;
		private const int POWERFX_X = ASSETS_X;
		private const int POWERFX_Y = PROJECTILES_Y + PROJECTILES_H;

		// text string graphics
		private const int STRINGS_H = TILE_W * 1;
		private const int STRINGS_X = ASSETS_X;
		private const int STRINGS_Y = POWERFX_Y + POWERFX_H;

		// player graphics
		private const int PLAYER_W = TILE_W * 2;
		private const int PLAYER_FULL_H = TILE_W * 3;
		private const int PLAYER_SPLIT_H = TILE_W * 2;
		private const int PLAYER_X = ASSETS_X;
		private const int PLAYER_FULL_Y = STRINGS_Y + STRINGS_H;
		private const int PLAYER_SPLIT_Y = PLAYER_FULL_Y + PLAYER_FULL_H;

		private const int PLAYER_IDLE_FRAMES = 1;
		private const int PLAYER_POSE_FRAMES = 3;
		private const int PLAYER_POSE_X = PLAYER_X + (PLAYER_W * PLAYER_IDLE_FRAMES);
		private const int PLAYER_SPECIAL_FRAMES = 2;
		private const int PLAYER_SPECIAL_X = PLAYER_POSE_X + (PLAYER_W * PLAYER_POSE_FRAMES);
		private const int PLAYER_POWER_X = PLAYER_SPECIAL_X + (PLAYER_W * PLAYER_SPECIAL_FRAMES);
		private const int PLAYER_POWER_FRAMES = 3;

		private const int PLAYER_RUN_FRAMES = 4;
		private const int PLAYER_LEGS_IDLE_X = PLAYER_X;
		private const int PLAYER_LEGS_RUN_X = PLAYER_LEGS_IDLE_X + PLAYER_W * PLAYER_IDLE_FRAMES;
		
		private const int PLAYER_BODY_RUN_X = PLAYER_LEGS_RUN_X + PLAYER_W * PLAYER_RUN_FRAMES;
		private const int PLAYER_BODY_FIRE_X = PLAYER_BODY_RUN_X + PLAYER_W * PLAYER_RUN_FRAMES;
		private const int PLAYER_ARMS_FIRE_X = PLAYER_BODY_FIRE_X + PLAYER_W * PLAYER_RUN_FRAMES;

		// title screen graphics
		private const int TITLE_TEXT_W = PLAYER_SPLIT_Y + PLAYER_SPLIT_H;
		private const int TITLE_TEXT_X = ASSETS_X;

		private const int TITLE_CODENAME_H = TILE_W * 2;
		private const int TITLE_CODENAME_Y = TITLE_TEXT_W;

		private const int TITLE_SAILOR_H = TILE_W * 5;
		private const int TITLE_SAILOR_Y = TITLE_CODENAME_Y + TITLE_CODENAME_H;

		private const int TITLE_ARCADE_H = TILE_W * 1;
		private const int TITLE_ARCADE_Y = TITLE_SAILOR_Y + TITLE_SAILOR_H;

		private const int TITLE_SIGNATURE_W = TILE_W * 5;
		private const int TITLE_SIGNATURE_H = TILE_W * 1;
		private const int TITLE_SIGNATURE_Y = TITLE_ARCADE_Y + TITLE_ARCADE_H;

		private const int TITLE_REDV_W = TILE_W * 5;
		private const int TITLE_REDV_H = TILE_W * 8;
		private const int TITLE_REDV_X = TITLE_TEXT_W + TITLE_TEXT_X;
		private const int TITLE_REDV_Y = TITLE_CODENAME_Y;

		private const int TITLE_FRAME_W = TILE_W * 7;
		private const int TITLE_FRAME_H = TILE_W * 1;
		private const int TITLE_FRAME_X = TITLE_REDV_X + (2 * TITLE_REDV_W);
		private const int TITLE_FRAME_Y = TITLE_CODENAME_Y;
		private const int TITLE_START_W = TILE_W * 6;
		private const int TITLE_START_H = TILE_W * 1;

		// world map graphics
		private const int MAPTILES_X = ASSETS_X;
		private const int MAPTILES_Y = TITLE_SIGNATURE_Y + TITLE_SIGNATURE_H;

		// game attributes
		private const int GAME_LIVES_DEFAULT = 1;
		private const int GAME_HEALTH_MAX = 3;
		private const int GAME_ENERGY_MAX = 7;
		private const int GAME_ENERGY_THRESHOLD_LOW = 3;
		private const int GAME_MOVE_SPEED = 5;
		private const int GAME_DODGE_DELAY = 500;
		private const int GAME_FIRE_DELAY = 20;
		private const int GAME_INVINCIBLE_DELAY = 5000;
		private const int GAME_DEATH_DELAY = 3000;
		private const int GAME_END_DELAY = 5000;
		private const int GAME_LOOT_DURATION = 8000;
		private const int GAME_PLAYER_DAMAGE = 1;
		private const int GAME_ENEMY_DAMAGE = 1;

		private const int POWERUP_HEALTH = 0;
		private const int POWERUP_LIVES = 2;

		private const int POWERUP_HEALTH_AMOUNT = 1;
		private const int POWERUP_LIVES_AMOUNT = 1;

		private const float ANIM_CUTSCENE_BACKGROUND_SPEED = 0.1f;

		public enum ProjectileType {
				LIGHTGUN,
				DEBRIS,
				BULLET,
				ENERGY
		};
		public static readonly Vector2[] GAME_PROJ_SPEED = {
				new Vector2(4.0f, 4.0f) * SPRITE_SCALE, 
				new Vector2(1.0f, 1.0f) * SPRITE_SCALE, 
				new Vector2(1.0f, 1.0f) * SPRITE_SCALE, 
				new Vector2(1.0f, 1.0f) * SPRITE_SCALE
		};
		public static readonly float[] GAME_PROJ_ROT_DELTA = {
				0.0f,
				0.0f,
				0.0f,
				0.0f
		};

		// game state
		private const int OPTION_RETRY = 0;
		private const int OPTION_QUIT = 1;

		/* Gameplay Variables */

		// program
		private static Vector2 mGameStartCoords;
		private static Vector2 mGameEndCoords;
		private behaviorAfterMotionPause mBehaviorAfterPause;

		// active game actors
		public List<LightGunGame.Bullet> mPlayerBullets = new List<LightGunGame.Bullet>();
		private static List<LightGunGame.Bullet> mEnemyBullets = new List<LightGunGame.Bullet>();
		private static List<LightGunGame.Monster> mEnemies = new List<LightGunGame.Monster>();
		private static List<TemporaryAnimatedSprite> mTemporarySprites = new List<TemporaryAnimatedSprite>();
		private static List<int> mPlayerMovementDirections = new List<int>();
		private List<Point>[] mSpawnQueue = new List<Point>[4];
		private int[,] mStageMap = new int[MAP_H, MAP_W];

		// player attributes
		private int mHealth;
		private int mLives;
		private int mEnergy;
		private int mWhichStage;
		private int mWhichWorld;
		private int mWhichGame;
		private int mStageTimer;
		private int mTotalTimer;
		private int mShotsSuccessful;
		private int mShotsFired;
		private int mScore;
		private int mGameOverOption;
		private SpecialPower mActiveSpecialPower;
		public Vector2 mPlayerPosition;
		public Rectangle mPlayerBoundingBox;

		// player state
		private SpriteEffects mMirrorPlayerSprite;
		private bool mHasPlayerQuit;
		
		// ui elements
		private static Texture2D mArcadeTexture;
		private static Color mScreenFlashColor;
		private float mCutsceneBackgroundPosition;

		// timers

		// midgame
		private static float mPlayerAnimationTimer;
		private static int mPlayerAnimationPhase;
		private static int mPlayerFireTimer;
		private static int mPlayerSpecialTimer;
		private static int mPlayerPowerTimer;
		private static int mPlayerInvincibleTimer;
		private static int mScreenFlashTimer;
		private static float mRespawnTimer;
		// postgame lose
		private static int mGameRestartTimer;
		private static int mGameEndTimer;
		// postgame win
		private static int mCutsceneTimer;
		private static int mCutscenePhase;
		// meta
		private static bool onStartMenu;
		private static bool onGameOver;
		private static bool onWorldComplete;
		private static bool onGameComplete;

		public static int TileSize
		{
			get
			{
				return TILE_W;
			}
		}

		public LightGunGame()
		{
			mArcadeTexture = Hikawa.SHelper.Content.Load<Texture2D>(
				Path.Combine(Const.AssetsPath, Const.MapsPath, Const.ArcadeSprites + ".png"));

			// Reload assets customised by the arcade game
			// ie. LooseSprites/Cursors
			Hikawa.SHelper.Content.AssetEditors.Add(new ArcadeAssetEditor());

			mShotsSuccessful = 0;
			mShotsFired = 0;
			mTotalTimer = 0;

			reset();
		}

		public void unload()
		{
			mHealth = GAME_HEALTH_MAX;
			mLives = GAME_LIVES_DEFAULT;
		}

		public void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (onStartMenu)
			{	// Progress through the start menu cutscene
				mCutscenePhase++;
			}
			else
			{
				if (mCutsceneTimer <= 0 && mPlayerPowerTimer <= 0 && mPlayerSpecialTimer <= 0 && mRespawnTimer <= 0 && mGameEndTimer <= 0 && mGameRestartTimer <= 0 && mPlayerFireTimer <= 1)
					// Fire lightgun trigger
					playerFire();
			}
		}

		public void leftClickHeld(int x, int y)
		{
			if (mCutsceneTimer <= 0 && mPlayerPowerTimer <= 0 && mPlayerSpecialTimer <= 0 && mRespawnTimer <= 0 && mGameEndTimer <= 0 && mGameRestartTimer <= 0 && mPlayerFireTimer <= 1)
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
			bool flag = false;
			if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k) && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, Keys.Left))
			{
				// Move left
				addPlayerMovementDirection(MOVE_LEFT);
				flag = true;
			}
			if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k) && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
			{
				// Move right
				addPlayerMovementDirection(MOVE_RIGHT);
				flag = true;
			}
			if (flag)
			{
				return;
			}
			
			if (debugCheats) {
				switch (k)
				{
					case Keys.D1:
						if (mHealth < GAME_HEALTH_MAX)
							Hikawa.SMonitor.Log("mHealth : " + mHealth + " -> " + ++mHealth, LogLevel.Debug);
						else
							Hikawa.SMonitor.Log("mHealth : " + mHealth + " == GAME_HEALTH_MAX", LogLevel.Debug);
						break;
					case Keys.D2:
						if (mEnergy < GAME_ENERGY_MAX)
							Hikawa.SMonitor.Log("mEnergy : " + mEnergy + " -> " + ++mEnergy, LogLevel.Debug);
						else
							Hikawa.SMonitor.Log("mEnergy : " + mEnergy + " == GAME_ENERGY_MAX", LogLevel.Debug);
						break;
					case Keys.D3:
						Hikawa.SMonitor.Log("mLives : " + mLives + " -> " + ++mLives, LogLevel.Debug);
						break;
					case Keys.OemOpenBrackets:
						Hikawa.SMonitor.Log("mWhichStage : " + mWhichStage + " -> " + (mWhichStage + 1), LogLevel.Debug);
						endCurrentStage();
						break;
					case Keys.OemCloseBrackets:
						Hikawa.SMonitor.Log("mWhichWorld : " + mWhichWorld + " -> " + (mWhichWorld + 1), LogLevel.Debug);
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
					if (mPlayerSpecialTimer <= 0 && mEnergy >= GAME_ENERGY_THRESHOLD_LOW)
					{
						mMirrorPlayerSprite = SpriteEffects.None;
						playerSpecialStart();
					}
					break;
				case Keys.Escape:
					// End minigame
					quitMinigame();
					break;
				case Keys.A:
					// Move left
					addPlayerMovementDirection(MOVE_LEFT);
					mPlayerAnimationTimer = 0;
					break;
				case Keys.D:
					// Move right
					addPlayerMovementDirection(MOVE_RIGHT);
					mPlayerAnimationTimer = 0;
					break;
			}
		}

		public void receiveKeyRelease(Keys k)
		{
			// Accept new input
			if (k != Keys.None)
			{
				Keys keys = Keys.Down;
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
				bool flag = false;
				if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k) && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, Keys.Left))
				{
					if (mPlayerMovementDirections.Contains(MOVE_RIGHT))
					{
						mPlayerMovementDirections.Remove(MOVE_RIGHT);
					}
					flag = true;
				}
				if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k) && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
				{
					if (mPlayerMovementDirections.Contains(MOVE_LEFT))
					{
						mPlayerMovementDirections.Remove(MOVE_LEFT);
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
				if (!mPlayerMovementDirections.Contains(MOVE_LEFT))
					return;
				mPlayerMovementDirections.Remove(MOVE_LEFT);
			}
			else if (k == Keys.D)
			{
				// Move right
				if (!mPlayerMovementDirections.Contains(MOVE_RIGHT))
					return;
				mPlayerMovementDirections.Remove(MOVE_RIGHT);
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
			// TODO : include GAME_X and GAME_Y into position calculations

			mGameStartCoords = new Vector2(
					(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width - GAME_W * SPRITE_SCALE) / 2,
					(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height - GAME_H * SPRITE_SCALE) / 2);
					
			mGameEndCoords = new Vector2(
				mGameStartCoords.X + GAME_X * SPRITE_SCALE + GAME_W * SPRITE_SCALE,
				mGameStartCoords.Y + GAME_Y * SPRITE_SCALE + GAME_H * SPRITE_SCALE);

			Hikawa.SMonitor.Log(
					"mGameStartCoords: " +
					"(" + Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width + " - " + GAME_W * SPRITE_SCALE + ") / 2,\n" +
					"(" + Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height + " - " + GAME_H * SPRITE_SCALE + ") / 2,\n" +
					"= " + mGameStartCoords.X + ", " + mGameStartCoords.Y,
					LogLevel.Debug);

			Hikawa.SMonitor.Log(
					"mGameEndCoords: " + mGameStartCoords.X + ", " + mGameStartCoords.Y,
					LogLevel.Debug);
		}

		private void quitMinigame()
		{
			if (Game1.currentLocation != null && Game1.currentLocation.Name.Equals((object)"Saloon") && Game1.timeOfDay >= 1700)
				Game1.changeMusicTrack("Saloon1");
			unload();
			Game1.currentMinigame = null;
		}

		private void reset()
		{
			changeScreenSize();

			mEnemyBullets.Clear();
			mEnemies.Clear();
			mTemporarySprites.Clear();

			mPlayerPosition = new Vector2(
					mGameStartCoords.X + GAME_X + (GAME_W * SPRITE_SCALE / 2),
					mGameStartCoords.Y + GAME_Y + (GAME_H * SPRITE_SCALE) - (PLAYER_FULL_H * SPRITE_SCALE)
			);

			// Player bounding box only includes the bottom 2/3rds of the sprite.
			mPlayerBoundingBox.X = (int)mPlayerPosition.X;
			mPlayerBoundingBox.Y = (int)mPlayerPosition.Y;
			mPlayerBoundingBox.Width = PLAYER_W * SPRITE_SCALE;
			mPlayerBoundingBox.Height = PLAYER_SPLIT_H * SPRITE_SCALE;

			mPlayerAnimationPhase = 0;
			mPlayerAnimationTimer = 0;
			mPlayerSpecialTimer = 0;
			mPlayerFireTimer = 0;

			mCutsceneTimer = 0;
			mCutscenePhase = 0;
			mCutsceneBackgroundPosition = 0f;

			// todo: reduce score with formula
			int reducedScore = 0;
			mScore = Math.Min(0, reducedScore);

			mHealth = GAME_HEALTH_MAX;
			mEnergy = 0;
			mActiveSpecialPower = SpecialPower.NONE;

			mWhichStage = 0;
			mWhichWorld = 0;

			mRespawnTimer = 0.0f;
			onGameOver = false;

			if (!debugSkipIntro)
				onStartMenu = true;
		}

		private void addPlayerMovementDirection(int direction)
		{
			if (mPlayerMovementDirections.Contains(direction))
				return;
			mPlayerMovementDirections.Add(direction);
		}

		private void playerPowerup(int which)
		{
			switch (which)
			{
				case POWERUP_HEALTH:
					mHealth = Math.Min(GAME_HEALTH_MAX, mHealth + POWERUP_HEALTH_AMOUNT);

					// todo spawn a new temporary sprite at the powerup object coordinates

					break;
			}
		}

		private void playerSpecialStart()
		{
			mPlayerAnimationPhase = 0;
			mPlayerSpecialTimer = 1;
		}
		
		private void playerSpecialEnd()
		{
			// Energy levels between 0 and the low-threshold will use a light special power.
			mActiveSpecialPower = mEnergy >= GAME_ENERGY_MAX ? SpecialPower.NORMAL : SpecialPower.MEGATON;

			mPlayerAnimationPhase = 0;
			mPlayerSpecialTimer = 0;
		}

		private void playerPowerEnd()
		{
			mActiveSpecialPower = SpecialPower.NONE;
			mPlayerAnimationPhase = 0;
			mPlayerPowerTimer = 0;
			mEnergy = 0;
		}

		private void playerFire()
		{
			// Position the source around the centre of the player
			Vector2 src = new Vector2(
					mPlayerPosition.X + (PLAYER_W / 2 * SPRITE_SCALE),
					mPlayerPosition.Y + (PLAYER_SPLIT_H / 2 * SPRITE_SCALE));

			// Position the target on the centre of the cursor
			Vector2 dest = new Vector2(
					Hikawa.SHelper.Input.GetCursorPosition().ScreenPixels.X + (TILE_W / 2 * SPRITE_SCALE),
					Hikawa.SHelper.Input.GetCursorPosition().ScreenPixels.Y + (TILE_W / 2 * SPRITE_SCALE));
			spawnBullets(src, dest, GAME_PLAYER_DAMAGE, ProjectileType.LIGHTGUN);
			mPlayerFireTimer = GAME_FIRE_DELAY;

			// Mirror player sprite to face target
			if (mPlayerMovementDirections.Count == 0)
				if (dest.X < mPlayerPosition.X)
					mMirrorPlayerSprite = SpriteEffects.FlipHorizontally;
				else
					mMirrorPlayerSprite = SpriteEffects.None;

			++mShotsFired;
		}

		private bool playerTakeDamage(int damage)
		{
			// todo: animate player

			mHealth = Math.Max(0, mHealth - damage);
			if (mHealth <= 0)
				return true;
			
			mScreenFlashColor = new Color(new Vector4(255, 0, 0, 0.25f));
			mScreenFlashTimer = 200;

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

			--mLives;
			mRespawnTimer = GAME_DEATH_DELAY;
			mSpawnQueue = new List<Point>[4];
			for (int i = 0; i < 4; ++i)
				mSpawnQueue[i] = new List<Point>();

			mTemporarySprites.Add(
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
					mGameStartCoords, 
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

			if (mLives >= 0)
				return;

			mTemporarySprites.Add(
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
					mGameStartCoords,
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
				endFunction = new TemporaryAnimatedSprite.endBehavior(playerGameOverCheck)
			});

			mRespawnTimer *= 3f;
		}

		private void playerGameOverCheck(int extra)
		{
			if (mLives >= 0)
			{
				playerRespawn();
				return;
			}
			onGameOver = true;
			mEnemies.Clear();
			quitMinigame();
			++Game1.currentLocation.currentEvent.CurrentCommand;
		}

		private void playerRespawn()
		{
			mPlayerInvincibleTimer = GAME_INVINCIBLE_DELAY;
			mHealth = GAME_HEALTH_MAX;
		}

		private void endCurrentStage()
		{
			mPlayerMovementDirections.Clear();

			// todo: set cutscenes, begin events, etc
		}

		private void endCurrentWorld()
		{
			// todo: set cutscenes, begin events, etc
		}

		// Begin the next stage within a world
		private void startNewStage()
		{
			++mWhichStage;
			mStageMap = getMap(mWhichStage);
		}

		// Begin the next world
		private void startNewWorld()
		{
			++mWhichWorld;
		}

		// New Game Plus
		private void startNewGame()
		{
			mGameRestartTimer = 2000;
			++mWhichGame;
		}

		private void spawnBullets(Vector2 spawn, Vector2 dest, int damage, ProjectileType type)
		{
			// Rotation
			float radiansBetween = Vector.RadiansBetween(dest, spawn);
			radiansBetween -= (float)(Math.PI / 2.0d);
			Hikawa.SMonitor.Log("RadiansBetween " + spawn.ToString() + ", " + dest.ToString() +
					" = " + string.Format("{0:0.00}", radiansBetween) + "rad",
					LogLevel.Debug);

			// Vector of motion
			Vector2 motion = Vector.PointAt(spawn, dest);
			motion.Normalize();

			// Spawn position
			Vector2 position = spawn + motion * (GAME_PROJ_SPEED[(int)type] * 5);

			// Add the bullet to the active lists for the respective spawner
			if (type == ProjectileType.LIGHTGUN)
				mPlayerBullets.Add(new Bullet(position, motion, radiansBetween, type));
			else
				mEnemyBullets.Add(new Bullet(position, motion, radiansBetween, type));
		}

		private void updateBullets(GameTime time)
		{
			// Handle player bullets
			for (int i = mPlayerBullets.Count - 1; i >= 0; --i)
			{
				// Damage monsters
				for (int j = mEnemies.Count - 1; j >= 0; --j)
				{
					if (mEnemies[j].position.Intersects(
							new Rectangle(
									(int)mPlayerBullets[i].position.X, 
									(int)mPlayerBullets[i].position.Y,
									TILE_W * SPRITE_SCALE, 
									TILE_W * SPRITE_SCALE)))
					{
						if (!mEnemies[j].takeDamage(GAME_PLAYER_DAMAGE))
						{
							mEnemies[j].die();
							mEnemies.RemoveAt(j);
							mPlayerBullets.RemoveAt(i);
						}
					}
					break;
				}

				// Update bullet positions
				mPlayerBullets[i].position += mPlayerBullets[i].motion * GAME_PROJ_SPEED[(int)mPlayerBullets[i].type];

				// Remove offscreen bullets
				if (mPlayerBullets[i].position.X <= mGameStartCoords.X
				|| mPlayerBullets[i].position.Y <= mGameStartCoords.Y
				|| mPlayerBullets[i].position.X >= mGameEndCoords.X
				|| mPlayerBullets[i].position.Y >= mGameEndCoords.Y)
				{
					mPlayerBullets.RemoveAt(i);
				}
			}

			// Handle enemy bullets
			for (int i = mEnemyBullets.Count - 1; i >= 0; --i)
			{
				// Damage the player
				if (mPlayerInvincibleTimer <= 0 && mRespawnTimer <= 0.0)
				{
					if (mPlayerInvincibleTimer <= 0 && mRespawnTimer <= 0.0
					&& mPlayerBoundingBox.Intersects(
							new Rectangle(
									(int)mEnemyBullets[i].position.X,
									(int)mEnemyBullets[i].position.Y, 
									TILE_W * SPRITE_SCALE,
									TILE_W * SPRITE_SCALE))) {
						if (!playerTakeDamage(GAME_ENEMY_DAMAGE))
						{
							playerDie();
							mEnemyBullets.RemoveAt(i);
						}
					}
					break;
				}

				// Update bullet positions
				mEnemyBullets[i].rotationCur += GAME_PROJ_ROT_DELTA[i];
				mEnemyBullets[i].position += mEnemyBullets[i].motion * GAME_PROJ_SPEED[(int)mEnemyBullets[i].type];

				// Remove offscreen bullets
				if (mEnemyBullets[i].position.X <= mGameStartCoords.X
				|| mEnemyBullets[i].position.Y <= mGameStartCoords.Y
				|| mEnemyBullets[i].position.X >= mGameEndCoords.X
				|| mEnemyBullets[i].position.Y >= mGameEndCoords.Y)
				{
					mEnemyBullets.RemoveAt(i);
				}
			}
		}

		public int[,] getMap(int wave)
		{
			int[,] map = new int[MAP_H, MAP_W];
			for (int i = 0; i < MAP_H; ++i)
			{
				for (int j = 0; j < MAP_W; ++j)
					map[i, j] = i != 0 && i != 15 && (j != 0 && j != 15) || (i > 6 && i < 10 || j > 6 && j < 10) ? (i == 0 || i == 15 || (j == 0 || j == 15) ? (Game1.random.NextDouble() < 0.15 ? 1 : 0) : (i == 1 || i == 14 || (j == 1 || j == 14) ? 2 : (Game1.random.NextDouble() < 0.1 ? 4 : 3))) : 5;
			}
			switch (wave)
			{
				case -1:
					for (int i = 0; i < MAP_H; ++i)
					{
						for (int j = 0; j < MAP_W; ++j)
						{
							if (map[i, j] == 0 || map[i, j] == 1 || (map[i, j] == 2 || map[i, j] == 5))
								map[i, j] = 3;
						}
					}
					break;
				case 0:
					for (int i = 0; i < MAP_H; ++i)
					{
						for (int j = 0; j < MAP_W; ++j)
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
			if (mHasPlayerQuit)
			{
				quitMinigame();
				return true;
			}

			// Run through the restart game timer
			if (mGameRestartTimer > 0)
			{
				elapsedGameTime = time.ElapsedGameTime;
				mGameRestartTimer -= time.ElapsedGameTime.Milliseconds;
				if (mGameRestartTimer <= 0)
				{
					unload();
					Game1.currentMinigame = new LightGunGame();
				}
			}

			// Run through screen effects
			if (mScreenFlashTimer > 0)
			{
				elapsedGameTime = time.ElapsedGameTime;
				mScreenFlashTimer -= time.ElapsedGameTime.Milliseconds;
			}

			// Run down player invincibility
			if (mPlayerInvincibleTimer > 0)
			{
				elapsedGameTime = time.ElapsedGameTime;
				mPlayerInvincibleTimer -= time.ElapsedGameTime.Milliseconds;
			}

			// Run down player lightgun animation
			if (mPlayerFireTimer > 0)
			{
				--mPlayerFireTimer;
			}

			// Handle player special trigger
			if (mPlayerSpecialTimer > 0)
			{
				elapsedGameTime = time.ElapsedGameTime;
				mPlayerSpecialTimer += time.ElapsedGameTime.Milliseconds;

				// Progress through player special trigger animation

				// Events 
				if (mPlayerAnimationPhase == PLAYER_SPECIAL_FRAMES)
				{
					// Transfer into the active special power
					playerSpecialEnd();
				}

				// Timers
				if (mPlayerAnimationPhase == 0)
				{
					if (mPlayerSpecialTimer >= TIMER_SPECIAL_PHASE_1)
						++mPlayerAnimationPhase;
				}
				else if (mPlayerAnimationPhase == 1)
				{
					if (mPlayerSpecialTimer >= TIMER_SPECIAL_PHASE_2)
						++mPlayerAnimationPhase;
				}
			}
			// Handle player power animations and effects
			else if (mActiveSpecialPower != SpecialPower.NONE)
			{
				elapsedGameTime = time.ElapsedGameTime;
				mPlayerPowerTimer += time.ElapsedGameTime.Milliseconds;

				// Progress through player power

				// Events
				if (mPlayerAnimationPhase == PLAYER_POWER_FRAMES)
				{
					// Return to usual game flow
					playerPowerEnd();
				}
				
				// Timers
				if (mPlayerAnimationPhase == 0)
				{
					// Frequently spawn bullets raining down on the field
					if (mPlayerPowerTimer % 5 == 0)
					{
						
					}
					// Progress to the next phase
					if (mPlayerPowerTimer >= TIMER_POWER_PHASE_1)
						++mPlayerAnimationPhase;
				}
				else if (mPlayerAnimationPhase == 1)
				{
					// Progress to the next phase
					if (mPlayerPowerTimer >= TIMER_POWER_PHASE_2)
						++mPlayerAnimationPhase;
				}
				else if (mPlayerAnimationPhase == 2)
				{
					// End the power
					if (mPlayerPowerTimer >= TIMER_POWER_PHASE_3)
						++mPlayerAnimationPhase;
				}
			}

			// Handle other game sprites
			for (int i = mTemporarySprites.Count - 1; i >= 0; --i)
			{
				if (mTemporarySprites[i].update(time))
					mTemporarySprites.RemoveAt(i);
			}
			

			/* Game Activity */


			// While the game offers player agency
			if (!onStartMenu && mCutsceneTimer <= 0)
			{
				// Update bullet data
				updateBullets(time);

				if (mPlayerSpecialTimer <= 0 && mPlayerPowerTimer <= 0)
				{
					// While the game is engaging the player
					if (mCutsceneTimer <= 0)
					{
						// Run down the death timer
						if (mRespawnTimer > 0.0)
						{
							elapsedGameTime = time.ElapsedGameTime;
							mRespawnTimer -= elapsedGameTime.Milliseconds;
						}

						// Handle player movement
						if (mPlayerMovementDirections.Count > 0)
						{
							switch (mPlayerMovementDirections.ElementAt<int>(0))
							{
								case MOVE_RIGHT:
									mMirrorPlayerSprite = SpriteEffects.None;
									if (mPlayerPosition.X + mPlayerBoundingBox.Width < mGameEndCoords.X)
										mPlayerPosition.X += GAME_MOVE_SPEED;
									else
										mPlayerPosition.X = mGameEndCoords.X - mPlayerBoundingBox.Width;
									break;
								case MOVE_LEFT:
									mMirrorPlayerSprite = SpriteEffects.FlipHorizontally;
									if (mPlayerPosition.X > mGameStartCoords.X)
										mPlayerPosition.X -= GAME_MOVE_SPEED;
									else
										mPlayerPosition.X = mGameStartCoords.X;
									break;
							}
						}

						elapsedGameTime = time.ElapsedGameTime;
						mPlayerAnimationTimer += elapsedGameTime.Milliseconds;
						mPlayerAnimationTimer %= (PLAYER_RUN_FRAMES) * PLAYER_ANIM_TIMESCALE;

						mPlayerBoundingBox.X = (int)mPlayerPosition.X;
					}

					// Handle enemy behaviours
					if (mHealth > 0)
					{

						// todo

					}
				}
			}

			// Sit on the start game screen until prompted elsewhere
			if (onGameOver || onStartMenu)
			{
				elapsedGameTime = time.ElapsedGameTime;

				// Cutscene phase 0 on the title screen progresses by click or by screen event
				if (mCutscenePhase >= 1)
				{
					mCutsceneTimer += elapsedGameTime.Milliseconds;
				}

				// Progress through a small intro sequence

				// Events
				if (mCutscenePhase == 0)
				{
					mCutsceneBackgroundPosition += GAME_W / UI_ANIM_TIMESCALE;
					if (mCutsceneBackgroundPosition >= GAME_W * 4 / 3)
					{
						++mCutscenePhase;
					}
				}
				// Timers
				else if (mCutscenePhase == 1)
				{
					if (mCutsceneTimer >= TIMER_TITLE_PHASE_1)
						++mCutscenePhase;
				}
				else if (mCutscenePhase == 2)
				{
					if (mCutsceneTimer < TIMER_TITLE_PHASE_1)
						mCutsceneTimer = TIMER_TITLE_PHASE_1;
					else if (mCutsceneTimer >= TIMER_TITLE_PHASE_5)
						++mCutscenePhase;
				}
				else if (mCutscenePhase == 3)
				{
					if (mCutsceneTimer < TIMER_TITLE_PHASE_5)
						mCutsceneTimer = TIMER_TITLE_PHASE_5;
				}
				else if (mCutscenePhase == 4)
				{
					// End the cutscene and begin the game after the user clicks past the end of intro cutscene (phase 3)
					mCutsceneTimer = 0;
					mCutscenePhase = 0;
					onStartMenu = false;
				}

			}

			// Run through the end of world cutscene
			else if (onWorldComplete)
			{
				elapsedGameTime = time.ElapsedGameTime;
				double delta = elapsedGameTime.Milliseconds * (double)ANIM_CUTSCENE_BACKGROUND_SPEED;
				mCutsceneBackgroundPosition = (float)(mCutsceneBackgroundPosition + delta) % 96f;
			}

			return false;
		}

		public void draw(SpriteBatch b)
		{
			b.Begin(
				SpriteSortMode.FrontToBack,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				(DepthStencilState)null,
				(RasterizerState)null);
				
			// Render screen flash effects
			if (mScreenFlashTimer > 0)
			{
				b.Draw(
						Game1.staminaRect,
						new Rectangle(
								(int)mGameStartCoords.X,
								(int)mGameStartCoords.Y,
								(int)(mGameEndCoords.X - mGameStartCoords.X),
								(int)(mGameEndCoords.Y - mGameStartCoords.Y)),
						new Rectangle?(
								Game1.staminaRect.Bounds),
						mScreenFlashColor,
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
			if (!onStartMenu && mRespawnTimer <= 0.0)
			{
				/// debug makito
				/// very important
				b.Draw(
						mArcadeTexture, 
						new Rectangle(
								(int)(mGameEndCoords.X - mGameStartCoords.X),
								(int)(mGameEndCoords.Y - mGameStartCoords.Y),
								TileSize * SPRITE_SCALE,
								TileSize * SPRITE_SCALE),
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
						mMirrorPlayerSprite,
						1.0f);
				/// very important
				/// debug makito

				// Draw the player
				if (mPlayerInvincibleTimer <= 0 && mPlayerInvincibleTimer / 100 % 2 == 0)
				{
					Rectangle[] destRects = new Rectangle[3];
					Rectangle[] srcRects = new Rectangle[3];

					// Draw full body action sprites
					if (mPlayerSpecialTimer > 0)
					{	// Activated special power
						destRects[0] = new Rectangle(
								(int)mPlayerPosition.X,
								(int)mPlayerPosition.Y,
								mPlayerBoundingBox.Width,
								PLAYER_FULL_H * SPRITE_SCALE);
						srcRects[0] = new Rectangle(
								PLAYER_SPECIAL_X + (PLAYER_W * mPlayerAnimationPhase),
								PLAYER_FULL_Y,
								PLAYER_W,
								PLAYER_FULL_H);
					}
					else if (mActiveSpecialPower != SpecialPower.NONE)
					{	// Player used a special power
						// Draw power effects by type
						if (mActiveSpecialPower == SpecialPower.NORMAL)
						{   // Player used Venus Love Shower / THRESHOLD_LOW === POWER_NORMAL
							// . . . .
						}
						// Draw full body sprite by phase
						destRects[0] = new Rectangle(
								(int)mPlayerPosition.X,
								(int)mPlayerPosition.Y,
								mPlayerBoundingBox.Width,
								PLAYER_FULL_H * SPRITE_SCALE);
						srcRects[0] = new Rectangle(
								PLAYER_POWER_X + (PLAYER_W * POWER_ANIMS[(int)mActiveSpecialPower]) + (PLAYER_W * mPlayerAnimationPhase),
								PLAYER_FULL_Y,
								PLAYER_W,
								PLAYER_FULL_H);
					}
					else if (mRespawnTimer > 0)
					{   // Player dying
						// . .. . .
					}
					// Draw full body idle sprite
					else if (mPlayerFireTimer <= 0 && mPlayerMovementDirections.Count == 0)
					{   // Standing idle
						destRects[0] = new Rectangle(
								(int)mPlayerPosition.X,
								(int)mPlayerPosition.Y,
								mPlayerBoundingBox.Width,
								PLAYER_FULL_H * SPRITE_SCALE);
						srcRects[0] = new Rectangle(
								PLAYER_X,
								PLAYER_FULL_Y,
								PLAYER_W,
								PLAYER_FULL_H);
					}
					// Draw appropriate sprite upper body
					else
					{
						if (mPlayerFireTimer > 0)
						{
							if (mPlayerMovementDirections.Count > 0)
							{   // Firing running torso
								int frame = (int)(mPlayerAnimationTimer / PLAYER_ANIM_TIMESCALE);
								destRects[0] = new Rectangle(
										(int)mPlayerPosition.X,
										(int)mPlayerPosition.Y,
										mPlayerBoundingBox.Width,
										mPlayerBoundingBox.Height);
								srcRects[0] = new Rectangle(
										PLAYER_BODY_FIRE_X + (PLAYER_W * frame),
										PLAYER_SPLIT_Y,
										PLAYER_W,
										PLAYER_SPLIT_H);
							}
							else
							{	// Firing standing torso
								destRects[0] = new Rectangle(
										(int)mPlayerPosition.X,
										(int)mPlayerPosition.Y,
										mPlayerBoundingBox.Width,
										mPlayerBoundingBox.Height);
								srcRects[0] = new Rectangle(
										PLAYER_BODY_FIRE_X,
										PLAYER_SPLIT_Y,
										PLAYER_W,
										PLAYER_SPLIT_H);
							}
							if (mPlayerBullets.Count > 0)
							{   // Firing arms
								float radiansBetween = Vector.RadiansBetween(
										Hikawa.SHelper.Input.GetCursorPosition().AbsolutePixels, mPlayerPosition);
								radiansBetween -= (float)(Math.PI / 2.0d);
								int frame = (int)Math.Min(PLAYER_BULLET_FRAMES, Math.Abs(radiansBetween));
								//Hikawa.SMonitor.Log("Rad: " + radiansBetween + "   |   Frame: " + frame, LogLevel.Debug);
								destRects[2] = new Rectangle(
										(int)mPlayerPosition.X,
										(int)mPlayerPosition.Y,
										mPlayerBoundingBox.Width,
										mPlayerBoundingBox.Height);
								srcRects[2] = new Rectangle(
										PLAYER_ARMS_FIRE_X + PLAYER_W * frame,
										PLAYER_SPLIT_Y,
										PLAYER_W,
										PLAYER_SPLIT_H);
							}
						}
						else
						{   // Running torso
							int frame = (int)(mPlayerAnimationTimer / PLAYER_ANIM_TIMESCALE);
							destRects[0] = new Rectangle(
									(int)mPlayerPosition.X,
									(int)mPlayerPosition.Y,
									mPlayerBoundingBox.Width,
									mPlayerBoundingBox.Height);
							srcRects[0] = new Rectangle(
									PLAYER_BODY_RUN_X + (PLAYER_W * frame),
									PLAYER_SPLIT_Y,
									PLAYER_W,
									PLAYER_SPLIT_H);
						}
					}

					// Draw appropriate sprite legs
					if (mPlayerMovementDirections.Count > 0)
					{   // Running
						int frame = (int)(mPlayerAnimationTimer / PLAYER_ANIM_TIMESCALE);
						destRects[1] = new Rectangle(
								(int)mPlayerPosition.X,
								(int)mPlayerPosition.Y + (PLAYER_FULL_H - PLAYER_SPLIT_H) * SPRITE_SCALE,
								mPlayerBoundingBox.Width,
								mPlayerBoundingBox.Height);
						srcRects[1] = new Rectangle(
								PLAYER_LEGS_RUN_X + (PLAYER_W * frame),
								PLAYER_SPLIT_Y,
								PLAYER_W,
								PLAYER_SPLIT_H);
					}
					else if (mPlayerFireTimer > 0)
					{   // Standing and firing
						destRects[1] = new Rectangle(
								(int)mPlayerPosition.X,
								(int)mPlayerPosition.Y + (PLAYER_FULL_H - PLAYER_SPLIT_H) * SPRITE_SCALE,
								mPlayerBoundingBox.Width,
								mPlayerBoundingBox.Height);
						srcRects[1] = new Rectangle(
								PLAYER_X,
								PLAYER_SPLIT_Y,
								PLAYER_W,
								PLAYER_SPLIT_H);
					}

					// Draw the player from each component sprite
					for(int i = 2; i >= 0; --i)
					{
						if (srcRects[i] != Rectangle.Empty)
						{
							b.Draw(
									mArcadeTexture,
									destRects[i],
									srcRects[i],
									Color.White,
									0.0f,
									Vector2.Zero,
									mMirrorPlayerSprite,
									(float)(mPlayerPosition.Y / 10000.0 + i / 1000.0 + 1.0 / 1000.0));
						}
					}
				}

				// Draw player bullets
				foreach (Bullet playerBullet in mPlayerBullets)
				{
					b.Draw(
							mArcadeTexture,
							new Vector2(
									playerBullet.position.X,
									playerBullet.position.Y),
							new Rectangle(
									PROJECTILES_X + (TileSize * (int)playerBullet.type),
									PROJECTILES_Y,
									TileSize,
									TileSize),
							Color.White,
							playerBullet.rotation,
							new Vector2(
									TileSize / 2,
									TileSize / 2),
							SPRITE_SCALE,
							SpriteEffects.None,
							0.9f);
				}

				// Draw enemy bullets
				foreach (Bullet enemyBullet in mEnemyBullets)
				{
					b.Draw(
							mArcadeTexture,
							new Vector2(
									enemyBullet.position.X,
									enemyBullet.position.Y),
							new Rectangle(
									PROJECTILES_X + (TileSize * (int)enemyBullet.type),
									PROJECTILES_Y,
									TileSize,
									TileSize),
							Color.White,
							enemyBullet.rotation,
							Vector2.Zero,
							SPRITE_SCALE,
							SpriteEffects.None,
							0.9f);
				}

				// Draw all the stuff
				foreach (TemporaryAnimatedSprite temporarySprite in mTemporarySprites)
				{
					temporarySprite.draw(
							b,
							true,
							0,
							0,
							1f);
				}

				// Draw enemies
				foreach (Monster monster in mEnemies)
				{
					monster.draw(b);
				}

				// Draw the background
				switch (mActiveSpecialPower)
				{
					case SpecialPower.NORMAL:
					case SpecialPower.MEGATON:
						if (mPlayerAnimationPhase == 1)
						{
							// Draw a black overlay
							b.Draw(
								mArcadeTexture,
								new Rectangle(
										(int)mGameStartCoords.X,
										(int)mGameStartCoords.Y,
										(int)(mGameEndCoords.X - mGameStartCoords.X),
										(int)(mGameEndCoords.Y - mGameStartCoords.Y)),
								new Rectangle(
										TITLE_FRAME_X,
										TITLE_FRAME_Y,
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
							goto case SpecialPower.NONE;
						}
						break;
					case SpecialPower.NONE:
					case SpecialPower.SULPHUR:
					case SpecialPower.INCENSE:
						// Draw the game map
						b.Draw(
							mArcadeTexture,
							new Rectangle(
									(int)mGameStartCoords.X,
									(int)mGameStartCoords.Y,
									(int)(mGameEndCoords.X - mGameStartCoords.X),
									(int)(mGameEndCoords.Y - mGameStartCoords.Y)),
							new Rectangle(
									TITLE_FRAME_X,
									TITLE_FRAME_Y,
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
			if (onStartMenu)
			{
				// Render each phase of the intro

				// Black backdrop
				b.Draw(
						mArcadeTexture,
						new Rectangle(
								0,
								0,
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width,
								Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height),
						new Rectangle(
								TITLE_FRAME_X,
								TITLE_FRAME_Y,
								TileSize,
								TileSize),
						Color.Black,
						0.0f,
						Vector2.Zero,
						SpriteEffects.None,
						0.0f);

				if (mCutscenePhase == 0)
				{
					// Draw the white title banner silhouetted on black

					// White V
					b.Draw(
							mArcadeTexture,
							new Rectangle(
									Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 3,
									Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 2,
									TITLE_REDV_W * SPRITE_SCALE,
									TITLE_REDV_H * SPRITE_SCALE),
							new Rectangle(
									TITLE_REDV_X + TITLE_REDV_W,
									TITLE_REDV_Y,
									TITLE_REDV_W,
									TITLE_REDV_H),
							Color.White,
							0.0f,
							new Vector2(
									TITLE_REDV_W / 2,
									TITLE_REDV_H / 2),
							SpriteEffects.None,
							0.5f);

					// Pan a vertical letterbox across the screen to simulate light gleam

					// Black frame
					b.Draw(
							mArcadeTexture,
							new Rectangle(
									-(Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width * (TITLE_FRAME_W / 3 * 2))
											+ ((int)mCutsceneBackgroundPosition),
									0,
									Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width * TITLE_FRAME_W,
									Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height),
							new Rectangle(
									TITLE_FRAME_X,
									TITLE_FRAME_Y,
									TITLE_FRAME_W,
									TITLE_FRAME_H),
							Color.White,
							0.0f,
							Vector2.Zero,
							SpriteEffects.None,
							1.0f);
				}

				if (mCutscenePhase >= 1)
				{
					// Draw the coloured title banner on black

					// Game title banner
					b.Draw(
							mArcadeTexture,
							new Rectangle(
									Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 3,
									Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 2,
									TITLE_REDV_W * SPRITE_SCALE,
									TITLE_REDV_H * SPRITE_SCALE),
							new Rectangle(
									TITLE_REDV_X,
									TITLE_REDV_Y,
									TITLE_REDV_W,
									TITLE_REDV_H),
							Color.White,
							0.0f,
							new Vector2(
									TITLE_REDV_W / 2,
									TITLE_REDV_H / 2),
							SpriteEffects.None,
							0.1f);
				}

				if (mCutscenePhase >= 2)
				{
					// Draw the coloured title banner with all title screen text

					if (mCutsceneTimer >= TIMER_TITLE_PHASE_2)
					{
						// "Codename"
						b.Draw(
								mArcadeTexture,
								new Rectangle(
										Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 2,
										Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 1,
										TITLE_TEXT_W * SPRITE_SCALE,
										TITLE_CODENAME_H * SPRITE_SCALE),
								new Rectangle(
										TITLE_TEXT_X,
										TITLE_CODENAME_Y,
										TITLE_TEXT_W,
										TITLE_CODENAME_H),
								Color.White,
								0.0f,
								new Vector2(
										TITLE_TEXT_W / 2,
										TITLE_CODENAME_H / 2),
								SpriteEffects.None,
								1.0f);
					}

					if (mCutsceneTimer >= TIMER_TITLE_PHASE_3)
					{
						// "Sailor"
						b.Draw(
								mArcadeTexture,
								new Rectangle(
										Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 2,
										Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 2,
										TITLE_TEXT_W * SPRITE_SCALE,
										TITLE_SAILOR_H * SPRITE_SCALE),
								new Rectangle(
										TITLE_TEXT_X,
										TITLE_SAILOR_Y,
										TITLE_TEXT_W,
										TITLE_SAILOR_H),
								Color.White,
								0.0f,
								new Vector2(
										TITLE_TEXT_W / 2,
										TITLE_SAILOR_H / 2),
								SpriteEffects.None,
								0.9f);
					}

					if (mCutsceneTimer >= TIMER_TITLE_PHASE_4)
					{
						// "Arcade Game"
						b.Draw(
								mArcadeTexture,
								new Rectangle(
										Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 5 * 2,
										Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 2,
										TITLE_TEXT_W * SPRITE_SCALE,
										TITLE_ARCADE_H * SPRITE_SCALE),
								new Rectangle(
										TITLE_TEXT_X,
										TITLE_ARCADE_Y,
										TITLE_TEXT_W,
										TITLE_ARCADE_H),
								Color.White,
								0.0f,
								new Vector2(
										TITLE_TEXT_W / 2,
										TITLE_ARCADE_H / 2),
								SpriteEffects.None,
								0.8f);
					}

				}

				if (mCutscenePhase >= 3)
				{
					// Display flashing 'fire to start' text and signature text

					if (mCutsceneTimer >= TIMER_TITLE_PHASE_5)
					{
						// "Blueberry 1991-1996"
						b.Draw(
							mArcadeTexture,
							new Rectangle(
									Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 2,
									Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 10 * 9,
									TITLE_SIGNATURE_W * SPRITE_SCALE,
									TITLE_SIGNATURE_H * SPRITE_SCALE),
							new Rectangle(
									TITLE_TEXT_X,
									TITLE_SIGNATURE_Y,
									TITLE_SIGNATURE_W,
									TITLE_SIGNATURE_H),
							Color.White,
							0.0f,
							new Vector2(
									TITLE_SIGNATURE_W / 2,
									TITLE_SIGNATURE_H / 2),
							SpriteEffects.None,
							1.0f);
					}

					if (mCutsceneTimer >= TIMER_TITLE_PHASE_6 && (mCutsceneTimer / 500) % 2 == 0)
					{
						// "Fire to start"
						b.Draw(
								mArcadeTexture,
								new Rectangle(
										Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Width / 2,
										Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Height / 5 * 4,
										TITLE_START_W * SPRITE_SCALE,
										TITLE_START_H * SPRITE_SCALE),
								new Rectangle(
										0,
										TileSize,
										TITLE_START_W,
										TITLE_START_H),
								Color.White,
								0.0f,
								new Vector2(
										TITLE_START_W / 2,
										TITLE_START_H / 2),
								SpriteEffects.None,
								1.0f);
					}
				}
			}

			// Display Game Over menu options
			else if (onGameOver)
			{
				b.Draw(
					Game1.staminaRect,
					new Rectangle(
						(int)mGameStartCoords.X,
						(int)mGameStartCoords.Y,
						16 * TileSize,
						16 * TileSize),
					new Rectangle?(
						Game1.staminaRect.Bounds),
					Color.Black,
					0.0f,
					Vector2.Zero,
					SpriteEffects.None,
					0.0001f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					mGameStartCoords + new Vector2(6f, 7f) * TileSize,
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					mGameStartCoords + new Vector2(6f, 7f) * TileSize + new Vector2(-1f, 0.0f),
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				b.DrawString(
					Game1.dialogueFont,
					Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11914"),
					mGameStartCoords + new Vector2(6f, 7f) * TileSize + new Vector2(1f, 0.0f),
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);

				string text1 = Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11917");
				if (mGameOverOption == 0)
					text1 = "> " + text1;

				string text2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:cs.11919");
				if (mGameOverOption == 1)
					text2 = "> " + text2;

				if (mGameRestartTimer <= 0 || mGameRestartTimer / 500 % 2 == 0)
				{
					b.DrawString(
						Game1.smallFont,
						text1,
						mGameStartCoords + new Vector2(6f, 9f) * TileSize,
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
					mGameStartCoords + new Vector2(6f, 9f) * TileSize + new Vector2(0.0f, (2 / 3)),
					Color.White,
					0.0f,
					Vector2.Zero,
					1f,
					SpriteEffects.None,
					1f);
			}

			// Show cutscene between worlds
			else if (onWorldComplete)
			{
				// todo: display world stats per stage
				// todo: display total time
			}

			b.End();
		}
		
		public delegate void behaviorAfterMotionPause();

		public class Powerup
		{
			public int which;
			public Point position;
			public int duration;
			public float yOffset;

			public Powerup(int which, Point position, int duration)
			{
				this.which = which;
				this.position = position;
				this.duration = duration;
			}

			public void draw(SpriteBatch b)
			{
				if (duration <= 2000 && duration / 200 % 2 != 0)
					return;
				b.Draw(
						mArcadeTexture,
						mGameStartCoords + new Vector2(position.X, position.Y + yOffset),
						new Rectangle(
								TILE_W,
								0, 
								16, 
								16),
						Color.White,
						0.0f,
						Vector2.Zero,
						SPRITE_SCALE,
						SpriteEffects.None,
						(float)(position.Y / 10000.0 + 1.0 / 1000.0));
			}
		}
		
		public class Bullet
		{
			public Vector2 position;        // Current position on screen
			public Vector2 motion;          // Line of motion between spawn and dest
			public Vector2 motionCur;       // Current position along vector of motion between spawn and dest
			public float rotation;          // Angle of vector between spawn and dest
			public float rotationCur;       // Angle of vector between spawn and dest
			public ProjectileType type;		// Which projectile, ie. light/heavy/bullet/lightgun

			public Bullet(Vector2 position, Vector2 motion, float rotation, ProjectileType type)
			{
				this.position = position;
				this.motion = motion;
				this.rotation = rotation;
				this.type = type;
				
				this.motion = motion;

				motionCur = motion;
				rotationCur = rotation;
			}
		}
		
		public class Monster
		{
			private Color tint = Color.White;
			private Color flashColor = Color.Red;
			public int health;
			public int type;
			public Rectangle position;
			public float flashColorTimer;
			public int ticksSinceLastMovement;

			public Monster(int which, int health, int speed, Point position)
			{
				type = which;
				this.health = health;
				this.position = new Rectangle(position.X, position.Y, TileSize, TileSize);
			}

			public Monster(int which, Point position)
			{
				type = which;
				this.position = new Rectangle(position.X, position.Y, TileSize, TileSize);
				switch (type) {
					// todo: enemy spawn parameters
				}
			}

			public virtual void draw(SpriteBatch b)
			{
			}

			public virtual bool takeDamage(int damage)
			{
				health = Math.Max(0, health - damage);
				if (health <= 0)
					return false;
				flashColor = Color.Red;
				flashColorTimer = 100f;
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
