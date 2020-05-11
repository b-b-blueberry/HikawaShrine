using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using xTile.Dimensions;
using xTile.ObjectModel;

using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

using SpaceCore.Events;

using Hikawa.Core;
using Hikawa.GameObjects;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa
{
	public class ModEntry : Mod
	{
		internal static ModEntry Instance;
		internal ModSaveData SaveData;
		internal IJsonAssetsApi JaApi;
		private readonly OverlayEffectControl _overlayEffectControl = new OverlayEffectControl();

		internal Config Config;
		internal ITranslationHelper i18n => Helper.Translation;
		
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

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<Config>();

			//helper.Content.AssetEditors.Add(new Editors.TestEditor(helper));
			helper.Content.AssetEditors.Add(new Editors.WorldEditor(helper));
			helper.Content.AssetEditors.Add(new Editors.EventEditor(helper));
			helper.Content.AssetEditors.Add(new Editors.ArcadeEditor(helper));

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.GameLoop.DayEnding += OnDayEnding;
			helper.Events.GameLoop.Saved += OnSaved;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			
			helper.Events.Player.Warped += OnWarped;
			helper.Events.Input.ButtonPressed += OnButtonPressed;

			SpaceEvents.ChooseNightlyFarmEvent += HikawaFarmEvents;

			AddConsoleCommands();
			if (Config.DebugMode)
			{
				// Add texture render tests
				//helper.Events.Display.Rendering += OnRendering;
				var textureName = Path.Combine(ModConsts.AssetsDirectory, ModConsts.SpritesDirectory,
					ModConsts.ExtraSpritesFile + ".png");
				_texture = Instance.Helper.Content.Load<Texture2D>(textureName);
			}
		}

		private void AddConsoleCommands()
		{
			if (!Config.DebugMode)
				return;

			// Add debug commands
			Helper.ConsoleCommands.Add("bharcade", "Start arcade game: use START, TITLE, RESET", (s, p) =>
			{
				if (p[0].ToLower() == "start")
					Game1.currentMinigame = new ArcadeGunGame();
				else if (Game1.currentMinigame != null && Game1.currentMinigame is ArcadeGunGame
				                                       && Game1.currentMinigame.minigameId()
				                                       == ModConsts.ArcadeMinigameId)
				{
					if (p[0].ToLower() == "title")
						((ArcadeGunGame)(Game1.currentMinigame)).ResetAndReturnToTitle();
					else if (p[0].ToLower() == "reset")
						((ArcadeGunGame)(Game1.currentMinigame)).ResetGame();
				}
			});
			Helper.ConsoleCommands.Add("bhoverlay", "Toggle screen overlays.", (s, p) =>
			{
				_overlayEffectControl.Toggle();
			});
			Helper.ConsoleCommands.Add("bhoffer", "Make a shrine offering: use S, M, or L.", (s, p) =>
			{
				MakeShrineOffering(Game1.player, "offer" + p[0]?.ToUpper()[0]);
			});
			Helper.ConsoleCommands.Add("bhcrows", "Respawn twin crows at the shrine.", (s, p) =>
			{
				SpawnCrows(Game1.getLocationFromName(ModConsts.ShrineMapId));
			});
			Helper.ConsoleCommands.Add("bhtotem", "Totem warp.", (s, p) =>
			{
				StartWarpToShrine(new StardewValley.Object(
					JaApi.GetObjectId("Warp Totem: Hilltop"), 1).getOne() as StardewValley.Object, Game1.currentLocation);
			});
			Helper.ConsoleCommands.Add("bhhome", "Warp to Rei's house.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, ModConsts.HouseMapId,
						5, 19, false));
			});
			Helper.ConsoleCommands.Add("bhshrine", "Warp to Hikawa Shrine.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, ModConsts.ShrineMapId,
						39, 60, false));
			});
			Helper.ConsoleCommands.Add("bhtown", "Warp to the shrine entrance.", (s, p) =>
			{
				Game1.player.warpFarmer(
					new Warp(0, 0, "Town",
						20, 5, false));
			});
		}

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
			SaveData = Helper.Data.ReadSaveData<ModSaveData>(
				ModConsts.SaveDataKey) ?? new ModSaveData();
		}

		/// <summary>
		/// Start of day
		/// </summary>
		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
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

			// todo: banana world effects
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

			// todo: shrine buffs take effect
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
			// todo: write save data
			//Helper.Data.WriteSaveData(ModConsts.SaveDataKey, SaveData);
		}

		/// <summary>
		/// Per-frame checks
		/// </summary>
		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
		}
		
		/// <summary>
		/// Location changed
		/// </summary>
		private void OnWarped(object sender, WarpedEventArgs e)
		{
			if (e.OldLocation.Name.Equals(e.NewLocation.Name)) return;

			if (_overlayEffectControl.IsEnabled())
				_overlayEffectControl.Disable();

			SetUpLocationSpecificFlair(Game1.currentLocation);
		}

		/// <summary>
		/// Button check
		/// </summary>
		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (Game1.eventUp && !Game1.currentLocation.currentEvent.playerControlSequence
			    || Game1.currentBillboard != 0 || Game1.activeClickableMenu != null || Game1.menuUp || Game1.nameSelectUp
			    || Game1.IsChatting || Game1.dialogueTyping || Game1.dialogueUp
			    || Game1.player.UsingTool || Game1.pickingTool || Game1.numberOfSelectedItems != -1
			    || !Game1.player.CanMove || Game1.fadeToBlack)
				return;
			
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
		
		private void CheckTileAction()
		{
			var grabTile = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, 
				Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;
			if (!Utility.tileWithinRadiusOfPlayer(
				(int)grabTile.X, (int)grabTile.Y, 1, Game1.player))
				grabTile = Game1.player.GetGrabTile();
			var tile = Game1.currentLocation.map.GetLayer("Buildings").PickTile(
				new Location(
					(int)grabTile.X * Game1.tileSize, 
					(int)grabTile.Y * Game1.tileSize), 
				Game1.viewport.Size);
			var action = (PropertyValue)null;
			tile?.Properties.TryGetValue("Action", out action);
			if (action == null) return;

			var strArray = ((string)action).Split(' ');
			var args = new string[strArray.Length - 1];
			Array.Copy(
				strArray, 1, 
				args, 0, 
				args.Length);

			var where = Game1.currentLocation;
			switch (strArray[0])
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
					break;

				// Trying to enter the Shrine Hall front doors
				case ModConsts.ActionShrineHall:
					break;
			}
		}

		/// <summary>
		/// Method lifted from StardewValley.Object.performUseAction(Farmer who): Object.cs:2723 from ILSpy
		/// </summary>
		public void CheckHeldObjectAction(StardewValley.Object o, GameLocation where)
		{
			if (!Game1.player.canMove || o.isTemporarilyInvisible)
				return;

			if (o.Name != null && o.Name == "Warp Totem: Hilltop")
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
		public void SetUpLocationSpecificFlair(GameLocation where)
		{
			Log.D($"Warped to {where.Name}, setting up flair.");
			switch (where.Name)
			{
				case ModConsts.ShrineMapId:
				{
					// Hikawa Shrine

					if (SaveData.StoryMist == (int)ModConsts.Progress.Started)
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
							Game1.changeMusicTrack("communityCenter");
					}
					else if (!Game1.isRaining)
					{
						// Crows on regular days

						if (Game1.timeOfDay < 1200)
						{
							// Spawn crows as critters
							SpawnCrows(where);
						}
						else if (!Game1.isDarkOut())
						{
							// Add crows as temp sprites
							var roll = Game1.random.NextDouble();
							if (roll < 0.3)
							{
								
							} else if (roll < 0.7)
							{

							}
							else
							{

							}
						}
					}

					break;
				}

				case ModConsts.HouseMapId:
				{
					const int doorMarkerIndex = 32;
					var point = new Point(7, 12);

					if (where.Map.GetLayer("Buildings").Tiles[point.X, point.Y].TileIndex == doorMarkerIndex)
					{
						Log.D($"Marker found at {point.ToString()}");
						if (where.interiorDoors.ContainsKey(point))
						{
							Log.D($"Door found at {point.ToString()}");
							var interiorDoor = where.interiorDoors.Doors.First (door => door.Position == point);
							if (interiorDoor != null)
							{
								var texture = where.Map.GetTileSheet(ModConsts.IndoorsSpritesFile).ImageSource;
								Log.D($"Tilesheet image source: {texture}");
								var sprite = new TemporaryAnimatedSprite(
									texture,
									new Microsoft.Xna.Framework.Rectangle(0, 512, 64, 48),
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
						}
						else
						{
							Log.E($"Failed to find a door at marker point {point.ToString()}");
						}
					}
					else
					{
						Log.E($"Door marker not found at point {point.ToString()}");
					}
					break;
				}

				case "Farm":
				{
					// Player's farm

					if (SaveData.StoryPlant == (int)ModConsts.Progress.Started)
					{
						// Plant
					}

					break;
				}

				case ModConsts.CorridorMapId:
				{
					// Doors

					// Haze effect
					_overlayEffectControl.Enable(OverlayEffectControl.Effect.Haze);

					break;
				}

				case ModConsts.NegativeMapId:
				{
					// Gap

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
			var rand = new Random();
			const int timeout = 5;
			for (var attempts = 0; attempts < timeout; ++attempts)
			{
				// Identify two separate nearby spawn positions for the crows around the map's middle
				var w = where.Map.Layers[0].LayerWidth;
				var h = where.Map.Layers[0].LayerHeight;
				var vTarget = new Location(
					rand.Next(w / 4, w / 4 * 3),
					rand.Next(h / 4, h / 4 * 3));
				var phobos = Location.Origin;
				var deimos = Location.Origin;
				for (var y = -1; y < 1; ++y)
				{
					for (var x = -1; x < 1; ++x)
					{
						if (where.isTilePassable(
							new Location(vTarget.X + x, vTarget.Y + y), Game1.viewport)) 
							phobos = new Location(vTarget.X + x, vTarget.X + y);
						if (where.isTilePassable(
							new Location(vTarget.X - x, vTarget.Y - y), Game1.viewport))
							deimos = new Location(vTarget.X - x, vTarget.X - y);
						if (phobos == deimos && phobos != Location.Origin)
							break;
						if (phobos == deimos || phobos == Location.Origin || deimos == Location.Origin)
							continue;
						SpawnCrows(where, phobos, deimos);
						return;
					}
				}
			}
			Log.D($"Failed to add crows after {timeout} attempts.");
		}

		/// <summary>
		/// Attempts to add twin crows to the map as critters.
		/// </summary>
		private static void SpawnCrows(GameLocation where, Location phobos, Location deimos)
		{
			Log.W($"Adding crows at {phobos.ToString()} and {deimos.ToString()}");
			where.addCritter(new Crow(phobos.X, phobos.Y));
			where.addCritter(new Crow(deimos.X, deimos.Y));
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

			// todo: mission data
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

			// todo: mission data
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

		public void StartWarpToShrine(StardewValley.Object o, GameLocation where) {
			var multiplayer = Helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
			var index = JaApi.GetObjectId(o.Name);

			Game1.player.jitterStrength = 1f;
			where.playSound("warrior");
			Game1.player.faceDirection(2);
			Game1.player.CanMove = false;
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
				// todo: all the cool things

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
							whichSound = "cm:blueberry.hikawa.rainsound_wom:rainsound";
						else if (whichBuff < 4)
							whichSound = "cm:blueberry.hikawa.rainsound_ooh:rainsound";
						else if (whichBuff < 7)
							whichSound = "cm:blueberry.hikawa.rainsound_ahh:rainsound";
						Game1.currentLocation.localSound(whichSound);
						Game1.drawObjectDialogue(i18n.Get("string.shrine.offering_accepted." + whichBuff));
					},
					true)
			});
		}
		
		private static string GetContentPackId(string name)
		{
			return Regex.Replace(ModConsts.ContentPackPrefix + name,
				"[^a-zA-Z0-9_.]", "");
		}

		public Dictionary<ISalable, int[]> GetSouvenirShopStock(NPC who)
		{
			var stock = new Dictionary<ISalable, int[]>();

			// todo: compile the shop stock for whichever events seen, whatever time of year, whoever is behind the counter, story progress

			if (who.Name == ModConsts.ReiNpcId)
			{
				foreach (var hat in JaApi.GetAllHatsFromContentPack(GetContentPackId("Hats")))
				{
					stock.Add(new StardewValley.Object(JaApi.GetHatId(hat), 1), new[] {1150, 1});
				}
			}

			return stock;
		}
		
		private void HikawaBombExploded(object sender, EventArgsBombExploded e)
		{
			var distance = Vector2.Distance(ModConsts.StoryStockPosition, e.Position);
			if (Game1.currentLocation.Name == "Town" && distance <= e.Radius * 2f)
			{
				Log.D("TACTICAL NUKE");
				Game1.playSound("reward");

				Game1.currentLocation.currentEvent = new Event(Helper.Content.Load<string>(
					Path.Combine(ModConsts.AssetsDirectory, ModConsts.EventsPath)));

				// todo: invalidate Town at the end of the event
				//Helper.Content.InvalidateCache(@"Maps/Town");

				SpaceEvents.BombExploded -= HikawaBombExploded;
			}
		}

		private void HikawaFarmEvents(object sender, EventArgsChooseNightlyFarmEvent e)
		{
			Log.D($"HikawaFarmEvents: (vanilla event: {e.NightEvent != null})");
			if (e.NightEvent != null)
				Log.D($"(vanilla event type: {e.NightEvent.GetType().FullName})");
			if (Config.DebugShowRainInTheNight
			    || SaveData.StoryPlant == (int) ModConsts.Progress.Started && Game1.weatherForTomorrow == Game1.weather_rain)
			{
				Log.D("Rain on the horizon");
				e.NightEvent = new RainInTheNight();
			}
		}

		private void HikawaFoodEaten(object sender, EventArgs e)
		{
			if (Game1.player.itemToEat.Name.StartsWith("Dark Fruit"))
			{
				++SaveData.BananaBunch;
				if (SaveData.BananaBunch > ModConsts.BananaBegins)
				{
					var foodEnergy = Game1.player.itemToEat.staminaRecoveredOnConsumption();
					Game1.player.Stamina += Math.Max(foodEnergy * 3, foodEnergy / (SaveData.BananaBunch * foodEnergy) * foodEnergy);
				}
			}
			else
			{
				if (SaveData.BananaBunch > ModConsts.BananaBegins)
				{
					var foodEnergy = Game1.player.itemToEat.staminaRecoveredOnConsumption();
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
				if (!Game1.IsMasterGame)
					return;

				var f = Game1.getLocationFromName("Farm") as Farm;
				if (f.terrainFeatures.ContainsKey(position))
					f.terrainFeatures.Remove(position);
				f.terrainFeatures.Add(position, new HikawaBanana());
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
				var mapId = "";
				if (false)
				{
					mapId = "Town";
					Game1.player.warpFarmer(
						new Warp(0, 0, mapId,
							20, 5, true));
				}
				else if (false)
				{
					mapId = ModConsts.HouseMapId;
					Game1.player.warpFarmer(
						new Warp(0, 0, mapId,
							5, 19, false));
				}
				else
				{
					mapId = ModConsts.ShrineMapId;
					Game1.player.warpFarmer(
						new Warp(0, 0, mapId,
							39, 60, false));
				}
				Log.D($"Pressed {btn} : Warping to {mapId}",
					Config.DebugMode);
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