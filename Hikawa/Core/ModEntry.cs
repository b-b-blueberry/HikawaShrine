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
using Object = StardewValley.Object;

using SpaceCore.Events;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using xTile.ObjectModel;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa
{
	public class ModEntry : Mod
	{
		internal static ModEntry Instance;
		internal ModSaveData SaveData;
		internal IJsonAssetsApi JaApi;
		private readonly GameObjects.OverlayEffectControl _overlayEffectControl = new GameObjects.OverlayEffectControl();

		internal Config Config;
		internal ITranslationHelper i18n => Helper.Translation;

		private bool _isPlayerAgencySuppressed;
		private bool _isPlayerSittingDown;
		private readonly int[] _playerSittingFrames = {62, 117, 54, 117};
		private Vector2 _playerLastStandingLocation;
		private bool _shouldCrowsSpawnToday;
		private bool _whatAboutCatsCanTheySpawnToday;


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
			helper.Events.GameLoop.Saved += OnSaved;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
			
			helper.Events.Player.Warped += OnWarped;
			helper.Events.Input.ButtonPressed += OnButtonPressed;

			SpaceEvents.ChooseNightlyFarmEvent += HikawaFarmEvents;
			
			if (Config.DebugMode)
			{
				// Add debugging commands
				AddConsoleCommands();

				// Add texture render tests
				//helper.Events.Display.Rendering += OnRendering;
				var textureName = Path.Combine(ModConsts.SpritesPath, ModConsts.ExtraSpritesFile + ".png");
				_texture = Instance.Helper.Content.Load<Texture2D>(textureName);
			}
		}

		private void AddConsoleCommands()
		{
			// TODO: METHOD: Find some way to set default warp location, intercept Warped maybe?

			Helper.ConsoleCommands.Add("harcade", "Start arcade game: use START, TITLE, RESET", (s, p) =>
			{
				if (p[0].ToLower() == "start")
					Game1.currentMinigame = new ArcadeGunGame();
				else if (Game1.currentMinigame != null
				         && Game1.currentMinigame is ArcadeGunGame
				         && Game1.currentMinigame.minigameId() == ModConsts.ArcadeMinigameId)
				{
					if (p[0].ToLower() == "title")
						((ArcadeGunGame)(Game1.currentMinigame)).ResetAndReturnToTitle();
					else if (p[0].ToLower() == "reset")
						((ArcadeGunGame)(Game1.currentMinigame)).ResetGame();
				}
			});
			Helper.ConsoleCommands.Add("hoverlay", "Toggle screen overlays.", (s, p) =>
			{
				_overlayEffectControl.Toggle();
			});
			Helper.ConsoleCommands.Add("hoffer", "Make a shrine offering: use S, M, or L.", (s, p) =>
			{
				MakeShrineOffering(Game1.player, "offer" + p[0]?.ToUpper()[0]);
			});
			Helper.ConsoleCommands.Add("hcrows", "Respawn twin crows at the shrine.", (s, p) =>
			{
				SpawnCrows(Game1.getLocationFromName(ModConsts.ShrineMapId));
			});
			Helper.ConsoleCommands.Add("hcrows2", "Respawn perched crows at the shrine.", (s, p) =>
			{
				SpawnPerchedCrows(Game1.getLocationFromName(ModConsts.ShrineMapId));
			});
			Helper.ConsoleCommands.Add("htotem", "Totem warp.", (s, p) =>
			{
				StartWarpToShrine(new Object(
					JaApi.GetObjectId("Warp Totem: Hilltop"), 1).getOne() as Object, Game1.currentLocation);
			});
			Helper.ConsoleCommands.Add("hh", "Warp to Rei's house.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, ModConsts.HouseMapId,
						5, 19, false));
			});
			Helper.ConsoleCommands.Add("hhome", "Warp to Rei's house.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, ModConsts.HouseMapId,
						5, 19, false));
			});
			Helper.ConsoleCommands.Add("hs", "Warp to Hikawa Shrine.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, ModConsts.ShrineMapId,
						39, 60, false));
			});
			Helper.ConsoleCommands.Add("hshrine", "Warp to Hikawa Shrine.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, ModConsts.ShrineMapId,
						39, 60, false));
			});
			Helper.ConsoleCommands.Add("ht", "Warp to the shrine entrance.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, "Town",
						20, 5, false));
			});
			Helper.ConsoleCommands.Add("htown", "Warp to the shrine entrance.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, "Town",
						20, 5, false));
			});
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
			SaveData = Helper.Data.ReadSaveData<ModSaveData>(
				ModConsts.SaveDataKey) ?? new ModSaveData();
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
			LoadModData();
		}

		/// <summary>
		/// Start of day
		/// </summary>
		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			_shouldCrowsSpawnToday = 
				Game1.currentSeason != "winter" && !Game1.isRaining
				|| Game1.currentSeason == "winter" && Game1.random.NextDouble() < 0.3d;

			_whatAboutCatsCanTheySpawnToday = true; // TODO: METHOD: caats spawn conditions

			_isPlayerAgencySuppressed = false;
			_isPlayerSittingDown = false;

			// TODO: CONTENT: Write and apply shrine buffs
			switch (SaveData.LastShrineBuffId)
			{
				case 0:
					break;
				case 1:
					break;
				case 2:
					break;
				case 3:
					break;
				case 4:
					break;
				case 5:
					break;
				case 6:
					break;
				case 7:
					break;
				case 8:
					break;
				case 9:
					break;
			}

			SaveData.AwaitingShrineBuff = false;
			if (SaveData.ShrineBuffCooldown > 0)
			{
				--SaveData.ShrineBuffCooldown;
			}

			if (SaveData.StoryStock <= (int) ModConsts.Progress.Started)
			{
				Log.D("Loading up bomba action listener.");
				SpaceEvents.BombExploded += HikawaBombExploded;
			}

			if (SaveData.StoryPlant >= (int) ModConsts.Progress.Started)
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
		private void OnSaved(object sender, SavedEventArgs e)
		{
			// TODO: DEBUG: Write save data
			//Helper.Data.WriteSaveData(ModConsts.SaveDataKey, SaveData);
		}
		
		private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
		{
			UnloadModData();
		}

		/// <summary>
		/// Per-frame checks
		/// </summary>
		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
			ReapplyBuff(e);
		}

		private void ReapplyBuff(UpdateTickedEventArgs e)
		{
			if (Game1.eventUp || !Context.IsWorldReady || SaveData.LastShrineBuffId < 1)
				return;

			var buff = Game1.buffsDisplay.otherBuffs.FirstOrDefault(_ => _.which == ModConsts.BuffId);
			if (buff == null)
			{
				Game1.buffsDisplay.addOtherBuff(
					buff = new Buff(
						0,
						0,
						0,
						0,
						0,
						0,
						0,
						0,
						0,
						SaveData.LastShrineBuffId == 8 ? 1 : 0,
						0,
						0,
						0,
						source: ModManifest.UniqueID,
						displaySource: i18n.Get("string.shrine.offering_accepted." + SaveData.LastShrineBuffId))
					{

					});
			}
			buff.millisecondsDuration = 50;

			switch (SaveData.LastShrineBuffId)
			{
				case 1: // Shivers () [寒]
					break;
				case 2: // Uneasy () [悪]
					break;
				case 3: // Hungry (Buff buffs) []
					break;
				case 4: // Comfort () [強]
					break;
				case 5: // Warm breeze (Loot) [金]
					break;
				case 6: // Sunlight (Health) [光]
					if (!e.IsMultipleOf(180))
						break;
					Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + 1);
					break;
				case 7: // Weight (Stamina) [心]
					if (!e.IsMultipleOf(180))
						break;
					Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + 1);
					break;
				case 8: // Wind (Speed) [風]
					break;
				case 9: // Great confidence (Luck) [幸]
					break;
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
					TryCheckForActions();

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
				Path.Combine(ModConsts.SpritesPath, ModConsts.ExtraSpritesFile + ".png"));
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
		
		/// <summary>
		/// Handles player interactions with custom mod elements.
		/// </summary>
		private void TryCheckForActions()
		{
			CheckTileAction();
			if (Game1.player.ActiveObject == null || Game1.player.isRidingHorse())
				return;
			CheckHeldObjectAction(Game1.player.ActiveObject, Game1.player.currentLocation);
		}

		private string[] GetTileProperty(Vector2 position)
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
		
		private void CheckTileAction()
		{
			var grabTile = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, 
				Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;
			if (!Utility.tileWithinRadiusOfPlayer(
				(int)grabTile.X, (int)grabTile.Y, 1, Game1.player))
				grabTile = Game1.player.GetGrabTile();

			CheckTileAction(grabTile);
		}

		private void CheckTileAction(Vector2 position)
		{
			var where = Game1.currentLocation;
			var property = GetTileProperty(position);

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
					if (SaveData.AtlantisInterlude)
					{
						where.createQuestionDialogue(
							i18n.Get("string.shrine.atlantis_prompt"),
							new[]
							{
								new Response("atlantis", i18n.Get("dialogue.response.ready")),
								new Response("cancel", i18n.Get("dialogue.response.later"))
							},
							delegate(Farmer farmer, string answer)
							{
								if (answer == "cancel")
									return;
								StartMission();
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
					where.createQuestionDialogue(
						i18n.Get("string.shrine.offering_prompt"), 
						new[]
						{
							new Response("offerS", $"{ModConsts.OfferingCostS}g"),
							new Response("offerM", $"{ModConsts.OfferingCostM}g"),
							new Response("offerL", $"{ModConsts.OfferingCostL}g"),
							new Response("cancel", i18n.Get("dialogue.response.cancel"))
						}, 
						MakeShrineOffering);
					break;

				// Interactions with the Ema stand at the Shrine
				case ModConsts.ActionEma:
					Log.W("ActionEma!");
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
			}
		}

		/// <summary>
		/// Method lifted from StardewValley.Object.performUseAction(Farmer who): Object.cs:2723 from ILSpy
		/// </summary>
		public void CheckHeldObjectAction(Object o, GameLocation where)
		{
			if (!Game1.player.CanMove || o.isTemporarilyInvisible || _isPlayerAgencySuppressed)
				return;

			if (o.Name != null && o.Name == "Warp Totem: Shrine")
			{
				if (!Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack
				    && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value)
				{
					StartWarpToShrine(o, where);
				}
			}
		}

		/// <summary>
		/// Handles player using custom tools and weapons.
		/// </summary>
		public void TryCheckForToolUse(Tool tool)
		{
			if (tool.Name == "Crystal Moon Wand")
			{
				Log.D("Wand: Start of anim");
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
							Log.D("Wand: End of anim");
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
			}
		}
		
		#endregion

		#region Manager Methods

		/// <summary>
		/// Adds unique elements to maps on entry.
		/// </summary>
		public void SetUpLocationCustomFlair(GameLocation where)
		{
			Log.D($"Warped to {where.Name}, adding flair.");
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
					else if (SaveData.StoryMist == (int)ModConsts.Progress.Started)
					{
						// Eerie effects
						_overlayEffectControl.Enable(GameObjects.OverlayEffectControl.Effect.Mist);
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
					var point = Point.Zero;

					// Rei's custom door
					const int doorMarkerIndex = 32;
					point = new Point(7, 12);

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
							Log.E($"Failed to find a door at marker point {point.ToString()}");
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
								"winter" => 217
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
							"winter" => 247
						};
						layer.Tiles[3, 15].TileIndex = index;
						layer.Tiles[3, 16].TileIndex = index + rowIncrement;
					}
					else
					{
						Log.E($"Door marker not found at point {point.ToString()}");
					}
					break;
				}
				
				// Player's farm
				case "Farm":
				{
					if (SaveData.StoryPlant == (int)ModConsts.Progress.Started)
					{
						// Plant
					}

					break;
				}
				
				// Doors
				case ModConsts.CorridorMapId:
				{
					// Haze effect
					_overlayEffectControl.Enable(GameObjects.OverlayEffectControl.Effect.Haze);

					break;
				}
				
				// Gap
				case ModConsts.NegativeMapId:
				{
					// Overlaid crystals
					// Obscuring fog around player

					break;
				}
			}
		}

		/// <summary>
		/// Attempts to add twin crows to the map as critters.
		/// </summary>
		private static void SpawnCrows(GameLocation where)
		{
			if (!where.IsOutdoors)
				return;

			const int retries = 25;
			for (var attempts = 0; attempts < retries; ++attempts)
			{
				// Identify two separate nearby spawn positions for the crows around the map's middle
				var target = new Location(
					Game1.random.Next(19, 62), // X position
					Game1.random.Next(22, 57)); // Y position
				var phobos = Location.Origin;
				var deimos = Location.Origin;
				Log.D($"New target: {target.ToString()} -->");
				for (var y = -1; y < 1; ++y)
				{
					for (var x = -1; x < 1; ++x)
					{
						Log.D($"Checking phobos [{target.X + x}, {target.Y + y}]...");
						Log.D($"Checking deimos [{target.X - x}, {target.Y - y}]...");

						if (where.isTilePassable(new Location(target.X + x, target.Y + y), Game1.viewport))
						{
							phobos = new Location(target.X + x, target.X + y);
							Log.D($"Phobos tile OK: {phobos.ToString()}");
						}

						if (where.isTilePassable(new Location(target.X - x, target.Y - y), Game1.viewport))
						{
							deimos = new Location(target.X - x, target.X - y);
							Log.D($"Deimos tile OK: {deimos.ToString()}");
						}

						if (phobos == deimos && phobos != Location.Origin)
						{
							Log.D("Skipping cluster, non-default Phobos is equal to Deimos");
							break;
						}
						if (phobos == deimos)
						{
							Log.D("Skipping tile, Phobos and Deimos are equal");
							continue;
						}
						if (phobos == Location.Origin || deimos == Location.Origin)
						{
							Log.D($"Skipping tile, Phobos ({phobos.ToString()}) or Deimos ({deimos.ToString()}) is default");
							continue;
						}

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

		private void StartMission()
		{
			Log.W("Start Mission");

			// If a mission requires that the player has a slot for an item eg. Wand, Mirror, and they don't have one, break out
			//if (Game1.player.freeSpotsInInventory() < 3 && SaveData.StoryDoors > (int) ModConsts.Progress.Started || SaveData.StoryGap)

			// TODO: CONTENT: Write and implement mission data
			if (SaveData.StoryPlant == (int) ModConsts.Progress.Started)
			{

			}
			else if (SaveData.StoryDoors == (int) ModConsts.Progress.Started)
			{

			}
			else if (SaveData.StoryGap == (int) ModConsts.Progress.Started)
			{

			}
			else if (SaveData.StoryTower == (int) ModConsts.Progress.Started)
			{

			}
		}

		private void EndMission()
		{
			Log.W("End Mission");
			
			// TODO: CONTENT: Write and implement mission data
			if (SaveData.StoryPlant == (int) ModConsts.Progress.Started)
			{

			}
			else if (SaveData.StoryDoors == (int) ModConsts.Progress.Started)
			{

			}
			else if (SaveData.StoryGap == (int) ModConsts.Progress.Started)
			{

			}
			else if (SaveData.StoryTower == (int) ModConsts.Progress.Started)
			{

			}
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
			var coords = ModConsts.ShrineDefaultWarpPosition;
			Game1.warpFarmer(ModConsts.ShrineMapId, coords.X, coords.Y, false);
			Game1.fadeToBlackAlpha = 0.99f;
			Game1.screenGlow = false;
			Game1.player.temporarilyInvincible = false;
			Game1.player.temporaryInvincibilityTimer = 0;
			Game1.displayFarmer = true;
		}
		
		private void MakeShrineOffering(Farmer farmer, string answer)
		{
			if (!answer.StartsWith("offer") 
			    || !answer.EndsWith("S") && !answer.EndsWith("M") && !answer.EndsWith("L"))
				return;

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
			}

			Log.D($"Shrine: Rolled {roll} (with luck at{Game1.player.DailyLuck}) for offering {tribute}g.");
			
			farmer.Money -= tribute;
			SaveData.ShrineBuffCooldown = cooldown;
			var whichBuff = (int)Math.Floor(roll);
			if (whichBuff < 0) whichBuff = 0;
			if (whichBuff > 9) whichBuff = 9;
			SaveData.LastShrineBuffId = whichBuff;

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
		
		private void HikawaBombExploded(object sender, EventArgsBombExploded e)
		{
			var distance = Vector2.Distance(ModConsts.StoryStockPosition, e.Position);
			if (Game1.currentLocation.Name == "Town" && distance <= e.Radius * 2f)
			{
				Log.D("TACTICAL NUKE",
					Config.DebugMode);
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
			if (Config.DebugShowRainInTheNight
			    || SaveData.StoryPlant == (int) ModConsts.Progress.Started && Game1.weatherForTomorrow == Game1.weather_rain)
			{
				Log.D("Rain on the horizon",
					Config.DebugMode);
				e.NightEvent = new GameObjects.RainInTheNight();
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
			
			// TODO: Test buff #7: Hungry

			if (SaveData.LastShrineBuffId == 7)
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