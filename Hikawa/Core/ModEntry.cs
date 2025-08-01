using HarmonyLib; // el diavolo nuevo
using Hikawa.Data;
using Hikawa.Objects.Critters;
using Hikawa.Objects.Items;
using Hikawa.Objects.Items.Data;
using Hikawa.Objects.Locations;
using Hikawa.Objects.Menus;
using Hikawa.Volleyball;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Object = StardewValley.Object;

namespace Hikawa
{
	public class ModEntry : Mod
	{
		public class ModState
		{
			// World
			public float PreciseTime;

			// Animations
			public int AnimationExtraInt;
			public float AnimationExtraFloat;
			public int AnimationTimer;
			public int AnimationStage;
			public bool AnimationFlag;
			public Vector2 AnimationTarget;

			// Others
			public Match3.Match3Game Match3;

			// Vortex
			public Point WarpFrom;
			public Point WarpTo;
			public string WarpLocation;
			public int WarpCount;
		}

		// Mod objects
		// main
		public static PerScreen<ModState> State { get; private set; }
		public static ModEntry Instance { get; private set; }
		public static Config Config { get; private set; }
		public static SaveData SaveData { get; private set; }
		public static ModData ModData { get; private set; }
        public static ITranslationHelper I18n => ModEntry.Instance.Helper.Translation;

        // sprites
        public static Texture2D Sprites { get; private set; }
        public static Texture2D OutdoorsSprites { get; private set; }

