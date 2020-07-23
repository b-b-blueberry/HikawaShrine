using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Objects;
using Object = StardewValley.Object;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using xTile.Dimensions;
using xTile.ObjectModel;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

using SpaceCore.Events;

using Hikawa.GameObjects;
using Hikawa.GameObjects.Menus;

namespace Hikawa
{
	public class ModEntry : Mod
	{
		internal Config Config;
		internal ITranslationHelper i18n => Helper.Translation;
		
		// Mod objects
		internal static ModEntry Instance;
		internal ModData SaveData;
		internal IJsonAssetsApi JaApi;
		private readonly OverlayEffectControl _overlayEffectControl = new OverlayEffectControl();

		// Mini-Sit
		private bool _isPlayerAgencySuppressed;
		private bool _isPlayerSittingDown;
		private readonly int[] _playerSittingFrames = {62, 117, 54, 117};
		private Vector2 _playerLastStandingLocation;

		// Critters
		private bool _shouldCrowsSpawnToday;
		private bool _whatAboutCatsCanTheySpawnToday;

		// Animations
		private int _animationExtraInt;
		private float _animationExtraFloat;
		private int _animationTimer;
		private int _animationStage;
		private bool _animationFlag;
		private Vector2 _animationTarget;

		// Mission
		private bool _isInterloper;

		// Others
		internal int CommonTextureScale = 4;
		internal int InitialBuffIconIndex = 24;
		internal Vector2 DummyChestCoords = new Vector2(38, 32);
		internal Vector2 DummyChestMissionOffset = new Vector2(1, 1);
		private bool _isPlayerGodMode;
		private bool _isPlayerBuddhaMode;
		private int _playerHealthToMaintain;
		private int _dizzyStack;

		internal enum Buffs
		{
			None,
			Shivers,
			Uneasy,
			Comfort,
			Warmth,
			Hunger,
			Sunlight,
			Weightless,
			Wind,
			Confidence,
			Rain,
			Water,
			Love,
			Count
		}


		// SPRITE TESTING
		private Texture2D _texture;
		private static readonly int X = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Center.X;
		private static readonly int Y = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Center.Y;
		private static float _yOffset;
		private static readonly Rectangle SourceRectGlare = new Rectangle(
			96, 144, 112, 32);
		private static readonly List<Rectangle> SourceRects = new List<Rectangle>
		{
			// 0 4 3 1 2
			new Rectangle(64, 32, 128, 38),
			new Rectangle(160, 74, 48, 64),
			new Rectangle(112, 74, 48, 64),
			new Rectangle(0, 80, 64, 64),
			new Rectangle(64, 80, 48, 64),
		};
		private static readonly Rectangle DestRectGlare = new Rectangle(
			X, Y - Y / 3 * 2, SourceRectGlare.Width * 4, SourceRectGlare.Height * 4);
		private static readonly List<Rectangle> DestRects = new List<Rectangle>
		{
			// 0 4 3 1 2
			new Rectangle(X - 16 * 4, Y + Y / 4 - 16 * 4, 128 * 4, 38 * 4),
			new Rectangle(X + 32 * 4, Y + Y / 4, 48 * 4, 64 * 4),
			new Rectangle(X + 16 * 4, Y + Y / 4, 48 * 4, 64 * 4),
			new Rectangle(X - 24 * 4, Y + Y / 4, 64 * 4, 64 * 4),
			new Rectangle(X + 00 * 4, Y + Y / 4, 48 * 4, 64 * 4),
		};
		// SPRITE TESTING


		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<Config>();

			//helper.Content.AssetEditors.Add(new Editors.TestEditor(helper));
			helper.Content.AssetEditors.Add(new Editors.WorldEditor(helper));
			//helper.Content.AssetEditors.Add(new Editors.EventEditor(helper));
			helper.Content.AssetEditors.Add(new Editors.ArcadeEditor(helper));

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.GameLoop.DayEnding += OnDayEnding;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
			
			helper.Events.Player.Warped += OnWarped;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			
			SpaceEvents.ChooseNightlyFarmEvent += HikawaFarmEvents;
			SpaceEvents.AfterGiftGiven += HikawaGiftsGiven;
			
			if (Config.DebugMode)
			{
				// Add debugging commands
				AddConsoleCommands();

				// Add texture render tests
				//helper.Events.Display.Rendering += OnRendering;
				var textureName = Path.Combine(ModConsts.SpritesPath, $"{ModConsts.ExtraSpritesFile}.png");
				_texture = Instance.Helper.Content.Load<Texture2D>(textureName);
			}
		}

		private void AddConsoleCommands()
		{
			// TODO: METHOD: Find some way to set default warp location, intercept Warped maybe?

			const string cmd = "bb"; // Command prefix
			var commands = new Dictionary<string, string[]>
			{
				{ cmd + "arcade",
					new[] { cmd + "a", "Start arcade game:"
					                 + " use START, TITLE, RESET." }}, 
				{ cmd + "overlay",
					new[] { cmd + "ov", $"Manage screen overlays:"
					                  + $" use 0~{OverlayEffectControl.Effect.Count - 1}." }},
				{ cmd + "offer",
					new[] { cmd + "of", "Make a shrine offering:"
					                  + " use S, M, or L." }},
				{ cmd + "crows",
					new[] { cmd + "c", "Respawn twin crows at the shrine." }},
				{ cmd + "crows2",
					new[] { cmd + "c2", "Respawn perched crows at the shrine." }},
				{ cmd + "totem",
					new[] { cmd + "t", "Fire a totem warp to the Shrine." }},
				{ cmd + "house",
					new[] { cmd + "h", "Warp to Rei's house." }},
				{ cmd + "shrine",
					new[] { cmd + "s", "Warp to Hikawa Shrine." }},
				{ cmd + "entry",
					new[] { cmd + "e", "Warp to the shrine entrance." }},
				{ cmd + "vortex", 
					new[] { cmd + "v", "Warp to a Vortex map:"
					                 + " use 1~3." }},
				{ cmd + "buff", 
					new[] { cmd + "bf", "Add a Shrine buff:"
					                  + $" use 0~{Buffs.Count - 1}." }},
				{ cmd + "god", 
					new[] { cmd + "g", "Toggle God mode." }},
				{ cmd + "buddha", 
					new[] { cmd + "bd", "Toggle Buddha mode." }},
				{ cmd + "pain", 
					new[] { cmd + "p", "Take random damage. Able to kill player from full health." }},
				{ cmd + "chapter", 
					new[] { cmd + "ch", $"Set or reset story progress:"
					                  + $" use 0~{ModData.Chapter.End - 1}"
					                  + $" 0~{ModData.Progress.Complete - 1}." }},
			};

			foreach (var command in commands)
			{
				Action<string, string[]> callback = (s, p) => {};
				switch (command.Key)
				{
					case cmd + "arcade":
						callback = (s, p) =>
						{
							var action = p[0].ToLower();
							if (action == "start")
							{
								Game1.currentMinigame = new ArcadeGunGame();
								return;
							}

							if (Game1.currentMinigame != null
									 && Game1.currentMinigame is ArcadeGunGame
							         && Game1.currentMinigame.minigameId() == ModConsts.ArcadeMinigameId)
							{
								if (action == "title")
								{
									((ArcadeGunGame)Game1.currentMinigame).ResetAndReturnToTitle();
									return;
								}
								if (action == "reset")
								{
									((ArcadeGunGame)Game1.currentMinigame).ResetGame();
									return;
								}
							}
							Log.E("Not a valid arcade action.");
						};
						break;

					case cmd + "overlay":
						callback = (s, p) =>
						{
							if (p.Length < 1)
							{
								Log.D($"Current effect: {_overlayEffectControl.CurrentEffect()}");
							}
							else
							{
								try
								{
									_overlayEffectControl.Enable((OverlayEffectControl.Effect) int.Parse(p[0]));
									return;
								}
								catch (FormatException) {}
								_overlayEffectControl.Toggle();
							}
						};
						break;
						
					case cmd + "offer":
						callback = (s, p) =>
						{
							try
							{
								MakeShrineOffering(Game1.player, "offer" + p[0]?.ToUpper()[0]);
								return;
							}
							catch (FormatException) {}
							Log.E("Not a valid offering token.");
						};
						break;

					case cmd + "crows":
						callback = (s, p) =>
						{
							SpawnCrows(Game1.getLocationFromName(ModConsts.ShrineMapId));
						};
						break;

					case cmd + "crows2":
						callback = (s, p) =>
						{
							SpawnPerchedCrows(Game1.getLocationFromName(ModConsts.ShrineMapId));
						};
						break;

					case cmd + "totem":
						callback = (s, p) =>
						{
							StartWarpToShrine(new Object(
								JaApi.GetObjectId("Warp Totem: Shrine"), 1).getOne() as Object, Game1.currentLocation);
						};
						break;

					case cmd + "home":
						callback = (s, p) =>
						{
							WarpToDefault(ModConsts.HouseMapId);
						};
						break;

					case cmd + "shrine":
						callback = (s, p) =>
						{
							WarpToDefault(ModConsts.ShrineMapId);
						};
						break;
						
					case cmd + "entry":
						callback = (s, p) =>
						{
							WarpToDefault("Town");
						};
						break;
						
					case cmd + "vortex":
						callback = (s, p) =>
						{
							var which = p.Length > 0 ? p[0] : "1";
							WarpToDefault(ModConsts.VortexMapId + which);
						};
						break;
						
					case cmd + "buff":
						callback = (s, p) =>
						{
							if (p.Length > 0)
							{
								try
								{
									SetBuff((Buffs) int.Parse(p[0]), true);
								}
								catch (Exception) {
									Log.E("Not a valid buff index.");
									return;
								}
							}
							Log.W($"Shrine buffs"
							      + $"\nAwaiting: {SaveData.AwaitingShrineBuff}"
							      + $" | Current: {SaveData.LastShrineBuffId}"
							      + $" | Cooldown: {SaveData.ShrineBuffCooldown}");
						};
						break;
						
					case cmd + "god":
						callback = (s, p) =>
						{
							ToggleGodMode();
							Log.W($"God mode {(_isPlayerGodMode ? "on" : "off")}");
						};
						break;

					case cmd + "buddha":
						callback = (s, p) =>
						{
							ToggleBuddhaMode();
							Log.W($"Buddha mode {(_isPlayerBuddhaMode ? "on" : "off")}");
						};
						break;

					case cmd + "pain":
						callback = (s, p) =>
						{
							var pain = Game1.random.Next(1, Game1.player.maxHealth * 3 / 2);
							Game1.player.takeDamage(pain, true, null);
							Log.W($"Hurt for {pain}");
						};
						break;

					case cmd + "chapter":
						callback = (s, p) =>
						{
							// Reset all chapter progress with no args
							var chapter = p.Length > 0 ? (ModData.Chapter) int.Parse(p[0]) : ModData.Chapter.None;
							var progress = p.Length > 1 ? (ModData.Progress) int.Parse(p[1]) : ModData.Progress.None;
							
							// Mark all chapters before this one as complete, and all after as not yet started
							for (var c = ModData.Chapter.None; c < chapter; ++c)
								SaveData.Story[c] = ModData.Progress.Complete;
							for (var c = chapter; c < ModData.Chapter.End; ++c)
								SaveData.Story[c] = ModData.Progress.None;

							// Set current chapter progress
							SaveData.Story[chapter] = progress;

							var currentStory = GetCurrentStory();
							Log.W($"Current chapter: {currentStory.Key} : {currentStory.Value}");
						};
						break;
				}
				
				for (var i = 0; i < command.Value.Length - 1; ++i)
					Helper.ConsoleCommands.Add(command.Value[i], command.Value[command.Value.Length - 1], callback);
				Helper.ConsoleCommands.Add(command.Key, command.Value[command.Value.Length - 1], callback);
			}
		}

