using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Hikawa.Modules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Objects;
using xTile.Dimensions;
using Color = Microsoft.Xna.Framework.Color;
using Object = StardewValley.Object;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa.Objects.Locations
{
	[XmlType($"Mods_Blueberry_Hikawa_{nameof(Shrine)}")] // SpaceCore serialisation signature
	public class Shrine : GameLocation
    {
		// Values
		public static readonly Vector2 StorageTileLocation = new Vector2(-100, -100);

		// Animations
		public int PetalIndex;
		public List<Vector2> PetalSpawnTiles = [];

		// Critters
		public bool ShouldCrowsSpawnToday;
		public bool WhatAboutCatsCanTheySpawnToday;

		public Shrine() : base() {}

		public Shrine(string filename, string locationName) : base(filename, locationName) {}

		public static Shrine Get()
		{
			return Game1.getLocationFromName(ModConsts.MapShrine) as Shrine;
		}

		#region Location methods

		public override void UpdateWhenCurrentLocation(GameTime time)
		{
			base.UpdateWhenCurrentLocation(time);

			// House chimney smoke puffs
			if (// Poll rate
				time.TotalGameTime.Ticks % 125 == 0
				// World state
				&& Game1.timeOfDay > 1100
				&& House.Get() is GameLocation house && house.characters.Any())
			{
				// StardewValley.Building.cs:Update
				var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
					textureName: "LooseSprites/Cursors",
					sourceRect: new Rectangle(372, 1956, 10, 10),
					position: ModConsts.HouseChimneyTile * Game1.tileSize,
					flipped: false,
					alphaFade: 0.002f,
					color: Color.Gray);
				sprite.alpha = 0.75f;
				sprite.motion = new Vector2(
					x: WeatherDebris.globalWind,
					y: -0.5f);
				sprite.acceleration = new Vector2(
					x: 0.002f,
					y: 0f);
				sprite.interval = 99999f;
				sprite.layerDepth = 1f;
				sprite.scale = 3f;
				sprite.scaleChange = 0.03f;
				sprite.rotationChange = (float)(Game1.random.Next(-3, 4) * Math.PI / 256f);
				sprite.drawAboveAlwaysFront = true;
				this.TemporarySprites.Add(sprite);
			}

			// Falling petals
			if (// Poll rate
				time.TotalGameTime.Ticks % 13 == 0
				// Game state
				&& this.PetalSpawnTiles.Any()
				// World state
				//&& Game1.IsSpring && Game1.dayOfMonth > WorldDate.DaysPerMonth / 2
				)
			{
				Vector2 tile = this.PetalSpawnTiles[this.PetalIndex];
				++this.PetalIndex;
				this.PetalIndex %= this.PetalSpawnTiles.Count;
				var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
					textureName: AssetManager.ExtraSpritesAssetName,
					sourceRect: new Rectangle(320, 208, 16, 16),
					position: tile * Game1.tileSize
						+ new Vector2(
							x: (float)(-2.5f + 5f * Game1.random.NextDouble()),
							y: (float)(-0.5f + 1f * Game1.random.NextDouble())) * Game1.tileSize,
					flipped: Game1.random.NextDouble() > 0.5d,
					alphaFade: 0f,
					color: Color.White);
				sprite.motion = new Vector2(
					x: WeatherDebris.globalWind,
					y: 0.5f);
				sprite.acceleration = new Vector2(
					x: 0.002f,
					y: 0f);
				sprite.alphaFadeFade = -0.00001f;
				sprite.animationLength = 11;
				sprite.totalNumberOfLoops = 8;
				sprite.interval = (float)(100f + 100f * Game1.random.NextDouble());
				sprite.layerDepth = 1f;
				sprite.scale = (float)(3f + 0.5f * Game1.random.NextDouble());
				sprite.scaleChange = -0.0025f;
				this.TemporarySprites.Add(sprite);
			}

			// Lost item quest sparkles
			if (// Quest flag
				(ModEntry.SaveData.LostJewelryQuestTile != Vector2.Zero || ModEntry.SaveData.LostGlassesQuestTile != Vector2.Zero)
				// Poll rate
				&& time.TotalGameTime.Ticks % 600 == 0 && Game1.random.NextDouble() < 0.5f
				// Game state
				&& Context.IsPlayerFree
				// World state
				&& !Game1.IsWinter && !Game1.isStartingToGetDarkOut(this) && !Game1.IsRainingHere(this))
			{
				Rectangle source = new(272, 0, 16, 16);
				Vector2 tile = ModEntry.SaveData.LostJewelryQuestTile != Vector2.Zero ? ModEntry.SaveData.LostJewelryQuestTile : ModEntry.SaveData.LostGlassesQuestTile;
				var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
					textureName: AssetManager.ExtraSpritesAssetName,
					sourceRect: source,
					position: tile * Game1.tileSize,
					flipped: false,
					alphaFade: 0f,
					color: Color.White);
				sprite.alpha = 0f;
				sprite.alphaFade = -0.035f;
				sprite.alphaFadeFade = -0.00075f;
				sprite.interval = 1500f;
				sprite.layerDepth = 0f;
				sprite.scale = Game1.pixelZoom;
				sprite.rotationChange = (float)(Math.PI * 2f / 10f * sprite.interval);
				this.TemporarySprites.Add(sprite);
			}
		}

		public override void DayUpdate(int dayOfMonth)
		{
			base.DayUpdate(dayOfMonth);

			// TODO: METHOD: caats spawn conditions
			if (this.CanItemBePlacedHere(Shrine.StorageTileLocation))
			{
				this.Objects.Add(Shrine.StorageTileLocation, new Chest(playerChest: true, tileLocation: Shrine.StorageTileLocation));
			}
			this.SpawnForage();
			this.ShouldCrowsSpawnToday = (!Game1.IsWinter && !Game1.isRaining) || (Game1.IsWinter && Game1.random.NextDouble() < 0.3d);
			this.WhatAboutCatsCanTheySpawnToday = false;
		}

		protected override void resetLocalState()
		{
			base.resetLocalState();

			Utils.ApplyCustomSharedMapProperties(this);

			Game1.background = new ShrineBackground(location: this);
			this.PetalSpawnTiles = Utils.GetTilesWithProperty(where: this, layer: "AlwaysFront", property: ModConsts.TilePetalSpawner);
		}

		protected override void resetSharedState()
		{
			base.resetSharedState();

			this.critters = [];
			if (Utils.IsItObonYet())
			{
				// Nice one

				// TODO: ASSETS: Obon decorations for the shrine

				//SpawnPerchedCrows(where, Vector2.Zero, Vector2.Zero);
			}
			else if (false)
			{
				// Eerie effects
				ModEntry.OverlayEffectControl.Enable(OverlayEffectControl.Effect.Mist);
				this.SpawnGenericCrowsAt(
				new Location(
					this.Map.Layers[0].LayerWidth / 2 - 1,
					this.Map.Layers[0].LayerHeight / 10 * 9),
				new Location(
					this.Map.Layers[0].LayerWidth / 2 + 1,
					this.Map.Layers[0].LayerHeight / 10 * 9));
				if (!Game1.isRaining)
				{
					Game1.changeMusicTrack("communityCenter");
				}
			}
			else
			{
				this.SpawnAnimals();
				if (this.ShouldCrowsSpawnToday)
					this.TrySpawnDailyCrows();
				if (this.WhatAboutCatsCanTheySpawnToday)
					this.TrySpawnDailyCats();
			}
		}

		public override void cleanupBeforePlayerExit()
		{
			Game1.background = null;

			base.cleanupBeforePlayerExit();
		}

		public override void cleanupBeforeSave()
		{
			this.ClearTempCharacters();

			base.cleanupBeforeSave();
		}

		public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
		{
			if (tileLocation.X == ModEntry.SaveData.LostJewelryQuestTile.X && tileLocation.Y == ModEntry.SaveData.LostJewelryQuestTile.Y)
			{
				Game1.playSound("getNewSpecialItem");
				who.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create(ModConsts.ItemLostJewelry));
			}
			else if (tileLocation.X == ModEntry.SaveData.LostGlassesQuestTile.X && tileLocation.Y == ModEntry.SaveData.LostGlassesQuestTile.Y)
			{
				Game1.playSound("getNewSpecialItem");
				who.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create(ModConsts.ItemLostGlasses));
			}
			return base.checkAction(tileLocation, viewport, who);
		}

		public override void tryToAddCritters(bool onlyIfOnScreen = false)
		{
			base.tryToAddCritters(onlyIfOnScreen);

			// Replace clouds with custom clouds to fade out over open sky
			foreach (Critter c in this.critters.Where(c => c is Cloud and not Hikawa.Objects.Critters.Cloud).ToList())
			{
				this.critters.Remove(c);
				this.critters.Add(new Objects.Critters.Cloud(c.position / Game1.tileSize));
			}
		}

		#endregion

		#region Spawn methods

		public bool TryGetRandomOpenTile(out Vector2 tile, Rectangle? bounds = null, int attempts = 10)
		{
			var layer = this.Map.GetLayer("Back");
			bounds ??= new(0, 0, layer.LayerWidth, layer.LayerHeight);
			for (int i = 0; i < attempts; ++i)
			{
				int x = Game1.random.Next(bounds.Value.X, bounds.Value.Width);
				int y = Game1.random.Next(bounds.Value.Y, bounds.Value.Height);
				tile = new Vector2(x, y);
				if (this.isTileLocationOpen(tile))
					return true;
			}
			tile = Vector2.Zero;
			return false;
		}

		public Vector2 GetLostJewelryQuestTile()
		{
			Vector2 tile = ModEntry.SaveData.LostJewelryQuestTile;
			if (tile == Vector2.Zero && this.TryGetRandomOpenTile(out tile, attempts: 99))
				ModEntry.SaveData.LostJewelryQuestTile = tile;
			return Vector2.Zero;
		}

		public void SpawnAnimals()
		{
			foreach (NPC chicken in this.characters.Where(c => c is Critters.Chicken).ToList())
			{
				this.characters.Remove(chicken);
			}
			if (Game1.timeOfDay < 1800)
			{
				this.addCharacter(new Critters.Chicken(where: this, position: new Vector2(53, 33) * Game1.tileSize));
				this.addCharacter(new Critters.Chicken(where: this, position: new Vector2(47, 49) * Game1.tileSize));
				this.addCharacter(new Critters.Chicken(where: this, position: new Vector2(52, 50) * Game1.tileSize, isBrown: true));
			}
		}
		
		/// <summary>
		/// Adds forage to the grassy edges of the map.
		/// Mostly lifted from StardewValley.GameLocation.cs:spawnObjects().
		/// </summary>
		public void SpawnForage()
		{
			/*
			Log.D($"Spawning forage on {this.Name} (currently {this.numberOfSpawnedObjectsOnMap})");
			const int limitPerMap = 3;
			const int retries = 10;
			Vector2 position;

			// Spawn chicken eggs
			position = new Vector2(Game1.random.Next(25, 35), Game1.random.Next(16, 23));
			if (Game1.random.NextDouble() < 0.06f)
			{
				if (!this.CanItemBePlacedHere(position))
					position = new Vector2(Game1.random.Next(42, 47), Game1.random.Next(16, 20));
				if (this.CanItemBePlacedHere(position))
				{
					var roll = Game1.random.NextDouble();
					if (this.dropObject(new StardewValley.Object(
							position, roll > 0.4f ? 176 : roll > 0.1f ? 180 : roll > 0.04f ? 174 : 182),
						position * 64f,
						Game1.viewport,
						true))
						Log.D("Chicken egg!!!! wow!!",
							ModEntry.Config.DebugMode);
				}
			}

			// Spawn location forage items
			Dictionary<string, string> forageData = ModEntry.Instance.Helper.GameContent.Load
				<Dictionary<string, string>>
				(AssetManager.ForageAssetName);
			if (!forageData.ContainsKey(this.Name))
			{
				Log.E($"No forage data found for {this.Name} ({this})");
				return;
			}
			string rawData = forageData[this.Name].Split('/')[Utility.getSeasonNumber(Game1.currentSeason)];
			if (rawData.Equals("-1") || this.numberOfSpawnedObjectsOnMap >= limitPerMap)
				return;

			// TODO: DEBUG: Does HikawaShrine forage spawning actually restrict to the playable area?

			string[] objectData = rawData.Split(' ');
			int numberToSpawn = Game1.random.Next(1, Math.Min(limitPerMap - 1, limitPerMap + 1 - this.numberOfSpawnedObjectsOnMap));
			for (int k = 0; k < numberToSpawn; k++)
			{
				for (int j = 0; j < retries; j++)
				{
					int x = Game1.random.Next(this.Map.DisplayWidth / Game1.tileSize);
					int y = Game1.random.Next(this.Map.DisplayHeight / Game1.tileSize);
					position = new Vector2(x, y);

					int whichObject = Game1.random.Next(objectData.Length / 2) * 2;
					if (this.CanItemBePlacedHere(position)
					    && Game1.random.NextDouble() < double.Parse(objectData[whichObject + 1])
					    && this.dropObject(new StardewValley.Object(
							position, 
							int.Parse(objectData[whichObject])), 
						new Vector2(x * Game1.tileSize, y * Game1.tileSize), 
						Game1.viewport,
						true))
					{
						++this.numberOfSpawnedObjectsOnMap;
						break;
					}
				}
			}
			*/
		}

		#endregion

		#region Critter methods

		public void TrySpawnDailyCrows()
		{
			if (Game1.timeOfDay < 1130)
			{
				// Spawn active crows on the ground in the morning
				this.TrySpawnGenericCrows();
			}
			if (!Game1.isDarkOut(this))
			{
				// Spawn passive crows as custom perched critters in the afternoon
				double roll = Game1.random.NextDouble();
				if (Game1.IsWinter)
					roll *= 0.5f;
				Vector2[] positions = ModConsts.CrowPerches[ModConsts.CrowPerches.Keys.First(key => roll < key)];
				int hopRange = Game1.IsWinter || positions.Length < 3
					? 0
					: (int)positions[2].X;

				this.SpawnPerchedCrowsAt(phobos: positions[0], deimos: positions[1], hopRange);
			}
		}

		/// <summary>
		/// Attempts to add twin crows to the map as critters.
		/// </summary>
		public void TrySpawnGenericCrows()
		{
			const int retries = 25;
			Location radius = ModConsts.CrowSpawnRadius;
			Location diameter = radius * 2;
			xTile.Dimensions.Rectangle spawnArea = ModConsts.CrowSpawnArea;

			for (int attempts = 0; attempts < retries; ++attempts)
			{
				// Identify two separate nearby spawn positions for the crows around the map's middle
				Location target = new Location(
					x: spawnArea.X + Game1.random.Next(spawnArea.Width),
					y: spawnArea.Y + Game1.random.Next(spawnArea.Height));
				Location phobos = target - radius + new Location(
					x: Game1.random.Next(diameter.X),
					y: Game1.random.Next(diameter.Y));
				Location deimos = target - radius + new Location(
					x: Game1.random.Next(diameter.X),
					y: Game1.random.Next(diameter.Y));

				Log.D($"Checking crow spawns at {phobos} and {deimos}",
					ModEntry.Config.DebugMode);

				if (phobos == deimos || !this.isTileLocationOpen(phobos) || !this.isTileLocationOpen(deimos))
					continue;

				this.SpawnGenericCrowsAt(phobos, deimos);
				return;
			}

			Log.D($"Failed to add crows after {retries} attempts.",
				ModEntry.Config.DebugMode);
		}

		public void ClearCrows()
		{
			this.critters.RemoveAll(critter => critter is Hikawa.Objects.Critters.Crow or StardewValley.BellsAndWhistles.Crow);
		}

		public void ClearCats()
		{
			this.critters.RemoveAll(critter => critter is Hikawa.Objects.Critters.Cat);
		}

		public void ClearTempCharacters()
		{
			this.characters.RemoveWhere(chara => chara is Monster);
		}

		/// <summary>
		/// Attempts to add twin crows to the map as default Crow critters.
		/// </summary>
		public void SpawnGenericCrowsAt(Location phobos, Location deimos)
		{
			Log.D($"Adding generic crows at {phobos} and {deimos}",
				ModEntry.Config.DebugMode);
			this.addCritter(new StardewValley.BellsAndWhistles.Crow(phobos.X, phobos.Y));
			this.addCritter(new StardewValley.BellsAndWhistles.Crow(deimos.X, deimos.Y));
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
		public void SpawnPerchedCrowsAt(Vector2 phobos, Vector2 deimos, int hopRange = 2)
		{
			Log.W($"Adding perched crows at {phobos} and {deimos}");
			this.ClearCrows();

			bool isDeimos = Game1.dayOfMonth % 3 == 0;   // Swap crow roles once every few days
			this.addCritter(new Critters.Crow(isDeimos: isDeimos, position: new Vector2(phobos.X, phobos.Y), hopRange: hopRange));
			this.addCritter(new Critters.Crow(isDeimos: !isDeimos, position: new Vector2(deimos.X, deimos.Y), hopRange: hopRange));
		}

		public void TrySpawnDailyCats()
		{
			double roll = Game1.random.NextDouble();
			Vector2 position = Vector2.Zero;
			int baseFrame = Critters.Cat.StandingBaseFrame;
			int scareRange = 0;
			bool flip = false;

			if (roll < 1f) // TODO: roll chance
			{
				// Test animation: Grooming
				//position = new Vector2(28, 48);
				position = new Vector2(23, 45);
				baseFrame = Critters.Cat.StandingBaseFrame;
				scareRange = 3;
				flip = false;
			}

			this.SpawnCatsAt(position, baseFrame, scareRange, flip);
		}

		public void SpawnCatsAt(Vector2 position, int baseFrame, int scareRange, bool flip)
		{
			Log.W($"Adding cat at {position}");
			this.ClearCats();
			this.addCritter(new Critters.Cat(position: position, baseFrame: baseFrame, scareRange: scareRange, flip: flip));
		}

		#endregion

		#region Totem warp methods

		public static void StartTotemWarp(Farmer who, Object o, bool isConsumed)
		{
			if (o is null)
				return;

			int index = o.ParentSheetIndex;
			ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(o.QualifiedItemId);
			Texture2D texture = itemData.GetTexture();

			if (isConsumed && --o.Stack <= 0)
			{
				who.removeItemFromInventory(o);
				who.showNotCarrying();
			}

			who.jitterStrength = 1f;
			who.faceDirection(2);
			who.temporarilyInvincible = true;
			who.temporaryInvincibilityTimer = -4000;

			who.currentLocation.playSound("warrior");
			Game1.changeMusicTrack("none");

			who.FarmerSprite.animateOnce(
			[
				new FarmerSprite.AnimationFrame(
					frame: 57,
					milliseconds: 2000,
					secondaryArm: false,
					flip: false),
				new FarmerSprite.AnimationFrame(
					frame: (short)who.FarmerSprite.CurrentFrame,
					milliseconds: 0,
					secondaryArm: false,
					flip: false,
					frameBehavior: TotemWarpToShrine,
					behaviorAtEndOfFrame: true)
			]);
			who.CanMove = false;

			Game1.screenGlowOnce(
				glowColor: Color.Violet,
				hold: false);
			Utility.addSprinklesToLocation(
				l: who.currentLocation,
				sourceXTile: who.TilePoint.X, sourceYTile: who.TilePoint.Y,
				tilesWide: 16,
				tilesHigh: 16,
				totalSprinkleDuration: 1300,
				millisecondsBetweenSprinkles: 20,
				sprinkleColor: Color.White,
				sound: null,
				motionTowardCenter: true);
			Game1.Multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(
				textureName: itemData.TextureName,
				sourceRect: itemData.GetSourceRect(),
				animationInterval: 9999f,
				animationLength: 1,
				numberOfLoops: 999,
				position: who.Position + new Vector2(0f, -96f),
				flicker: false,
				flipped: false,
				layerDepth: 1f,
				alphaFade: 0.0075f,
				color: Color.White,
				scale: Game1.pixelZoom * 1f,
				scaleChange: 0.01f,
				rotation: 0f,
				rotationChange: 0f)
			{
				motion = new Vector2(0f, -1f),
				alpha = 1f,
				shakeIntensity = 1f,
				initialPosition = who.Position + new Vector2(0f, -96f),
				xPeriodic = true,
				xPeriodicLoopTime = 1000f,
				xPeriodicRange = 4f
			});
			Game1.Multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(
				textureName: itemData.TextureName,
				sourceRect: itemData.GetSourceRect(),
				animationInterval: 9999f,
				animationLength: 1,
				numberOfLoops: 999,
				position: who.Position + new Vector2(-64f, -96f),
				flicker: false,
				flipped: false,
				layerDepth: 0.9999f,
				alphaFade: 0.0075f,
				color: Color.White,
				scale: Game1.pixelZoom * 0.5f,
				scaleChange: 0.005f,
				rotation: 0f,
				rotationChange: 0f)
			{
				motion = new Vector2(0f, -0.5f),
				alpha = 1f,
				shakeIntensity = 1f,
				delayBeforeAnimationStart = 10,
				initialPosition = who.Position + new Vector2(-64f, -96f),
				xPeriodic = true,
				xPeriodicLoopTime = 1000f,
				xPeriodicRange = 4f
			});
			Game1.Multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(
				textureName: itemData.TextureName,
				sourceRect: itemData.GetSourceRect(),
				animationInterval: 9999f,
				animationLength: 1,
				numberOfLoops: 999,
				position: who.Position + new Vector2(64f, -96f),
				flicker: false,
				flipped: false,
				layerDepth: 0.9988f,
				alphaFade: 0.0075f,
				color: Color.White,
				scale: Game1.pixelZoom * 0.5f,
				scaleChange: 0.005f,
				rotation: 0f,
				rotationChange: 0f)
			{
				motion = new Vector2(0f, -0.5f),
				alpha = 1f,
				alphaFade = 0.0075f,
				delayBeforeAnimationStart = 20,
				shakeIntensity = 1f,
				initialPosition = Game1.player.Position + new Vector2(64f, -96f),
				xPeriodic = true,
				xPeriodicLoopTime = 1000f,
				xPeriodicRange = 4f
			});
		}

		/// <summary>
		/// Method lifted from StardewValley.Object.totemWarp(Farmer who)
		/// </summary>
		public static void TotemWarpToShrine(Farmer who)
		{
			for (int j = 0; j < 12; j++)
			{
				Game1.Multiplayer.broadcastSprites(
					location: who.currentLocation,
					sprites: new TemporaryAnimatedSprite(
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
			Game1.displayFarmer = false;
			who.currentLocation.playSound("wand");
			who.temporarilyInvincible = true;
			who.temporaryInvincibilityTimer = -2000;
			who.freezePause = 1000;
			Game1.flashAlpha = 1f;
			DelayedAction.fadeAfterDelay(() => Shrine.FinishTotemWarp(who), 1000);
			new Rectangle(who.GetBoundingBox().Location, new Point(Game1.tileSize)).Inflate(192, 192);

			int i = 0;
			for (int x = who.TilePoint.X + 8; x >= who.TilePoint.X - 8; x--)
			{
				Game1.Multiplayer.broadcastSprites(
					location: who.currentLocation,
					sprites: new TemporaryAnimatedSprite(
						6,
						new Vector2(x, who.TilePoint.Y) * Game1.tileSize,
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
		public static void FinishTotemWarp(Farmer who)
		{
			Point tileLocation = Point.Zero;
			Utility.getDefaultWarpLocation(ModConsts.MapShrine, ref tileLocation.X , ref tileLocation.Y);
			Game1.warpFarmer(locationName: ModConsts.MapShrine, tileX: tileLocation.X, tileY: tileLocation.Y, flip: false);
			Game1.fadeToBlackAlpha = 0.99f;
			Game1.screenGlow = false;
			Game1.player.temporarilyInvincible = false;
			Game1.player.temporaryInvincibilityTimer = 0;
			Game1.displayFarmer = true;
		}

		#endregion
    }
}