		// data
		public static Lazy<BowsDataAsset> BowsData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<BowsDataAsset>(AssetManager.BowsDataAssetName));
		public static Lazy<BugsDataAsset> BugsData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<BugsDataAsset>(AssetManager.BugsDataAssetName));
		public static Lazy<CrowTradeRulesDataAsset> CrowTradeRulesData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<CrowTradeRulesDataAsset>(AssetManager.CrowTradeRulesDataAssetName));
		public static Lazy<DecorSpawnsDataAsset> DecorSpawnsData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<DecorSpawnsDataAsset>(AssetManager.DecorSpawnsDataAssetName));
		public static Lazy<KitesDataAsset> KitesData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<KitesDataAsset>(AssetManager.KiteDataAssetName));
		public static Lazy<ShrineTreesDataAsset> ShrineTreesData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<ShrineTreesDataAsset>(AssetManager.ShrineTreesDataAssetName));
		public static Lazy<ShrubsDataAsset> ShrubsData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<ShrubsDataAsset>(AssetManager.ShrubsDataAssetName));

		// fonts
		public static Lazy<SpriteFont> Italics = new(() => ModEntry.Instance.Helper.GameContent.Load<SpriteFont>(AssetManager.ItalicsFontAssetName));
		public static Lazy<SpriteFont> Handwriting = new(() => ModEntry.Instance.Helper.GameContent.Load<SpriteFont>(Path.Combine("Fonts", "SpriteFont1.ja-JP")));

		// modules
		public static Modules.OverlayEffectControl OverlayEffectControl { get; private set; }


		public override void Entry(IModHelper helper)
		{
			ModEntry.Instance = this;
			ModEntry.Config = helper.ReadConfig<Config>();
			ModEntry.State = new PerScreen<ModState>(() => new());
			ModEntry.OverlayEffectControl = new();
			Modules.DialogueEffects.State = new PerScreen<Modules.DialogueEffects.DialogueEffectsState>(() => new());

			helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
		}

		private void Init()
		{
			if (!Interfaces.Interfaces.Init(manifest: this.ModManifest, registry: this.Helper.ModRegistry))
			{
				Log.E("Mod will not be loaded.");
				return;
			}

			// continue init after setup delay
			this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.OnDelayAfterGameLaunched;
		}

		private void InitLate()
		{
			// common assets
			ModEntry.ModData = ModEntry.Instance.Helper.GameContent.Load<ModData>(AssetManager.DataAssetName);
			ModEntry.Sprites = ModEntry.Instance.Helper.GameContent.Load<Texture2D>(AssetManager.ExtraSpritesAssetName);
            ModEntry.OutdoorsSprites = ModEntry.Instance.Helper.GameContent.Load<Texture2D>(AssetManager.OutdoorsSpritesAssetName);

			if (ModEntry.ModData is null)
			{
				Log.E("Mod data could not be loaded.");
				return;
			}

			// evil doings
			Harmony harmony = new(id: this.Helper.ModRegistry.ModID);
			harmony.PatchAll();

			// criminal activity
			this.MangleTranslations();

			// lawful activity
			ItemRegistry.AddTypeDefinition(new BowItemDataDefinition());
			ItemRegistry.AddTypeDefinition(new KiteItemDataDefinition());
            ItemRegistry.AddTypeDefinition(new BugToolItemDataDefinition());
            ItemRegistry.AddTypeDefinition(new BugFurnitureItemDataDefinition());
			this.RegisterMapActions();
			this.RegisterEventCommands();

			// modules
			Modules.DialogueEffects.Init();

			// dev tests
			if (ModEntry.Config.DebugMode)
			{
				ConsoleCommands.Add(this.Helper, prefix: ModEntry.ModData.ConsoleCommandPrefix);
				Modules.SpriteTest.Init(helper: this.Helper);
			}

			// more game events when we're confident everything loaded
			this.Helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
			this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
			this.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
			this.Helper.Events.GameLoop.DayEnding += this.OnDayEnding;
			this.Helper.Events.GameLoop.Saving += this.OnSaving;
			this.Helper.Events.Player.Warped += this.OnWarped;
			this.Helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
			this.Helper.Events.Display.RenderedStep += this.OnRenderedStep;
			this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
			this.Helper.Events.Content.AssetRequested += AssetManager.TryEdit;
		}

		private void OnRenderedStep(object sender, RenderedStepEventArgs e)
		{
			if (Game1.currentLocation?.critters is null)
				return;

			bool isWorld = e.Step is StardewValley.Mods.RenderSteps.World_Sorted;
			bool isAlwaysFront = e.Step is StardewValley.Mods.RenderSteps.World_AlwaysFront;

			if (isWorld || isAlwaysFront)
				foreach (LightTile light in Game1.currentLocation.critters.Where(c => c is LightTile))
					if (light.Data.DrawAbove != isWorld)
						light.DrawLightTile(b: e.SpriteBatch);

			if (isAlwaysFront)
			{
				foreach (Kite kite in Game1.currentLocation.Objects.Values.Where(o => o is Kite))
					kite.drawAboveFrontLayer(e.SpriteBatch, (int)kite.TileLocation.X, (int)kite.TileLocation.Y);
				//if (Game1.player.CurrentItem is Kite kite1)
				//	kite1.drawAboveFrontLayer(e.SpriteBatch, Game1.player.TilePoint.X, Game1.player.TilePoint.Y);
			}
		}

		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
			ModEntry.State.Value.PreciseTime = Utils.GetPreciseTimeOfDay(Game1.timeOfDay);
			Modules.DialogueEffects.Update(e.Ticks);
		}

		/// <summary>
		/// Replaces mod translation entries with those from the CP component.
		/// </summary>
		private void MangleTranslations()
		{
			(object instance, object files) GetTranslations(string uniqueId)
			{
				Type SCore = Type
					.GetType("StardewModdingAPI.Framework.SCore, StardewModdingAPI");
				object SCoreInstance = SCore
					.GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static)
					.GetGetMethod(true)
					.Invoke(null, null);
				object SModRegistry = SCore
					.GetField("ModRegistry", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(SCoreInstance);
				object SModMetadata = SModRegistry
					.GetType()
					.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance)
					.Invoke(SModRegistry, [uniqueId]);
				object directoryPath = SModMetadata
					.GetType()
					.GetProperty("DirectoryPath", BindingFlags.Public | BindingFlags.Instance)
					.GetGetMethod()
					.Invoke(SModMetadata, null);

				List<string> errors = [];
				object SCoreTranslationFiles = SCore
					.GetMethod("ReadTranslationFiles", BindingFlags.NonPublic | BindingFlags.Instance)
					.Invoke(SCoreInstance, [Path.Combine((string)directoryPath, "i18n"), errors]);
				object SModTranslations = SModMetadata
					.GetType()
					.GetProperty("Translations", BindingFlags.Public | BindingFlags.Instance)
					.GetGetMethod()
					.Invoke(SModMetadata, null);
				return (SModTranslations, SCoreTranslationFiles);
			}

			(object instance, object files) coreTranslations = GetTranslations(this.ModManifest.UniqueID);
			(object instance, object files) contentTranslations = GetTranslations(ModConsts.ContentModID);

			// evil plans
			coreTranslations.instance
				.GetType()
				.GetMethod("SetTranslations", BindingFlags.NonPublic | BindingFlags.Instance)
				.Invoke(coreTranslations.instance, [contentTranslations.files]);
		}

		#region Game Events

		/// <summary>
		/// Pre-game
		/// </summary>
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			this.Init();
		}

		/// <summary>
		/// Post-launch
		/// </summary>
		private void OnDelayAfterGameLaunched(object sender, OneSecondUpdateTickedEventArgs e)
		{
			this.Helper.Events.GameLoop.OneSecondUpdateTicked -= this.OnDelayAfterGameLaunched;

			this.InitLate();
		}

		/// <summary>
		/// Post-save unloaded
		/// </summary>
		private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
		{
			ModEntry.SaveData = null;
		}

		/// <summary>
		/// Pre-start of day
		/// </summary>
		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			ModEntry.SaveData = this.Helper.Data.ReadSaveData<SaveData>(ModEntry.ModData.SaveDataKey) ?? new SaveData();
			Modules.DialoguePicker.LoadData();
		}

		/// <summary>
		/// Start of day
		/// </summary>
		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			if (Context.IsMainPlayer)
			{
                ModEntry.Instance.Helper.GameContent.InvalidateCache(ModEntry.OutdoorsSprites.Name);

				Utils.AddBugProperties();
            }
		}

		/// <summary>
		/// End of day
		/// </summary>
		private void OnDayEnding(object sender, DayEndingEventArgs e)
		{
			if (Context.IsMainPlayer)
			{
				Utils.ClearBugProperties();
			}
		}

		/// <summary>
		/// Post-end of day
		/// </summary>
		private void OnSaving(object sender, SavingEventArgs e)
		{
			Modules.DialoguePicker.SaveData();
		}

		/// <summary>
		/// Location changed
		/// </summary>
		private void OnWarped(object sender, WarpedEventArgs e)
		{
			if (e.OldLocation.Name != e.NewLocation.Name)
			{
				if (e.NewLocation is Town town)
				{
					Utils.ApplyCustomSharedMapProperties(town);
				}

				if (e.OldLocation.Name.StartsWith(ModEntry.ModData.ContentPrefix))
				{
					// Handle warps out of mod locations
					Game1.freezeControls = false;
				}

				if (e.OldLocation is VolleyballLocation volleyball)
				{
					// Remove volleyball map
					Game1.removeLocationFromLocationLookup(volleyball.NameOrUniqueName);
					Game1.locations.Remove(volleyball);
				}

				// Reset generic handlers
				if (ModEntry.OverlayEffectControl.IsEnabled())
					ModEntry.OverlayEffectControl.Disable();
			}
		}

		/// <summary>
		/// Button check
		/// </summary>
		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (Game1.currentLocation is VolleyballLocation vbl)
			{
				vbl.HandleInput(e.Button);
			}

			if (Utils.IsPlayerAgencyLost())
				return;

			if (Game1.player.ActiveObject is null
				|| Game1.player.ActiveObject.isTemporarilyInvisible
				|| Game1.currentLocation is null
				|| Game1.currentLocation.currentEvent is not null
				|| Utils.IsPlayerSwimming() || Game1.player.isRidingHorse() || Game1.isFestival())
				return;

			this.CheckHeldObjectAction(Game1.player.ActiveObject, Game1.player.currentLocation, e.Button);
		}
		
		#endregion

		#region Map Actions

		public void RegisterMapActions()
		{
			Dictionary<string, Func<GameLocation, string[], Farmer, Point, bool>> tileActions = new()
			{
				{
					ModEntry.ModData.ActionShrineShop, (GameLocation where, string[] args, Farmer who, Point tile) =>
					{
						// Using the Shrine souvenir shop
						if (where is Shrine shrine && shrine.GetShopPerson() is NPC npc)
						{
							var dialogue = npc.TryGetDialogue("shop_main");
							npc.setNewDialogue(dialogue, add: true, clearOnMovement: true);
							Game1.drawDialogue(npc);
							return true;
						}
						return false;
					}
				},
				{
					ModEntry.ModData.ActionShrineOffering, (GameLocation where, string[] args, Farmer who, Point tile) =>
					{
						// Using the Shrine offertory box
						Utils.CreateInspectThenQuestionDialogue(
							[
								ModEntry.I18n.Get("world.shrine.offer.inspect"),
								ModEntry.I18n.Get($"world.shrine.offer.prompt")
							],
							[
								new Response("offer_yes", ModEntry.I18n.Get("ui.menu.yes")),
								new Response("offer_no", ModEntry.I18n.Get("ui.menu.no"))
							]);
						return true;
					}
				},
				{
					ModEntry.ModData.ActionCrowTrade, (GameLocation where, string[] args, Farmer who, Point tile) =>
					{
						// Interactions with the crow trade tile at the Shrine
						return where is Shrine shrine && shrine.HandleCrowTradeAction(who);
					}
				},
				{
					ModEntry.ModData.ActionEma, (GameLocation where, string[] args, Farmer who, Point tile) =>
					{
						// Interactions with the Ema stand at the Shrine
						Game1.activeClickableMenu = new EmaMenu();
						return true;
					}
				},
				{
					ModEntry.ModData.ActionShrineHall, (GameLocation where, string[] args, Farmer who, Point tile) =>
					{
						// Trying to enter the Shrine Hall front doors
						return true;
					}
				},
				{
					ModEntry.ModData.ActionLockbox, (GameLocation where, string[] args, Farmer who, Point tile) =>
					{
						// Lockbox
						return true;
					}
				},
				{
					ModEntry.ModData.ActionWardrobe, (GameLocation where, string[] args, Farmer who, Point tile) =>
					{
						// Wardrobe
						// Offer to toggle seasonal outfits on Hikawa characters
						Game1.playSound("doorCreak");
						Game1.freezeControls = true;
						Game1.delayedActions.Add(new DelayedAction(300, () =>
						{
							Game1.freezeControls = false;
							Utils.CreateInspectThenQuestionDialogue(
								[
									I18n.Get("world.house.wardrobe", new {season = Game1.CurrentSeasonDisplayName}),
									I18n.Get($"world.house.wardrobe.{(false ? "disable" : "enable")}")
								],
								[
									new Response("wardrobe_yes", I18n.Get("ui.menu.yes")),
									new Response("wardrobe_no", I18n.Get("ui.menu.no"))
								]);
						}));
						return true;
					}
				},
				{
					ModEntry.ModData.ActionVortex, (GameLocation where, string[] args, Farmer who, Point tile) =>
					{
						// Vortex warps
						if (where is Vortex && args.Length > 2 && int.TryParse(args[1], out int toX) && int.TryParse(args[2], out int toY))
						{
							Point toTile = new Point(toX, toY);
							string toLocation = args.Length > 3 ? args[3] : null;
							Vortex.TouchVortexWarp(tile, toTile, toLocation);
							return true;
						}
						return false;
					}
				}
			};

			foreach ((string key, var action) in tileActions)
			{
				GameLocation.RegisterTileAction(key: key, action: action);
			}

			Dictionary<string, Action<GameLocation, string[], Farmer, Vector2>> touchActions = new()
			{
				{
					// Hop touch-action
					ModEntry.ModData.TouchActionHop, (GameLocation where, string[] args, Farmer who, Vector2 tile) =>
					{
						// Don't allow for triggering other Hop tiles while already hopping
						if (Game1.player.freezePause > 0)
							return;

						const int argsToSkip = 1; // First element is the action name, unused
						const int argsLength = 4; // Each hop parses 4 elements in args before continuing
						void hop(int argsIndex, Vector2 fromPosition)
						{
							Vector2 toTile = Vector2.Zero;
							if (float.TryParse(args[argsIndex + 0], out toTile.X)
								&& float.TryParse(args[argsIndex + 1], out toTile.Y)
								&& int.TryParse(args[argsIndex + 2], out int facingDirection))
							{
								// Behaviour on hop started:

								const int duration = 350;
								Vector2 toPosition = toTile * Game1.tileSize;

								// Play starting sound cue
								Utils.TryPlaySound(cueName: args[argsIndex + 3]);

								// Play dust-puff effect
								TemporaryAnimatedSprite puff = new(
									textureName: "TileSheets/animations",
									sourceRect: new Rectangle(0, 320, 64, 64),
									animationInterval: 50f,
									animationLength: 8,
									numberOfLoops: 0,
									position: new Vector2(
										x: fromPosition.X - fromPosition.X % Game1.tileSize + 16,
										y: fromPosition.Y - fromPosition.Y % Game1.tileSize + 16),
									flicker: false,
									flipped: false)
								{
									scale = 0.5f,
									alpha = 0.95f,
									alphaFade = 0.01f
								};
								Game1.player.currentLocation.TemporarySprites.Add(puff);

								// StardewValley.Farmer.cs:BeginSitting
								// Stop player animations and hop to the target position
								Game1.player.Halt();
								Game1.player.synchronizedJump(4f);
								Game1.player.FarmerSprite.StopAnimation();
								Game1.player.LerpPosition(
									start_position: Game1.player.Position,
									end_position: toPosition,
									duration: duration / 1000f);

								Game1.player.FarmerSprite.setCurrentAnimation(animation:
								[
									new FarmerSprite.AnimationFrame(
										frame: new[]{ FarmerSprite.walkUp, FarmerSprite.walkRight, FarmerSprite.walkDown, FarmerSprite.walkRight }[facingDirection],
										milliseconds: duration,
										secondaryArm: false,
										flip: facingDirection == Game1.left,
										frameBehavior: (Farmer who) =>
										{
											// Behaviour on hop completed:

											// Required for ending hop-animation
											Game1.player.Halt();
											Game1.player.FarmerSprite.StopAnimation();
											Game1.player.completelyStopAnimatingOrDoingAction();

											// Check progress in hop-chain given in args
											int nextIndex = argsIndex + argsLength;
											if (args.Length >= nextIndex + argsLength)
											{
												// Continue to next point in hop-chain
												hop(argsIndex: nextIndex, fromPosition: Game1.player.Position);
											}
											else
											{
												// At end of hop-chain:

												// Match player facing-direction to last hop's direction
												Game1.player.FacingDirection = facingDirection;

												// Play landing sound cue
												Game1.player.checkForFootstep();
											}
										},
										behaviorAtEndOfFrame: true)
								]);
								// Required for holding hop-animation until complete
								Game1.player.FarmerSprite.PauseForSingleAnimation = true;
							}
						}
						if (args.Length - argsToSkip >= argsLength)
						{
							hop(argsIndex: argsToSkip, fromPosition: Game1.player.Position);
						}
					}
				}
			};

			foreach ((string key, var action) in touchActions)
			{
				GameLocation.RegisterTouchAction(key: key, action: action);
			}
		}

		#endregion

		#region Event actions

		public void RegisterEventCommands()
		{
			/*
			Dictionary<string, EventCommandDelegate> eventCommands = new()
			{
				{
					// Crystal ball cutscene
					ModConsts.EventCommandCrystalBall, new EventCommandDelegate((Event e, string[] args, EventContext context) =>
					{
						if (e.currentCustomEventScript != null)
						{
							if (e.currentCustomEventScript.update(context.Time, e))
							{
								e.currentCustomEventScript = null;
								e.CurrentCommand++;
							}
						}
						else
						{
							e.currentCustomEventScript = new CrystalBall();
							Game1.globalFadeToClear(afterFade: null, fadeSpeed: 0.01f);
						}
					})
				}
			};
			foreach (var pair in eventCommands)
			{
				Event.RegisterCustomCommand(name: pair.Key, action: pair.Value);
			}
			*/
		}

		#endregion

		#region Object actions

		/// <summary>
		/// Handles player using custom objects and items.
		/// </summary>
		public void CheckHeldObjectAction(Object o, GameLocation where, SButton btn)
		{
			if (o.Name == ModEntry.ModData.ItemTotem)
			{
				if (btn.IsActionButton())
				{
					Shrine.StartTotemWarp(who: Game1.player, o: o, isConsumed: true);
				}
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
*/

#endregion