using HarmonyLib; // el diavolo nuevo
using Hikawa.Data;
using Hikawa.Objects.Critters;
using Hikawa.Objects.Items;
using Hikawa.Objects.Items.Data;
using Hikawa.Objects.Locations;
using Hikawa.Objects.Trinkets;
using Hikawa.Volleyball;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Companions;
using StardewValley.Extensions;
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

            // Bugs
            public Dictionary<string, int> BugsPlaced = [];

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
        public static Texture2D EventSprites { get; private set; }
        public static Texture2D OutdoorsSprites { get; private set; }

        // data
        public static Lazy<BowsDataAsset> BowsData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<BowsDataAsset>(AssetManager.BowsDataAssetName));
		public static Lazy<BugsDataAsset> BugsData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<BugsDataAsset>(AssetManager.BugsDataAssetName));
		public static Lazy<CrowTradeRulesDataAsset> CrowTradeRulesData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<CrowTradeRulesDataAsset>(AssetManager.CrowTradeRulesDataAssetName));
		public static Lazy<DecorSpawnsDataAsset> DecorSpawnsData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<DecorSpawnsDataAsset>(AssetManager.DecorSpawnsDataAssetName));
		public static Lazy<KitesDataAsset> KitesData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<KitesDataAsset>(AssetManager.KiteDataAssetName));
		public static Lazy<ShrineTreesDataAsset> ShrineTreesData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<ShrineTreesDataAsset>(AssetManager.ShrineTreesDataAssetName));
		public static Lazy<ShrubsDataAsset> ShrubsData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<ShrubsDataAsset>(AssetManager.ShrubsDataAssetName));
		public static Lazy<VolleyballDataAsset> VolleyballData { get; private set; } = new(() => ModEntry.Instance.Helper.GameContent.Load<VolleyballDataAsset>(AssetManager.VolleyballDataAssetName));

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
			ModEntry.EventSprites = ModEntry.Instance.Helper.GameContent.Load<Texture2D>(AssetManager.EventSpritesAssetName);
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
			TileActions.RegisterAll(ModEntry.ModData.ContentPrefix);
			EventCommands.RegisterAll(ModEntry.ModData.ContentPrefix);
			ItemQueryResolvers.RegisterAll(ModEntry.ModData.ContentPrefix);
			GameStateQueryResolvers.RegisterAll(ModEntry.ModData.ContentPrefix);

			// modules
			Modules.DialogueEffects.Init();

			// dev tests
			if (ModEntry.Config.DebugMode)
			{
				ConsoleCommands.RegisterAll(this.Helper, prefix: ModEntry.ModData.ConsoleCommandPrefix);
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
			GameLocation location = Game1.currentLocation;
			if (location is null)
				return;

			bool isWorld = e.Step is StardewValley.Mods.RenderSteps.World_Sorted;
			bool isAlwaysFront = e.Step is StardewValley.Mods.RenderSteps.World_AlwaysFront;

			// Crow trinket companions
			if (isAlwaysFront)
				if (!location.shouldHideCharacters())
					if (Game1.currentMinigame is null)
						if (location.currentEvent is null || location.currentEvent.isFestival || location.currentEvent.farmerActors.Count == 0)
							foreach (var farmer in location.farmers)
								foreach (Companion companion in farmer.companions)
									if (companion is CrowCompanion crow)
										crow.DrawAboveAlwaysFront(e.SpriteBatch);

			// Critters
            if (location.critters is not null)
			{
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
		}

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (ModEntry.State.Value is not null)
                ModEntry.State.Value.PreciseTime = Utils.GetPreciseTimeOfDay(Game1.timeOfDay);
            Modules.DialogueEffects.Update(e.Ticks);

			this.UpdatePlayerWading();
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
			ModEntry.State.ResetAllScreens();
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

		public void UpdatePlayerWading()
		{
			// wading

			var player = Game1.player;
			var location = player.currentLocation;

			if (location is null)
				return;

			var tile = player.TilePoint;
			var water = location?.doesTileHaveProperty(tile.X, tile.Y, "Water", "Back");
			var key = ModEntry.ModData.ContentPrefix + "_InWater";
			player.modData.TryGetValue(key, out string wasInWater);
			player.modData[key] = water;

			// wading in water
			if (water is not null)
			{
				// start jumping in
				if (wasInWater is null)
				{
					// i swear to god this was the only way
					// helpfully OnBridge does also prevent people from doing all the things i don't want them to do while wading
					player.onBridge.Value = true;
					player.jump(4);
				}
				// finish jumping in
				if (player.yJumpOffset == 0 && player.yOffset == 0)
                {
                    player.playNearbySoundAll("pullItemFromWater");
					Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(27, 100, 4, 0, new Vector2(player.Position.X, player.StandingPixel.Y), false, false) { layerDepth = 1f, motion = (player.Position - player.lastPosition) / 2 });
				}
				// wading
				if (player.yJumpOffset == 0)
				{
					player.yOffset = 4 * Game1.pixelZoom;
					player.Speed = Farmer.walkingSpeed;
					player.canOnlyWalk = true;
					player.running = false;
					player.shouldShadowBeOffset = true;

					int oldSwimTimer = player.swimTimer;
					player.swimTimer -= (int)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
					if (player.timerSinceLastMovement == 0)
					{
						if (oldSwimTimer > 400 && player.swimTimer <= 400 && player.IsLocalPlayer)
						{
							Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(Game1.animationsName, new Rectangle(0, 0, Game1.tileSize, Game1.tileSize), 150f - ((Math.Abs(player.xVelocity) + Math.Abs(player.yVelocity)) * 3), 8, 0, new Vector2(player.Position.X, player.StandingPixel.Y), false, Game1.random.NextBool(), .01f, .01f, Color.White, 1f, .003f, 0, 0));
						}
						if (player.swimTimer <= 0)
						{
							player.swimTimer = 800;
							if (player.IsLocalPlayer)
							{
								player.playNearbySoundAll("slosh");
								Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(Game1.animationsName, new Rectangle(0, 0, Game1.tileSize, Game1.tileSize), 150f - ((Math.Abs(player.xVelocity) + Math.Abs(player.yVelocity)) * 3), 8, 0, new Vector2(player.Position.X, player.StandingPixel.Y), false, Game1.random.NextBool(), .01f, .01f, Color.White, 1f, .003f, 0, 0));
							}
						}
					}
				}
            }
            // jump out
            else if (wasInWater is not null)
			{
				player.onBridge.Value = false;
				player.yOffset = 0;
				player.shouldShadowBeOffset = false;
                player.jump(4);
                player.freezePause = 100;
            }
		}
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