		// SPRITE TESTING
		private void OnRendering(object sender, RenderingEventArgs e)
		{
			_yOffset = 6f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / (Math.PI * 300f));

			// Crystal glare
			e.SpriteBatch.Draw(_texture,
				new Rectangle(DestRectGlare.X,
					DestRectGlare.Y + (int)Math.Ceiling(_yOffset),
					DestRectGlare.Width,
					DestRectGlare.Height),
				SourceRectGlare,
				Color.White,
				0f,
				new Vector2(SourceRectGlare.Width / 2, SourceRectGlare.Height / 2), 
				SpriteEffects.None,
				1f);

			// Crystal ball
			for (var i = 0; i < SourceRects.Count; ++i)
			{
				e.SpriteBatch.Draw(_texture,
					new Rectangle(
						DestRects[i].X,
						DestRects[i].Y + (int)Math.Ceiling(_yOffset + _yOffset * Math.Abs(DestRects.Count / 2 - i) / 2),
						//DestRects[i].Y + (int)Math.Ceiling(_yOffset),
						DestRects[i].Width,
						DestRects[i].Height),
					SourceRects[i],
					Color.White,
					0f,
					new Vector2(SourceRects[i].Width / 2, SourceRects[i].Height / 2),
					SpriteEffects.None,
					0.9f - i / 10000f);
			}
		}
		// SPRITE TESTING

		#region Data Model

		private void LoadModData()
		{
			SaveData = Helper.Data.ReadSaveData<ModData>(
				ModConsts.SaveDataKey) ?? new ModData();
		}

		private void UnloadModData()
		{
			SaveData = null;
			_isPlayerSittingDown = false;
		}

		#endregion

		#region Game Events

		/// <summary>
		/// Pre-game
		/// </summary>
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			JaApi = Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
			JaApi.LoadAssets(Path.Combine(Helper.DirectoryPath, ModConsts.JaContentPackPath));
		}

		/// <summary>
		/// Pre-start of day
		/// </summary>
		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			LoadModData();
			GenerateDummyStorage();
		}

		private void GenerateDummyStorage()
		{
			var where = Game1.getLocationFromName(ModConsts.ShrineMapId);
			for (var x = 0; x < 2; ++x)
			{
				for (var y = 0; y < 2; ++y)
				{
					var position = new Vector2(DummyChestCoords.X + x, DummyChestCoords.Y + y);
					if (!where.isTileLocationTotallyClearAndPlaceable(position))
						continue;

					where.Objects.Add(position, new Chest(true, position));
					Log.W($"Added dummy storage at {position}");
				}
			}
		}

		/// <summary>
		/// Start of day
		/// </summary>
		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			var currentStory = GetCurrentStory();

			_shouldCrowsSpawnToday = 
				Game1.currentSeason != "winter" && !Game1.isRaining
				|| Game1.currentSeason == "winter" && Game1.random.NextDouble() < 0.3d;

			_whatAboutCatsCanTheySpawnToday = true; // TODO: METHOD: caats spawn conditions

			_isPlayerAgencySuppressed = false;
			_isPlayerSittingDown = false;

			if (SaveData.AwaitingShrineBuff)
			{
				Log.W($"Awaited and receiving buff: {SaveData.LastShrineBuffId}");
				SetBuff(SaveData.LastShrineBuffId, false);
			}
			else if (SaveData.ShrineBuffCooldown > 0)
			{
				--SaveData.ShrineBuffCooldown;
				Log.W($"Buff cooldown: {SaveData.ShrineBuffCooldown}");
			}

			if (currentStory.Value <= ModData.Progress.Started)
			{
				Log.D("Loading up bomba action listener.");
				SpaceEvents.BombExploded += HikawaBombExploded;
			}

			if (currentStory.Key == ModData.Chapter.Plant
			    && currentStory.Value >= ModData.Progress.Started)
			{
				SpaceEvents.OnItemEaten += HikawaFoodEaten;
				if (SaveData.BananaBunch > 0)
				{
					Game1.player.Stamina = Math.Min(Game1.player.MaxStamina / 3f,
						Game1.player.Stamina - 4 * SaveData.BananaBunch);
					--SaveData.BananaBunch;
				}
			}

			// TODO: CONTENT: Write and implement banana world effects
			if (SaveData.BananaRepublic == 0) {}
			else
			{
				if (SaveData.BananaRepublic < 3)
				{

				}
				else if (SaveData.BananaRepublic < 15)
				{

				}
				else if (SaveData.BananaRepublic < 50)
				{

				} 
				else if (SaveData.BananaRepublic < 300)
				{

				}
				else if (SaveData.BananaRepublic >= 300)
				{

				}

				SaveData.BananaRepublic -= Math.Max(1, (int) Math.Ceiling(SaveData.BananaRepublic / 25f));
			}

			if (Config.DebugMode)
				WarpToDefault(ModConsts.DebugDefaultWarpTo);
		}

		/// <summary>
		/// End of day
		/// </summary>
		private void OnDayEnding(object sender, DayEndingEventArgs e)
		{
		}

		/// <summary>
		/// Post-end of day
		/// </summary>
		private void OnSaving(object sender, SavingEventArgs e)
		{
			// TODO: DEBUG: Write save data
			//Helper.Data.WriteSaveData(ModConsts.SaveDataKey, Data);

			// TODO: DEBUG: Write bundle data
			if (Game1.dayOfMonth == 27)
				EmaMenu.ResetBundlesForNewSeason();
		}
		
		private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
		{
			Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

			UnloadModData();
		}

		/// <summary>
		/// Per-frame checks
		/// </summary>
		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;

			// Maintain permanent buffs for the day
			if (SaveData.LastShrineBuffId > Buffs.None
			    && !SaveData.AwaitingShrineBuff
			    && !Game1.eventUp && Context.IsPlayerFree)
				ReapplyBuffs(e.IsMultipleOf(180));

			if (!_isPlayerGodMode) {}
			else if (Game1.player.health < _playerHealthToMaintain)
			{
				Log.W($"Blocked {_playerHealthToMaintain - Game1.player.health} damage.");
				Game1.player.health = _playerHealthToMaintain;

				CheckForBuddhaOnMission();
			}

			if (!_isPlayerBuddhaMode) {}
			else if (Game1.player.health < 1)
			{
				Log.W($"Blocked {_playerHealthToMaintain - Game1.player.health} damage.");
				Game1.player.health = _playerHealthToMaintain = 1;
				_isPlayerGodMode = true;
			}
		}
		
		/// <summary>
		/// Location changed
		/// </summary>
		private void OnWarped(object sender, WarpedEventArgs e)
		{
			_isPlayerSittingDown = false;
			_isPlayerAgencySuppressed = false;

			if (e.OldLocation.Name.Equals(e.NewLocation.Name)) return;

			if (_overlayEffectControl.IsEnabled())
				_overlayEffectControl.Disable();

			SetBuddhaMode(CheckInterloper());
			SetGodMode(false);

			SetUpLocationCustomFlair(Game1.currentLocation);
		}

		/// <summary>
		/// Button check
		/// </summary>
		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (Game1.eventUp && !Game1.currentLocation.currentEvent.playerControlSequence
			    || Game1.currentBillboard != 0 || Game1.activeClickableMenu != null || Game1.menuUp || Game1.nameSelectUp
			    || Game1.IsChatting || Game1.dialogueTyping || Game1.dialogueUp
			    || Game1.player.UsingTool || Game1.pickingTool || Game1.numberOfSelectedItems != -1 || Game1.fadeToBlack)
				return;

			if (_isPlayerAgencySuppressed)
			{
				Helper.Input.Suppress(e.Button);
				return;
			}
			
			if (Game1.player.CanMove)
			{
				var btn = e.Button;

				// Additional world interactions
				if (btn.IsActionButton())
					TryCheckForActions(e.Cursor.GrabTile);

				// Debug functions
				if (Config.DebugMode)
					DebugCommands(btn);

				// Tool overrides
				if (btn.IsUseToolButton() && Game1.player.CurrentTool != null)
					TryCheckForToolUse(Game1.player.CurrentTool);
			}
			else if (!_isPlayerSittingDown)
			{}
			else
			{
				Helper.Input.Suppress(e.Button);
				SitDownEnd();
			}
		}

		#endregion

		#region Mini-Sit

		/// <summary>
		/// Lock the player into a sitting-down animation facing a given direction until they press any key.
		/// </summary>
		/// <param name="position">Target position in world coordinates to sit at.</param>
		/// <param name="direction">Value for direction to face, follows standard SDV rules of clockwise-from-zero.</param>
		private void SitDownStart(Vector2 position, int direction) {
			_playerLastStandingLocation = Game1.player.getTileLocation();

			Game1.playSound("breathin");
			Game1.player.faceDirection(direction);
			Game1.player.completelyStopAnimatingOrDoingAction();
			Game1.player.setTileLocation(position);
			Game1.player.yOffset = 0f;

			var animFrames = new FarmerSprite.AnimationFrame[1];
			animFrames[0] = new FarmerSprite.AnimationFrame(
				_playerSittingFrames[direction], 999999, false, direction == 3);
			Game1.player.FarmerSprite.animateOnce(animFrames);
			Game1.player.CanMove = false;
			_isPlayerSittingDown = true;
		}

		/// <summary>
		/// Remove restrictions from the player after sitting, and teleport them to their last standing position.
		/// </summary>
		private void SitDownEnd() {
			Game1.playSound("breathout");
			Game1.player.completelyStopAnimatingOrDoingAction();
			Game1.player.faceDirection(2);
			Game1.player.setTileLocation(_playerLastStandingLocation);
			Game1.player.CanMove = true;
			_isPlayerSittingDown = false;
			_playerLastStandingLocation = Vector2.Zero;

			if (Game1.currentSeason != "winter" || !Game1.currentLocation.IsOutdoors)
				return;

			var position = new Vector2(
				Game1.player.lastPosition.X,
				Game1.player.lastPosition.Y - 32);
			var id = 87008
			         + (int)Math.Floor(position.Y / 64)
			         * Game1.currentLocation.Map.DisplayWidth / 64 
			         + (int)Math.Floor(position.X / 64);

			// TODO: SYSTEM: Consider ways of having the farmer sprite appear above Front tiles on the tile above where they sit

			Log.D($"Cold butt identified: {id}, exists: {Game1.currentLocation.getTemporarySpriteByID(id) != null}",
				Config.DebugMode);

			if (Game1.currentLocation.getTemporarySpriteByID(id) != null)
				return;

			Log.D("Adding cold butt.",
				Config.DebugMode);

			var assetKey = Helper.Content.GetActualAssetKey(
				Path.Combine(ModConsts.SpritesPath, $"{ModConsts.ExtraSpritesFile}.png"));
			var direction = Game1.player.FacingDirection;
			var layer = (Game1.player.getStandingY() - 64f) / 10000f - 1f / 1000f;
			var multiplayer = Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
			multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(
				assetKey, 
				new Rectangle(64, direction % 2 == 0 ? 0 : 16, 16, 16),
				9999,
				1,
				9999,
				position,
				false,
				direction == 3,
				layer,
				0f,
				Color.White,
				4f,
				0f,
				direction == 0 ? 0f : (float)Math.PI,
				0f)
			{
				id = id,
				holdLastFrame = true,
				verticalFlipped = direction == 0
			});
		}

		#endregion
		
		#region Tile Actions

		/// <summary>
		/// Handles player interactions with custom mod elements.
		/// </summary>
		private void TryCheckForActions(Vector2 position)
		{
			CheckTileAction(position);
			if (Game1.player.ActiveObject == null || Game1.player.isRidingHorse())
				return;
			CheckHeldObjectAction(Game1.player.ActiveObject, Game1.player.currentLocation);
		}

		private string[] GetTileAction(Vector2 position)
		{
			var tile = Game1.currentLocation.map.GetLayer("Buildings").PickTile(
				new Location(
					(int)position.X * Game1.tileSize, 
					(int)position.Y * Game1.tileSize), 
				Game1.viewport.Size);
			var action = (PropertyValue)null;
			tile?.Properties.TryGetValue("Action", out action);

			if (action == null)
				return null;

			var strArray = ((string)action).Split(' ');
			var args = new string[strArray.Length - 1];
			Array.Copy(
				strArray, 1, 
				args, 0, 
				args.Length);

			return strArray;
		}
		
		private void CheckTileAction(Vector2 position)
		{
			var where = Game1.currentLocation;
			var property = GetTileAction(position);

			if (property == null)
				return;

			var action = property[0];
			switch (action)
			{
				// Enter the arcade machine minigame if used in the world
				case ModConsts.ActionArcade:
					Game1.currentMinigame = new ArcadeGunGame();
					break;

				// Bring up the shop menu if someone's attending the omiyageya
				case ModConsts.ActionShrineShop:
					var who = where.isCharacterAtTile(ModConsts.ShrineSouvenirShopPosition);
					if (who != null)
					{
						Game1.activeClickableMenu = new ShopMenu(GetSouvenirShopStock(who), 0, who.Name);
					}
					else
					{
						Game1.drawObjectDialogue(i18n.Get("string.shrine.shop_closed"));
					}
					break;

				// Using the Shrine offertory box
				case ModConsts.ActionShrineOffering:
					if (SaveData.Interlude)
					{
						CreateQuestionDialogue(
							i18n.Get("string.shrine.interloper_prompt"),
							new List<Response>
							{
								new Response("interloper", i18n.Get("dialogue.response.ready")),
								new Response("cancel", i18n.Get("dialogue.response.later"))
							});
					}
					else if (SaveData.AwaitingShrineBuff)
					{
						Game1.drawObjectDialogue(i18n.Get("string.shrine.offering_awaiting"));
						break;
					}
					else if (SaveData.ShrineBuffCooldown > 0)
					{
						Game1.drawObjectDialogue(i18n.Get("string.shrine.offering_cooldown"));
						break;
					}
					CreateQuestionDialogue(
						i18n.Get("string.shrine.offering_prompt"), 
						new List<Response>
						{
							new Response("offerS", $"{ModConsts.OfferingCostS}g"),
							new Response("offerM", $"{ModConsts.OfferingCostM}g"),
							new Response("offerL", $"{ModConsts.OfferingCostL}g"),
							new Response("cancel", i18n.Get("dialogue.response.cancel"))
						});
					break;

				// Interactions with the Ema stand at the Shrine
				case ModConsts.ActionEma:
					Log.W("ActionEma!");
					Game1.activeClickableMenu = new EmaMenu();
					break;
					
				// Trying to enter the Shrine Hall front doors
				case ModConsts.ActionShrineHall:
					Log.W("ActionShrineHall!");
					break;

				// Lockbox
				case ModConsts.ActionLockbox:
					Log.W("ActionLockbox!");
					break;

				// Sit on benches
				case ModConsts.ActionSit:
					var tileCoordinates = new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y));
					var direction = property.Length > 1 ? int.Parse(property[1]) : 2;
					SitDownStart(tileCoordinates, direction);

					break;
					
				// Vortex warps
				case ModConsts.ActionVortex:
					TouchVortexWarp(position);
					break;

				// Frogman Seijin
				case ModConsts.ActionFrogman:
					Log.W("ActionFrogman!");

					const int delay = 1500;
					Game1.player.completelyStopAnimatingOrDoingAction();
					_isPlayerAgencySuppressed = true;
					SetGodMode(true);

					// TODO: CONTENT: Action Frogman: Animate frogman for (delay) duration

					Game1.delayedActions.Add(new DelayedAction(delay, () =>
					{
						Game1.playSound("rainsound_wom");
						CreateInspectDialogue(i18n.Get("talk.misc.frogman"));

						// TODO: CONTENT: Action Frogman: Stop animating frogman and do something

						_isPlayerAgencySuppressed = false;
						SetGodMode(false);
					})); 

					break;
			}
		}

		#endregion

		#region Object Actions

		/// <summary>
		/// Method lifted from StardewValley.Object.performUseAction(Farmer who): Object.cs:2723 from ILSpy
		/// </summary>
		public void CheckHeldObjectAction(Object o, GameLocation where)
		{
			if (!Game1.player.CanMove || o.isTemporarilyInvisible || _isPlayerAgencySuppressed 
			    || !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack 
			    && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value)
				return;

			if (string.IsNullOrEmpty(o.Name))
				return;

			switch (o.Name)
			{
				case "Warp Totem: Shrine":
					StartWarpToShrine(o, where);
					break;

				case "Crystal Mirror":
					PromptCrystalMirror();
					break;
			}
		}

		/// <summary>
		/// Handles player using custom tools and weapons.
		/// </summary>
		public void TryCheckForToolUse(Tool tool)
		{
			switch (tool.Name)
			{
				case "Crystal Moon Wand":
					Log.W("Wand: Start of anim");
					Game1.playSound("wand");
					Game1.player.FarmerSprite.animateOnce(new[]
					{
						new FarmerSprite.AnimationFrame(
							57,
							1500,
							false,
							false),
						new FarmerSprite.AnimationFrame(
							(short)Game1.player.FarmerSprite.CurrentFrame,
							0,
							false,
							false,
							delegate
							{
								// TODO: SYSTEM: Crystal Moon Wand behaviours

								Log.W("Wand: End of anim");
							},
							true)
					});
					Game1.screenGlowOnce(Color.Violet, false);
					Utility.addSprinklesToLocation(
						Game1.player.currentLocation, 
						Game1.player.getTileX(), Game1.player.getTileY(), 
						16, 
						16, 
						1300, 
						20,
						Color.White,
						null,
						true);
					break;
			}
		}

		#endregion

		#region Miscellaneous Methods
		
		private static void WarpToDefault(string where)
		{
			if (string.IsNullOrEmpty(where) || Game1.getLocationFromName(where) == null)
				return;
			Game1.player.warpFarmer(
				new Warp(0, 0,
					where,
					ModConsts.DefaultWarps[where].X, ModConsts.DefaultWarps[where].Y,
					false));
		}

		private bool ToggleGodMode()
		{
			return SetGodMode(!_isPlayerGodMode);
		}

		private bool SetGodMode(bool isEnabled)
		{
			_isPlayerGodMode = isEnabled;
			_playerHealthToMaintain = Game1.player.health;
			return _isPlayerGodMode;
		}

		private bool ToggleBuddhaMode()
		{
			return SetBuddhaMode(!_isPlayerBuddhaMode);
		}

		private bool SetBuddhaMode(bool isEnabled)
		{
			_isPlayerBuddhaMode = isEnabled;
			return _isPlayerBuddhaMode;
		}

		private void SetBuff(Buffs id, bool reset)
		{
			Log.W($"Set buff '{id}', reset: {reset}");

			if (!Enum.IsDefined(typeof(Buffs), id))
				throw new ArgumentOutOfRangeException();

			// Remove any existing Shrine buff
			var currentBuff = Game1.buffsDisplay.otherBuffs.FirstOrDefault(_ => _.which == ModConsts.SharedBuffId);
			currentBuff?.removeBuff();
			
			SaveData.AwaitingShrineBuff = false;
			if (reset)
			{
				// Reset buff data for this save
				SaveData.LastShrineBuffId = id;
				SaveData.ShrineBuffCooldown = 0;
			}

			// Apply the buff
			ReapplyBuffs(true);
		}

		private void ReapplyBuffs(bool isThreeSecondUpdate)
		{
			var buff = Game1.buffsDisplay.otherBuffs.FirstOrDefault(_ => _.which == ModConsts.SharedBuffId);
			if (buff == null)
			{
				Game1.buffsDisplay.addOtherBuff(
					buff = new Buff(
						SaveData.LastShrineBuffId == Buffs.Comfort ? 1 : 0,
						SaveData.LastShrineBuffId == Buffs.Water ? 2 : 0,
						0,
						0,
						SaveData.LastShrineBuffId ==Buffs.Confidence ? 2 : 0,
						SaveData.LastShrineBuffId == Buffs.Comfort ? 1 : 0,
						0,
						SaveData.LastShrineBuffId == Buffs.Weightless ? 16 : 0,
						SaveData.LastShrineBuffId == Buffs.Comfort || SaveData.LastShrineBuffId == Buffs.Warmth ? 24 : 0,
						SaveData.LastShrineBuffId == Buffs.Wind ? 1 : 0,
						SaveData.LastShrineBuffId == Buffs.Sunlight ? 1 : 0,
						SaveData.LastShrineBuffId == Buffs.Sunlight ? 1 : 0,
						0,
						ModManifest.UniqueID,
						i18n.Get("string.shrine.buff_hover"))
					{
						sheetIndex = InitialBuffIconIndex + (int) SaveData.LastShrineBuffId,
						description = i18n.Get("string.shrine.offering_accepted." + (int)SaveData.LastShrineBuffId),
						which = ModConsts.SharedBuffId,
					});
			}
			buff.millisecondsDuration = 50;

			switch (SaveData.LastShrineBuffId)
			{
				case Buffs.Shivers: // Shivers () [寒]
					break;
				case Buffs.Uneasy: // Uneasy () [悪]
					break;
				case Buffs.Comfort: // Comfort (Farming) [畑]
					break;
				case Buffs.Warmth: // Warm breeze (Loot) [金]
					break;
				case Buffs.Hunger: // Hungry (Buff buffs) [心]
					break;
				case Buffs.Sunlight: // Sunlight (Health) [光]
					if (!isThreeSecondUpdate)
						break;
					Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + 1);
					break;
				case Buffs.Weightless: // Weight lifted (Stamina) [強]
					if (!isThreeSecondUpdate)
						break;
					Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + 1);
					break;
				case Buffs.Wind: // Wind (Speed) [風]
					break;
				case Buffs.Confidence: // Great confidence (Luck) [幸]
					break;
				case Buffs.Rain: // Cold breeze (Rain) [雨]
					break;
				case Buffs.Water: // Water (Fishing) [魚]
					break;
			}
		}

		/// <summary>
		/// Forces an NPC into a custom schedule for the day.
		/// </summary>
		private void ForceNpcSchedule(NPC npc)
		{
			npc.Schedule = npc.getSchedule(Game1.dayOfMonth);
			npc.scheduleTimeToTry = 9999999;
			npc.ignoreScheduleToday = false;
			npc.followSchedule = true;
		}

		private void ResetAnimationVars() {
			Game1.player.completelyStopAnimatingOrDoingAction();
			_animationExtraFloat = _animationStage = _animationTimer = 0;
			_animationTarget = Vector2.Zero;
			_animationFlag = false;
		}

		#endregion
		
		#region General Location Methods

		/// <summary>
		/// Adds unique elements to maps on entry.
		/// </summary>
		public void SetUpLocationCustomFlair(GameLocation where)
		{
			Log.D($"Warped to {where.Name}, adding flair.");
			var currentStory = GetCurrentStory();
			switch (where.Name)
			{
				// Hikawa Shrine
				case ModConsts.ShrineMapId:
				{
					if (IsItObonYet())
					{
						// Nice one

						// TODO: ASSETS: Obon decorations for the shrine

						SpawnPerchedCrows(where);
					}
					else if (currentStory.Key == ModData.Chapter.Mist && currentStory.Value == ModData.Progress.Started)
					{
						// Eerie effects
						_overlayEffectControl.Enable(OverlayEffectControl.Effect.Mist);
						SpawnCrows(
							where,
							new Location(
								where.Map.Layers[0].LayerWidth / 2 - 1,
								where.Map.Layers[0].LayerHeight / 10 * 9),
							new Location(
								where.Map.Layers[0].LayerWidth / 2 + 1,
								where.Map.Layers[0].LayerHeight / 10 * 9));
						if (!Game1.isRaining)
						{
							Game1.changeMusicTrack("communityCenter");
						}
					}
					else
					{
						if (_shouldCrowsSpawnToday)
						{
							if (Game1.timeOfDay < 1130)
							{
								// Spawn active crows on the ground in the morning
								SpawnCrows(where);
							}
							if (!Game1.isDarkOut())
							{
								// Spawn passive crows as custom perched critters in the afternoon
								var roll = Game1.random.NextDouble();
								var phobos = Vector2.Zero;
								var deimos = Vector2.Zero;
								var hopRange = 0;

								if (Game1.currentSeason == "winter")
									roll /= 2f;
								if (roll < 0.2d)
								{
									// Shrine front
									phobos = new Vector2(37.8f, 31.6f);
									deimos = new Vector2(39.2f, 31.6f);
								}
								else if (roll < 0.3d)
								{
									// Shrine left
									phobos = new Vector2(33, 31);
									deimos = new Vector2(35, 31.25f);
								}
								else if (roll < 0.4d)
								{
									// Shrine right
									phobos = new Vector2(42, 31.25f);
									deimos = new Vector2(44, 31);
								}
								else if (roll < 0.5d)
								{
									// House
									phobos = new Vector2(58, 20);
									deimos = new Vector2(60, 20.75f);
									hopRange = 2;
								}
								else if (roll < 0.65d)
								{
									// Tourou
									phobos = new Vector2(35, 39.2f);
									deimos = new Vector2(42, 39.2f);
								}
								else if (roll < 0.8d)
								{
									// Torii
									phobos = new Vector2(37, 50f);
									deimos = new Vector2(39, 50f);
									hopRange = 2;
								}
								else if (roll < 0.9d)
								{
									// Ema
									phobos = new Vector2(44.5f, 41.1f);
									deimos = new Vector2(46.5f, 41.1f);
								}
								else if (roll < 0.95d)
								{
									// Omiyageya
									phobos = new Vector2(27f, 39.3f);
									deimos = new Vector2(29.075f, 39.125f);
								}
								else
								{
									// Hall
									phobos = new Vector2(56, 32f);
									deimos = new Vector2(57, 34);
								}
								if (Game1.currentSeason == "winter")
									hopRange = 0;

								SpawnPerchedCrows(where, phobos, deimos, hopRange);
							}
						}
						if (_whatAboutCatsCanTheySpawnToday)
						{
							var roll = Game1.random.NextDouble();
							var position = Vector2.Zero;
							var baseFrame = GameObjects.Critters.Cat.StandingBaseFrame;
							var scareRange = 0;
							var flip = false;

							if (roll < 1f)
							{
								// Test animation: Grooming
								//position = new Vector2(28, 48);
								position = new Vector2(23, 45);
								baseFrame = GameObjects.Critters.Cat.StandingBaseFrame;
								scareRange = 3;
								flip = false;
							}

							SpawnCats(where, position, baseFrame, scareRange, flip);
						}
					}
					break;
				}

				// Rei's house
				case ModConsts.HouseMapId:
				{
					// Rei's custom door
					const int doorMarkerIndex = 32;
					var point = new Point(7, 12);

					if (where.Map.GetLayer("Buildings").Tiles[point.X, point.Y].TileIndex == doorMarkerIndex)
					{
						Log.D($"Marker found at {point.ToString()}");
						if (where.interiorDoors.ContainsKey(point))
						{
							Log.D($"Door found at {point.ToString()}");
							var interiorDoor = where.interiorDoors.Doors.First (door => door.Position == point);
							var texture = where.Map.GetTileSheet(ModConsts.IndoorsSpritesFile).ImageSource;
							Log.D($"Tilesheet image source: {texture}");
							var sprite = new TemporaryAnimatedSprite(
								texture,
								new Rectangle(0, 512, 64, 48),
								100f,
								4,
								1,
								new Vector2(point.X - 3, point.Y - 2) * 64f,
								false,
								false,
								((point.Y + 1) * 64 - 12) / 10000f,
								0f,
								Color.White,
								4f,
								0f,
								0f,
								0f)
							{
								holdLastFrame = true, 
								paused = true
							};
							interiorDoor.Sprite = sprite;
						}
						else
						{
							Log.E($"Failed to find a door at marker point {point.ToString()}: No entry!");
						}

						// Seasonal tiles
						// Butsudan
						var tilesheet = where.Map.GetTileSheet(ModConsts.IndoorsSpritesFile);
						var layer = where.Map.GetLayer("Buildings");
						var rowIncrement = tilesheet.SheetWidth;
						var index = 218;
						if (IsItObonYet())
						{
							// Obon
							layer.Tiles[16, 3].TileIndex = index;
							layer.Tiles[16, 4].TileIndex = index + rowIncrement;
							layer.Tiles[17, 3].TileIndex = index + 1;
							layer.Tiles[17, 4].TileIndex = index + rowIncrement + 1;
						}
						else
						{
							// Seasonal
							index = Game1.currentSeason switch
							{
								"spring" => 214,
								"summer" => 215,
								"fall" => 216,
								"winter" => 217,
								_ => index
							};
							layer.Tiles[17, 3].TileIndex = index;
							layer.Tiles[17, 4].TileIndex = index + rowIncrement;
						}
						// Window flowers
						index = Game1.currentSeason switch
						{
							"spring" => 244,
							"summer" => 245,
							"fall" => 246,
							"winter" => 247,
							_ => index
						};
						layer.Tiles[3, 15].TileIndex = index;
						layer.Tiles[3, 16].TileIndex = index + rowIncrement;
					}
					else
					{
						Log.E($"Door marker not found at point {point.ToString()}: No entry!");
					}
					break;
				}
				
				// Player's farm
				case "Farm":
				{
					if (currentStory.Key == ModData.Chapter.Plant
					    && currentStory.Value == ModData.Progress.Started)
					{
						// Plant
					}

					break;
				}
				
				// Doors
				case ModConsts.CorridorMapId:
				{
					// Haze effect
					_overlayEffectControl.Enable(OverlayEffectControl.Effect.Haze);

					break;
				}
				
				// Vortex
				case ModConsts.VortexMapId + "1":
				case ModConsts.VortexMapId + "2":
				case ModConsts.VortexMapId + "3":
				{
					// Obscuring darkness
					_overlayEffectControl.Enable(OverlayEffectControl.Effect.Dark, 1f);

					break;
				}
			}
		}

		#endregion

		#region Shrine Methods

		/// <summary>
		/// Attempts to add twin crows to the map as critters.
		/// </summary>
		private static void SpawnCrows(GameLocation where)
		{
			if (!where.IsOutdoors)
				return;

			const int retries = 25;
			const int radius = (3 - 1) / 2;
			var spawnArea = new Rectangle(19, 22, 43, 35);
			for (var attempts = 0; attempts < retries; ++attempts)
			{
				// Identify two separate nearby spawn positions for the crows around the map's middle
				var target = new Location(
					Game1.random.Next(spawnArea.X, spawnArea.X + spawnArea.Width),
					Game1.random.Next(spawnArea.Y, spawnArea.Y + spawnArea.Height));
				var phobos = Location.Origin;
				var deimos = Location.Origin;
				for (var y = -radius; y < radius; ++y)
				{
					for (var x = -radius; x < radius; ++x)
					{
						if (where.isTilePassable(new Location(target.X + x, target.Y + y), Game1.viewport))
							phobos = new Location(target.X + x, target.X + y);
						if (where.isTilePassable(new Location(target.X - x, target.Y - y), Game1.viewport))
							deimos = new Location(target.X - x, target.X - y);

						if (phobos == deimos && phobos != Location.Origin)
							break;
						if (phobos == deimos)
							continue;
						if (phobos == Location.Origin || deimos == Location.Origin)
							continue;

						SpawnCrows(where, phobos, deimos);
						return;
					}
				}
				Log.W($"Failed to add crows around {target.ToString()}.");
			}
			Log.W($"Failed to add crows after {retries} attempts.");
		}

		/// <summary>
		/// Attempts to add twin crows to the map as default Crow critters.
		/// </summary>
		private static void SpawnCrows(GameLocation where, Location phobos, Location deimos)
		{
			where.addCritter(new Crow(phobos.X, phobos.Y));
			where.addCritter(new Crow(deimos.X, deimos.Y));
		}
		
		private static void SpawnPerchedCrows(GameLocation where)
		{
			SpawnPerchedCrows(where, new Vector2(36, 48), new Vector2(41, 48), 2);
		}

		/// <summary>
		/// Attempt to add twin crows as custom CrowPerched critters.
		/// Crow tile coordinates are multiplied by 64f to get world coordinates.
		/// Crows swap places and patterns once every few days.
		/// </summary>
		/// <param name="where">Map location to spawn in.</param>
		/// <param name="phobos">Tile coordinates for the left-side crow.</param>
		/// <param name="deimos">Tile coordinates for the right-side crow.</param>
		/// <param name="hopRange">Distance to each side the crows can hop. 0 to disable.</param>
		private static void SpawnPerchedCrows(GameLocation where, Vector2 phobos, Vector2 deimos, int hopRange)
		{
			Log.W($"Adding perched crows at {phobos.ToString()} and {deimos.ToString()}");
			var isDeimos = Game1.dayOfMonth % 3 == 0;
			where.addCritter(new GameObjects.Critters.Crow(isDeimos,
				new Vector2(phobos.X, phobos.Y), hopRange));
			where.addCritter(new GameObjects.Critters.Crow(!isDeimos,
				new Vector2(deimos.X, deimos.Y), hopRange));
		}

		private static void SpawnCats(GameLocation where, Vector2 position, int baseFrame, int scareRange, bool flip)
		{
			Log.W($"Adding cat at {position.ToString()}");
			where.addCritter(new GameObjects.Critters.Cat(position, baseFrame, scareRange, flip));
		}

		public void StartWarpToShrine(Object o, GameLocation where) {
			var multiplayer = Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
			var index = JaApi.GetObjectId(o.Name);

			Game1.player.jitterStrength = 1f;
			where.playSound("warrior");
			Game1.player.faceDirection(2);
			Game1.player.temporarilyInvincible = true;
			Game1.player.temporaryInvincibilityTimer = -4000;
			Game1.changeMusicTrack("none");

			Game1.player.FarmerSprite.animateOnce(new[]
			{
				new FarmerSprite.AnimationFrame(
					57,
					2000,
					false,
					false),
				new FarmerSprite.AnimationFrame(
					(short)Game1.player.FarmerSprite.CurrentFrame,
					0,
					false,
					false,
					TotemWarpToShrine,
					true)
			});
			Game1.player.CanMove = false;

			multiplayer.broadcastSprites(where, new TemporaryAnimatedSprite(
				index, 
				9999f, 
				1, 
				999, 
				Game1.player.Position + new Vector2(0f, -96f), 
				false,
				false,
				false, 
				0f)
			{
				motion = new Vector2(0f, -1f),
				scaleChange = 0.01f,
				alpha = 1f,
				alphaFade = 0.0075f,
				shakeIntensity = 1f,
				initialPosition = Game1.player.Position + new Vector2(0f, -96f),
				xPeriodic = true,
				xPeriodicLoopTime = 1000f,
				xPeriodicRange = 4f,
				layerDepth = 1f
			});
			multiplayer.broadcastSprites(where, new TemporaryAnimatedSprite(
				index, 
				9999f, 
				1, 
				999, 
				Game1.player.Position + new Vector2(-64f, -96f),
				false,
				false,
				false, 
				0f)
			{
				motion = new Vector2(0f, -0.5f),
				scaleChange = 0.005f,
				scale = 0.5f,
				alpha = 1f,
				alphaFade = 0.0075f,
				shakeIntensity = 1f,
				delayBeforeAnimationStart = 10,
				initialPosition = Game1.player.Position + new Vector2(-64f, -96f),
				xPeriodic = true,
				xPeriodicLoopTime = 1000f,
				xPeriodicRange = 4f,
				layerDepth = 0.9999f
			});
			multiplayer.broadcastSprites(where, new TemporaryAnimatedSprite(
				index, 
				9999f, 
				1, 
				999, 
				Game1.player.Position + new Vector2(64f, -96f), 
				false,
				false,
				false, 
				0f) 
			{
				motion = new Vector2(0f, -0.5f),
				scaleChange = 0.005f,
				scale = 0.5f,
				alpha = 1f,
				alphaFade = 0.0075f,
				delayBeforeAnimationStart = 20,
				shakeIntensity = 1f,
				initialPosition = Game1.player.Position + new Vector2(64f, -96f),
				xPeriodic = true,
				xPeriodicLoopTime = 1000f,
				xPeriodicRange = 4f,
				layerDepth = 0.9988f
			});
			Game1.screenGlowOnce(Color.Violet, false);
			Utility.addSprinklesToLocation(
				where, 
				Game1.player.getTileX(), Game1.player.getTileY(), 
				16, 
				16, 
				1300, 
				20,
				Color.White,
				null,
				true);
		}

		/// <summary>
		/// Method lifted from StardewValley.Object.totemWarp(Farmer who): Object.cs:2614 from ILSpy
		/// </summary>
		public void TotemWarpToShrine(Farmer who)
		{
			var multiplayer = Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
			for (var j = 0; j < 12; j++)
			{
				multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(
					354, 
					Game1.random.Next(25, 75), 
					6, 
					1, 
					new Vector2(
						Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), 
						Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), 
					false, 
					Game1.random.NextDouble() < 0.5));
			}
			who.currentLocation.playSound("wand");
			Game1.displayFarmer = false;
			Game1.player.temporarilyInvincible = true;
			Game1.player.temporaryInvincibilityTimer = -2000;
			Game1.player.freezePause = 1000;
			Game1.flashAlpha = 1f;
			DelayedAction.fadeAfterDelay(FinishTotemWarp, 1000);
			new Rectangle(
				who.GetBoundingBox().X, who.GetBoundingBox().Y, 64, 64)
				.Inflate(192, 192);

			var i = 0;
			for (var x = who.getTileX() + 8; x >= who.getTileX() - 8; x--)
			{
				multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(
					6, 
					new Vector2(x, who.getTileY()) * 64f, 
					Color.White, 
					8, 
					false, 
					50f) 
				{
					layerDepth = 1f,
					delayBeforeAnimationStart = i * 25,
					motion = new Vector2(-0.25f, 0f)
				});
				i++;
			}
		}
		
		/// <summary>
		/// Method lifted from StardewValley.Object.totemWarpForReal(): Object.cs:2641 from ILSpy
		/// </summary>
		public void FinishTotemWarp()
		{
			var coords = ModConsts.TotemWarpPosition;
			Game1.warpFarmer(ModConsts.ShrineMapId, coords.X, coords.Y, false);
			Game1.fadeToBlackAlpha = 0.99f;
			Game1.screenGlow = false;
			Game1.player.temporarilyInvincible = false;
			Game1.player.temporaryInvincibilityTimer = 0;
			Game1.displayFarmer = true;
		}
		
		private void MakeShrineOffering(Farmer farmer, string answer)
		{
			Game1.playSound("purchase");
			SaveData.AwaitingShrineBuff = true;
			var roll = Game1.player.DailyLuck;
			var tribute = 0;
			var cooldown = 0;
			switch (answer)
			{
				case "offerS":
					cooldown = 1;
					tribute = ModConsts.OfferingCostS;
					roll += Game1.random.Next(0, 4);
					break;

				case "offerM":
					cooldown = 2;
					tribute = ModConsts.OfferingCostM;
					roll += Game1.random.Next(0, 6) * 1.25f;
					break;

				case "offerL":
					cooldown = 3;
					tribute = ModConsts.OfferingCostL;
					roll += Game1.random.Next(2, 6) * 1.5f;
					break;

				default:
					throw new FormatException($"Input string \"{answer}\" was not in correct format.");
			}

			Log.D($"Shrine: Rolled {roll} (with luck at{Game1.player.DailyLuck}) for offering {tribute}g.",
				Config.DebugMode);
			
			farmer.Money -= tribute;
			SaveData.ShrineBuffCooldown = cooldown;
			var whichBuff = (int)Math.Floor(roll);
			if (whichBuff < 0) whichBuff = 0;
			if (whichBuff > (int) Buffs.Confidence) whichBuff = (int) Buffs.Confidence;

			// Roll for extra effects
			if (whichBuff > 0)
			{
				roll = Game1.random.NextDouble();
				Log.D($"Shrine: Rerolled {roll}.",
					Config.DebugMode);
				if (roll < 0.05)
				{
					whichBuff = (int) Buffs.Rain;
				}
				else if (roll < 0.1)
				{
					whichBuff = (int) Buffs.Water;
				}
				else if (roll < 0.15)
				{
					whichBuff = (int) Buffs.Love;
				}
			}

			SaveData.LastShrineBuffId = (Buffs) whichBuff;

			farmer.FarmerSprite.animateOnce(new[]
			{
				new FarmerSprite.AnimationFrame(
					57,
					1500,
					false,
					false),
				new FarmerSprite.AnimationFrame(
					(short)Game1.player.FarmerSprite.CurrentFrame,
					3500,
					false,
					false,
					delegate
					{
						var whichSound = "yoba";
						if (whichBuff == 0)
							whichSound = ModConsts.ContentPrefix + "rainsound_wom";
						else if (whichBuff < 4)
							whichSound = ModConsts.ContentPrefix + "rainsound_ooh";
						else if (whichBuff < 7)
							whichSound = ModConsts.ContentPrefix + "rainsound_ahh";
						Game1.currentLocation.localSound(whichSound);
						Game1.drawObjectDialogue(i18n.Get("string.shrine.offering_accepted." + whichBuff));
						_isPlayerAgencySuppressed = false;
					},
					true)
			});
			_isPlayerAgencySuppressed = true;
		}
		
		public static bool IsItObonYet()
		{
			return Game1.currentSeason == "summer" && Game1.dayOfMonth > 27
			       || Game1.currentSeason == "fall" && Game1.dayOfMonth < 3;
		}
		
		private static string GetContentPackId(string name)
		{
			return Regex.Replace(ModConsts.ContentPrefix + name,
				"[^a-zA-Z0-9_.]", "");
		}

		public Dictionary<ISalable, int[]> GetSouvenirShopStock(NPC who)
		{
			var stock = new Dictionary<ISalable, int[]>();

			// TODO: CONTENT: Compile the shop stock
			// Build for whichever events seen, whatever time of year, whoever is behind the counter, story progress

			if (who.Name == ModConsts.ReiNpcId)
			{
				foreach (var hat in JaApi.GetAllHatsFromContentPack(GetContentPackId("Hats")))
				{
					stock.Add(new Object(JaApi.GetHatId(hat), 1), new[] {1150, 1});
				}
			}

			return stock;
		}

		#endregion

		#region Vortex Methods
		
		private void TouchVortexWarp(Vector2 fromPosition)
		{
			_isPlayerAgencySuppressed = true;
			Helper.Events.GameLoop.UpdateTicked += UpdateVortexWarp;
			ResetAnimationVars();
			_animationExtraFloat = Game1.player.FacingDirection;
			_animationTarget = fromPosition;
		}

		private void UpdateVortexWarp(object sender, UpdateTickedEventArgs e)
		{
			const int halfwayStage = 32; // When AnimationStage reaches this value, set AnimationFlag
			const int animRate = 10; // Overall speed of animation
			const int numOfBeeps = 3;
			const int inVelocity = 1;
			const int outVelocity = 2;

			_animationExtraInt = halfwayStage * 2 * animRate;

			// Spin the player with acceleration and deceleration
			// AnimationStage: Linearly increasing/decreasing rate to turn the player with FacingDirection
			// AnimationFlag: Whether we've passed the middle of the animation
			// AnimationExtraFloat: Stores original facing direction
			// AnimationExtraInt: Stores duration of animation for the dizzy cooldown timer to add onto
			// After Flag is set; fade to black, warp the player, double the counter speed, and run it in reverse

			_animationTimer += animRate;
			if (_animationTimer % Math.Max(halfwayStage * 2 - _animationStage, 1)
			    > animRate * (_animationFlag ? outVelocity : inVelocity))
				return;

			// Turn the player around 90° for each stage
			_animationStage += _animationFlag ? -1 : 1;
			Game1.player.FacingDirection = (int)(_animationExtraFloat + _animationStage) % 4;
			
			// Play sounds as you warp out/in
			if (_animationStage > 0 && _animationStage % (halfwayStage / numOfBeeps) == 0 || _animationStage == 1)
			{
				Game1.playSound(ModConsts.ContentPrefix + "vortex" + Math.Min(4, Math.Max(0, _animationStage / 10)));
			}
			// Warp after spin-in
			if (_animationStage == halfwayStage && !_animationFlag)
				Game1.globalFadeToBlack(VortexWarpActuallyHappens, 0.04f);
			// Exit after spin-out
			if (_animationStage <= 0 && _animationFlag)
				EndVortexWarp();
		}

		private void VortexWarpActuallyHappens()
		{
			var property = GetTileAction(_animationTarget);
			if (property != null && property.Length > 1)
			{
				var target = new Vector2(int.Parse(property[1]), int.Parse(property[2]));
				if (property.Length > 3)
					Game1.currentLocation = Game1.getLocationFromName(property[3]);
				Game1.player.Position = new Vector2(target.X * 64f, (target.Y + 1) * 64f);
				Game1.globalFadeToClear(null, 0.04f);
			}
			else
			{
				Log.E($"Bad vortex warp tile data: Position {_animationTarget} : {property}");
				Log.E("This is exactly the reason why you have a magic mirror");
			}
			_animationFlag = true; // Start running counter in reverse
		}

		private void EndVortexWarp()
		{
			const int dizzyExtraCooldownTime = 22000;

			_isPlayerAgencySuppressed = false;
			Helper.Events.GameLoop.UpdateTicked -= UpdateVortexWarp;
			ResetAnimationVars();

			++_dizzyStack;
			Log.W($"Dizzy up to {_dizzyStack}");
			if (_dizzyStack > 2)
			{
				_isPlayerAgencySuppressed = true;
				SetGodMode(true);
				
				Game1.currentLocation.localSound("croak");
				Game1.player.FacingDirection = 2;
				Game1.player.FarmerSprite.animateOnce(224, 400f, 4, who =>
				{
					--_dizzyStack;
					_isPlayerAgencySuppressed = false;
					SetGodMode(false);
				});
				Game1.player.doEmote(12);
			}
			else
			{
				var cooldownBeforeDizzyClears = _animationExtraInt + dizzyExtraCooldownTime;
				Game1.delayedActions.Add(new DelayedAction(cooldownBeforeDizzyClears, () =>
				{
					--_dizzyStack;
					Log.W($"Dizzy down to {_dizzyStack}");
				}));
			}
			
		}

		#endregion

		#region Mission Methods

		internal static KeyValuePair<ModData.Chapter, ModData.Progress> GetCurrentStory()
		{
			var reverseOrder = Instance.SaveData.Story;
			return reverseOrder.Reverse().FirstOrDefault(_ => _.Value != ModData.Progress.None);
		}

		// TODO: SYSTEM: Implement CleanUpMissionState when moving in/out of chapters

		private void CleanUpMissionState()
		{
			SetGodMode(false);
			SetBuddhaMode(false);
			CheckInterloper();

			var itemsToRemove = new[]
			{
				"Crystal Mirror",
			};
			foreach (var itemToRemove in itemsToRemove)
			{
				var item = Game1.player.hasItemWithNameThatContains(itemToRemove);
				if (item != null)
					Game1.player.removeItemFromInventory(item);
			}
		}

		internal bool CheckInterloper()
		{
			_isInterloper = false;
			var currentStory = GetCurrentStory();
			var where = Game1.currentLocation;
			if (where.Name.StartsWith(ModConsts.VortexMapId)
			    || where.Name.StartsWith(ModConsts.CorridorMapId))
			{
				_isInterloper = true;
			}
			Log.W($"Interloper: {_isInterloper}");
			return _isInterloper;
		}

		private void StartMission()
		{
			Log.W("StartMission");

			// If a mission requires that the player has a slot for an item eg. Wand or Mirror, and they don't have one, break out
			//if (Game1.player.freeSpotsInInventory() < 3 && Data.StoryDoors > (int) ModConsts.Progress.Started || Data.StoryGap)

			// TODO: CONTENT: Fill in mission start data

			// TODO: SYSTEM: Add Buddha mode on lethal missions
		}

		private void FleeMission()
		{
			Log.W("FleeMission");
			
			// TODO: CONTENT: Fill in mission flee data
		}

		private void EndMission()
		{
			Log.W("EndMission");
			
			// TODO: CONTENT: Fill in mission ending data

		}

		private void CheckForBuddhaOnMission()
		{
			if (CheckInterloper())
			{
				Log.W("Interloper caught in Buddha");

				// TODO: CONTENT: Fill in mission knockout data

				StartBubbleWarpOut();
			}
		}
		
		private void PromptCrystalMirror()
		{
			SetGodMode(true);

			var dialogue = new List<string>{ i18n.Get("dialogue.flee_inspect") };
			var options = new List<Response>
			{
				new Response("flee", i18n.Get("dialogue.response.veryready")),
				new Response("cancel", i18n.Get("dialogue.response.cancel")),
			};
			CreateInspectThenQuestionDialogue(dialogue, options);
		}

		private void StartBubbleWarpOut()
		{
			SetGodMode(true);
			_isPlayerAgencySuppressed = true;
			ResetAnimationVars();
			Helper.Events.GameLoop.UpdateTicked += UpdateBubbleWarp;
			Helper.Events.Display.RenderingWorld += DrawBubbleWarp;

			// TODO: CONTENT: Choose a unique animation set for each of defeated and fleeing, use the mirror sprite if possible

			_animationExtraFloat = Game1.player.health < 10 ? 5 : 70;
			Game1.player.FarmerSprite.setCurrentAnimation(new []
			{
				new FarmerSprite.AnimationFrame((int)_animationExtraFloat, 1500,
					false, false, delegate 
					{
						
					}), 
			});
		}

		private void DrawBubbleWarp(object sender, RenderingWorldEventArgs e)
		{
			// TODO: CONTENT: Draw bubble forming animation
			// TODO: CONTENT: Draw bubble floating animation
		}

		private void UpdateBubbleWarp(object sender, UpdateTickedEventArgs e)
		{
			const int bubbleHeight = 96;
			const int upVelocity = -3;
			const int downVelocity = 2;
			const int looneyTunesDuration = 400;

			// AnimationExtraValue: The farmer's animation frame while in the bubble
			// AnimationTarget: The current position of the bubble as it moves up/down the screen
			// AnimationFlag: Whether the animation is in reverse, and floating down from the top of the screen into a new location

			_animationTarget.X = 6f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / (Math.PI * 384f));
			_animationTarget.Y += _animationFlag ? downVelocity : upVelocity;

			// TODO: SLEEPY: Test bubble out offset and animation
			var farmerOffset = 0 - _animationTarget.Y
			                   + Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Y
			                   + Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Height / 2f;
			Game1.player.FarmerSprite.setCurrentFrame((int)_animationExtraFloat, (int)farmerOffset);
			
			// End bubble warp once the bubble reaches a goal position
			if (_animationFlag && Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Height / 2f - _animationTarget.Y
			    < downVelocity * 1.5f)
			{
				// TODO: CONTENT: Play bubble hold-and-pop animation
				Game1.playSound("coin");

				Game1.player.FarmerSprite.setCurrentAnimation(new []
				{
					// Frame 94 -- Shocked farmer -- o><
					new FarmerSprite.AnimationFrame(94, looneyTunesDuration, false, false,
						delegate 
						{
							Game1.playSound("clubhit");
							EndBubbleWarp();
						})
				});
			}
			else if (Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Y - _animationTarget.Y
			    < bubbleHeight * CommonTextureScale)
			{
				Game1.globalFadeToBlack(EndBubbleWarp);
			}
		}

		private void EndBubbleWarp()
		{
			SetBuddhaMode(false);
			SetGodMode(false);
			_isPlayerAgencySuppressed = false;
			ResetAnimationVars();

			// Continue to the float-down phase from the float-up phase
			// End the bubble warp when float-down finishes
			if (_animationFlag)
			{
				
				Helper.Events.GameLoop.UpdateTicked -= UpdateBubbleWarp;
				Helper.Events.Display.RenderingWorld += DrawBubbleWarp;
			}
			else
			{
				Game1.currentLocation = Game1.getLocationFromName(ModConsts.ShrineMapId);
				Game1.player.Position = new Vector2(38.5f, 39f);
				_animationFlag = true;
			}
		}

		#endregion

		#region SpaceEvents
		
		private void HikawaBombExploded(object sender, EventArgsBombExploded e)
		{
			var distance = Vector2.Distance(ModConsts.StoryStockPosition, e.Position);
			if (Game1.currentLocation.Name == "Town" && distance <= e.Radius * 2f)
			{
				Game1.playSound("reward");

				Game1.currentLocation.currentEvent = new Event(Helper.Content.Load<string>(
					Path.Combine(ModConsts.AssetsPath, ModConsts.EventsPath)));

				// TODO: METHOD: Patch Town at the end of the event
				//Helper.Content.InvalidateCache(@"Maps/Town");

				SpaceEvents.BombExploded -= HikawaBombExploded;
			}
		}

		private void HikawaFarmEvents(object sender, EventArgsChooseNightlyFarmEvent e)
		{
			Log.D($"HikawaFarmEvents: (vanilla event: {e.NightEvent != null})",
				Config.DebugMode);
			if (e.NightEvent != null)
				Log.D($"(vanilla event type: {e.NightEvent.GetType().FullName})",
					Config.DebugMode);
			var currentStory = GetCurrentStory();
			if (currentStory.Key == ModData.Chapter.Plant
			    && currentStory.Value == ModData.Progress.Started
			    && Game1.weatherForTomorrow == Game1.weather_rain
			    || Config.DebugShowRainInTheNight)
			{
				Log.D("Rain on the horizon.",
					Config.DebugMode);
				e.NightEvent = new GameObjects.Events.RainInTheNight();
			}
		}
		
		// TODO: SYSTEM: Remove this event listener from the list under conditions
		private void HikawaFoodEaten(object sender, EventArgs e)
		{
			var item = Game1.player.itemToEat;
			var itemDescription = Game1.objectInformation[item.ParentSheetIndex].Split('/');
			var isDrink = itemDescription.Length > 6 && itemDescription[6].Equals("drink");

			// Avoid stardrops and inedible objects
			if (item.ParentSheetIndex == 434 || int.Parse(itemDescription[2]) <= 0)
				return;
			
			// TODO: Test buff: Hunger

			if (SaveData.LastShrineBuffId == Buffs.Hunger)
			{
				// Boost recovery
				var energy = item.staminaRecoveredOnConsumption();
				var health = item.healthRecoveredOnConsumption();
				Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + energy / 15);
				Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + health / 12);
				
				// Buff buffs
				var stats = itemDescription.Length > 7
					? Array.ConvertAll(itemDescription[7].Split(' '), int.Parse)
					: new[] {0};
				var newStats = new[]
				{
					stats[0] > 0 ? stats[0] + 1 : 0,
					stats[1] > 0 ? stats[1] + 1 : 0,
					stats[2] > 0 ? stats[2] + 1 : 0,
					stats[3],
					stats[4],
					stats[5] > 0 ? stats[5] + 1 : 0,
					stats[6],
					stats[7] + stats[7] / 8,
					stats[8] + stats[8] / 5,
					stats[9],
					stats[10] > 0 ? stats[10] + 1 : 0,
					stats.Length > 11 && stats[11] > 0 ? stats[11] + 1 : 0
				};
				var buff = new Buff(
					newStats[0],
					newStats[1],
					newStats[2],
					newStats[3],
					newStats[4],
					newStats[5],
					newStats[6],
					newStats[7],
					newStats[8],
					newStats[9],
					newStats[10],
					newStats[11],
					itemDescription.Length > 8 ? int.Parse(itemDescription[8]) : -1,
					itemDescription[0], 
					itemDescription[4]);
				var duration = Math.Min(120000, (int) (int.Parse(itemDescription[2]) / 20f * 30000f));

				Log.D($"Boosting buff: {item.DisplayName}"
				      + $"\nOriginal: {stats.Aggregate("", (s, i) => s + $"{i} ")}" 
				      + $"\nBoosted:  {newStats.Aggregate("", (s, i) => s + $"{i} ")}",
					Config.DebugMode);
				Log.D($"Boosting recovery: E{energy} + {energy / 15}, H{health} + {health / 12}",
					Config.DebugMode);

				if (isDrink)
				{
					Game1.buffsDisplay.tryToAddDrinkBuff(buff);
				}
				else
				{
					Game1.buffsDisplay.tryToAddFoodBuff(buff, duration);
				}
			}

			if (item.Name.StartsWith("Dark Fruit") || item.Name == "Energized Dark Fruit")
			{
				++SaveData.BananaBunch;
				if (SaveData.BananaBunch > ModConsts.BananaBegins)
				{
					var foodEnergy = item.staminaRecoveredOnConsumption();
					Game1.player.Stamina += Math.Max(foodEnergy * 3, foodEnergy / (SaveData.BananaBunch * foodEnergy) * foodEnergy);
				}
			}
			else
			{
				if (SaveData.BananaBunch > ModConsts.BananaBegins)
				{
					var foodEnergy = item.staminaRecoveredOnConsumption();
					Game1.player.Stamina -= Math.Min(foodEnergy, foodEnergy / (SaveData.BananaBunch * foodEnergy) * foodEnergy);
				}
			}
		}
		
		private void HikawaGiftsGiven(object sender, EventArgsGiftGiven e)
		{
			// TODO: Buff effects for Love
		}

		#endregion

		#region Dialogue Methods
		
		private void CreateInspectDialogue(string dialogue)
		{
			Game1.drawDialogueNoTyping(dialogue);
		}

		private void CreateQuestionDialogue(string question, List<Response> answers)
		{
			Game1.currentLocation.createQuestionDialogue(question, answers.ToArray(), DialogueAnswers);
		}

		/// <summary>
		/// Creates a hybrid dialogue box using features of inspectDialogue and questionDialogue.
		/// A series of dialogues is presented, with the final dialogue having assigned responses.
		/// </summary>
		private void CreateInspectThenQuestionDialogue(List<string> dialogues, List<Response> answerChoices)
		{
			Game1.currentLocation.afterQuestion = DialogueAnswers;
			Game1.activeClickableMenu = new MultipleDialogueQuestion(Helper, dialogues, answerChoices);
			Game1.dialogueUp = true;
			Game1.player.canMove = false;
		}

		private void DialogueAnswers(Farmer who, string answer)
		{
			if (string.IsNullOrEmpty(answer) || answer == "cancel")
				return;
			var ans = answer.Split(' ');
			Log.W($"Received dialogue answer \'{ans}\'.");
			switch (ans[0])
			{
				case "offerS":
				case "offerM":
				case "offerL":
					MakeShrineOffering(who, answer);
					break;

				case "interloper":
					StartMission();
					break;

				case "flee":
					StartBubbleWarpOut();
					break;

				default:
					Log.E($"Invalid dialogue key: {answer}");
					SetGodMode(false);
					break;
			}
		}

		#endregion

		#region Debug Methods

		private void DebugCommands(SButton btn)
		{
			if (btn.Equals(Config.DebugPlantBanana))
			{
				var position = Game1.player.getTileLocation();
				--position.Y;
				if (!Context.IsMainPlayer)
					return;

				var farm = Game1.getLocationFromName("Farm") as Farm;
				if (farm.terrainFeatures.ContainsKey(position))
					farm.terrainFeatures.Remove(position);
				farm.terrainFeatures.Add(position, new GameObjects.HikawaBanana());
			}
			if (btn.Equals(Config.DebugPlayArcade))
			{
				if (false)
				{
					_overlayEffectControl.Toggle();
				}
				else
				{
					Log.D($"Pressed {btn} : Playing {ModConsts.ArcadeMinigameId}",
						Config.DebugMode);
					Game1.currentMinigame = new ArcadeGunGame();
				}
			}
			else if (btn.Equals(Config.DebugWarpShrine))
			{
				var mapName = "";
				if (false)
				{
					mapName = "Town";
					Game1.player.warpFarmer(
						new Warp(0, 0, mapName,
							20, 5, true));
				}
				else if (false)
				{
					mapName = ModConsts.HouseMapId;
					Game1.player.warpFarmer(
						new Warp(0, 0, mapName,
							5, 19, false));
				}
				else
				{
					mapName = ModConsts.ShrineMapId;
					Game1.player.warpFarmer(
						new Warp(0, 0, mapName,
							39, 60, false));
				}
				Log.D($"Pressed {btn} : Warping to {mapName}",
					Config.DebugMode);
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
}

#region Nice Code

// nice code

/*

// Oscillation
if (fairyAnimationTimer > 2000 && fairyPosition.Y > -999999f)
{
	fairyPosition.X += (float)Math.Cos((double)time.TotalGameTime.Milliseconds * Math.PI / 256.0) * 2f;
	fairyPosition.Y -= (float)time.ElapsedGameTime.Milliseconds * 0.2f;
}

/// <summary>
/// Erases common tile features from the destination, replaces them with a clone of the source.
/// </summary>
/// <param name="source">Location to be cloned.</param>
/// <param name="dest">Location to be overwritten.</param>
public static void SoftCopyLocationObjects(GameLocation source, GameLocation dest)
{
	dest.objects.Clear();
	foreach (var k in source.Objects.Keys)
	{
		dest.Objects.TryGetValue(k, out var v);
		dest.objects.Add(k, v);
	}
	dest.netObjects.Clear();
	foreach (var k in source.netObjects.Keys)
	{
		source.netObjects.TryGetValue(k, out var v);
		dest.netObjects.Add(k, v);
	}
	dest.terrainFeatures.Clear();
	foreach (var f in source.terrainFeatures)
		dest.terrainFeatures.Add(f);
}

public override bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, 
xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character)
{
	if (oldMariner != null && position.Intersects(oldMariner.GetBoundingBox()))
	{
		return true;
	}
	return base.isCollidingPosition(position, viewport, isFarmer, damagesFarmer, glider, character);
}

public override void checkForMusic(GameTime time)
{
	if (Game1.random.NextDouble() < 0.003 && Game1.timeOfDay < 1900)
	{
		localSound("seagulls");
	}
	base.checkForMusic(time);
}


case 139067618:
if (s == "IceCreamStand")
{
    if (this.isCharacterAtTile(new Vector2((float) tileLocation.X, (float) (tileLocation.Y - 2))) != null 
	|| this.isCharacterAtTile(new Vector2((float) tileLocation.X, (float) (tileLocation.Y - 1))) != null 
	|| this.isCharacterAtTile(new Vector2((float) tileLocation.X, (float) (tileLocation.Y - 3))) != null)
    {
    Game1.activeClickableMenu = (IClickableMenu) new ShopMenu(new Dictionary<ISalable, int[]>()
    {
        {
        (ISalable) new Object(233, 1, false, -1, 0),
        new int[2]{ 250, int.MaxValue }
        }
    }, 0, (string) null, (Func<ISalable, Farmer, int, bool>) null, (Func<ISalable, bool>) null, (string) null);
    goto default;
    }
    else if (Game1.currentSeason.Equals("summer"))
    {
    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:IceCreamStand_ComeBackLater"));
    goto default;
    }
    else
    {
    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:IceCreamStand_NotSummer"));
    goto default;
    }
}
else
    goto default;
*/

#endregion