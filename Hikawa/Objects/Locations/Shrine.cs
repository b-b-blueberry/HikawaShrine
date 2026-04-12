using Hikawa.Data;
using Hikawa.Modules;
using Hikawa.Objects.Critters;
using Hikawa.Objects.Menus;
using Netcode;
using StardewModdingAPI;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.TerrainFeatures;
using System;
using System.Linq;
using System.Xml.Serialization;
using Object = StardewValley.Object;

namespace Hikawa.Objects.Locations
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(Shrine)}")] // SpaceCore serialisation signature
	public class Shrine : GameLocation
	{
		/** PERSISTENT **/

		// Items
		public NetRef<Item> CrowTradeItem = new();
		public NetMutex CrowTradeMutex = new();

		// Events
		public bool IsSummerFestival;

		/** TEMPORARY **/

		// Animations
		[XmlIgnore]
		public int BellTimer;
		[XmlIgnore]
		public int BellTimerMax = 1500;
		[XmlIgnore]
		public (Vector2, Vector2) BellShake = new();
        [XmlIgnore]
        public ICue WindChimeCue;

        // Crows
		[XmlIgnore]
		public Vector2 CrowTradeTile;
		[XmlIgnore]
		public bool IsCrowTradeUsedToday;

		// Critters
		[XmlIgnore]
		public bool ShouldCrowsSpawnToday;
		[XmlIgnore]
		public bool WhatAboutCatsCanTheySpawnToday;
		[XmlIgnore]
		public bool ShouldChickenSpawnToday;
		[XmlIgnore]
		public bool IsCrowTradeAvailableToday;
		[XmlIgnore]
		public ShrineBabyCrowController BabyCrows;

		public Shrine() : base() {}

		public Shrine(string filename, string locationName) : base(filename, locationName) {}

		public static Shrine Get()
		{
			return Game1.RequireLocation<Shrine>(ModEntry.ModData.MapShrine);
		}

		#region Location methods

		protected override void initNetFields()
		{
			base.initNetFields();

			this.NetFields
				.AddField(this.CrowTradeItem, nameof(this.CrowTradeItem))
				.AddField(this.CrowTradeMutex.NetFields, $"{nameof(this.CrowTradeItem)}.{nameof(this.CrowTradeMutex.NetFields)}");
		}

		protected override void drawCharacters(SpriteBatch b)
		{
			base.drawCharacters(b);

			if (this.BabyCrows is ShrineBabyCrowController c)
			{
				c.IsDrawingAboveAlwaysFront = false;
				c.Draw(b);
			}
		}

		public override void drawAboveFrontLayer(SpriteBatch b)
		{
			// Generous draw bounds for large ShrineTree sprites
			Vector2 tile = Vector2.Zero;
			Point min = new(x: -3, y: -3);
			Point max = new(x: 5, y: 9);
			for (int x = Game1.viewport.X / Game1.tileSize + min.X; x < (Game1.viewport.X + Game1.viewport.Width) / Game1.tileSize + max.X; x++)
			{
				for (int y = Game1.viewport.Y / Game1.tileSize + min.Y; y < (Game1.viewport.Y + Game1.viewport.Height) / Game1.tileSize + max.Y; y++)
				{
					tile.X = x;
					tile.Y = y;
					if (this.terrainFeatures.TryGetValue(tile, out var tf) && tf is not Flooring)
						tf.draw(b);
				}
			}

			Vector2 zero = new Vector2(x: Game1.viewport.X, y: Game1.viewport.Y) * -1f;
			/*
			// Crow trade stump
			if (this.IsCrowTradeItemReady)
			{
				float yOffset = Game1.pixelZoom * (float)MathF.Round(MathF.Sin((float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250), 2);
				Vector2 local = zero
					+ this.CrowTradeTile * Game1.tileSize
					+ new Vector2(0, -2) * Game1.tileSize
					+ new Vector2(0, yOffset);
				b.Draw(
					texture: Game1.emoteSpriteSheet,
					position: local,
					sourceRectangle: new Rectangle(0, 32, 16, 16),
					color: Color.White * 0.85f,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: Game1.pixelZoom,
					effects: SpriteEffects.None,
					layerDepth: 0.98f);
			}
			*/
			// Shrine bell
			float layerDepth = 0.0001f;
			float bellRatioRaw = 1f - (float)this.BellTimer / this.BellTimerMax;
			float bellRatio = -MathF.Sin(-MathF.PI + bellRatioRaw * MathF.PI * 3f);

			Vector2 position = new(34.5f + 2f / Game1.smallestTileSize, 32f + 2f / Game1.smallestTileSize);
			b.Draw(
				texture: ModEntry.OutdoorsSprites,
				position: zero
					+ position * Game1.tileSize
					+ new Vector2(0f, -3f / Game1.smallestTileSize) * Game1.tileSize
					+ new Vector2(this.BellShake.Item1.X * bellRatio * 0.5f, bellRatio * 0.15f * Game1.tileSize)
					,
				color: Color.White,
				sourceRectangle: new(240, 128, 16, 48),
				rotation: 0f,
				origin: Vector2.Zero,
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				layerDepth: layerDepth * 1);
			b.Draw(
				texture: ModEntry.OutdoorsSprites,
				position: zero
					+ position * Game1.tileSize
					+ new Vector2(0f, 0f) * Game1.tileSize
					+ this.BellShake.Item1 * bellRatio * 0.5f
					,
				color: Color.White,
				sourceRectangle: new(240, 176, 16, 16),
				rotation: 0f,
				origin: Vector2.Zero,
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				layerDepth: layerDepth * 2);
			b.Draw(
				texture: ModEntry.OutdoorsSprites,
				position: zero
					+ position * Game1.tileSize
					+ new Vector2(8f, 8f) * Game1.pixelZoom
					+ this.BellShake.Item2 * bellRatio * 0.5f
					,
				color: Color.White,
				sourceRectangle: new(256, 176, 16, 16),
				rotation: 0f * bellRatio * MathF.PI * 0.5f,
				origin: new(8, 8),
				scale: Game1.pixelZoom,
				effects: SpriteEffects.None,
				layerDepth: layerDepth * 3);
		}

		public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
		{
			base.drawAboveAlwaysFrontLayer(b);

			if (this.BabyCrows is ShrineBabyCrowController c)
			{
				c.IsDrawingAboveAlwaysFront = true;
				c.Draw(b);
			}
		}

		public override void UpdateWhenCurrentLocation(GameTime time)
		{
			base.UpdateWhenCurrentLocation(time);

			int ms = time.ElapsedGameTime.Milliseconds;
			long ticks = time.TotalGameTime.Ticks;
			const int sparkleInterval = 600;

			this.BabyCrows?.Update(time);
			this.UpdateWindEffects(ticks);

			// Bell animation sequence
			if (this.BellTimer > 0)
			{
				this.BellTimer = Math.Max(0, this.BellTimer - ms);
				int i = 4;
				if (ticks % 2 == 0)
					this.BellShake = (new(Game1.random.Next(-i, i), Game1.random.Next(-i, i)), new(Game1.random.Next(-i, i), Game1.random.Next(-i, i)));
			}

			// Lost item quest sparkles
			if (ModEntry.SaveData is not null
				// Quest flag
				&& (ModEntry.SaveData.LostJewelryQuestTile != default || ModEntry.SaveData.LostGlassesQuestTile != default)
				// Poll rate
				&& ticks % sparkleInterval == 0 && Game1.random.NextDouble() < 0.5f
				// Game state
				&& Context.IsPlayerFree
				// World state
				&& !Game1.IsWinter && !Game1.isStartingToGetDarkOut(this) && !Game1.IsRainingHere(this)
				)
			{
				Vector2 tile = ModEntry.SaveData.LostJewelryQuestTile != default ? ModEntry.SaveData.LostJewelryQuestTile : ModEntry.SaveData.LostGlassesQuestTile; 
				Utils.CreateSparkleAtTile(where: this, tile: tile);
			}

			// Crow trade item
			if (// Ready flag
				this.IsCrowTradeItemReady
				// Poll rate
				&& ticks % sparkleInterval == 0 //&& Game1.random.NextDouble() < 0.5f
				// Game state
				&& Context.IsPlayerFree
				)
			{
				Vector2 tile = this.CrowTradeTile + new Vector2(x: -0.0333f, y: -1.333f);
				Utils.CreateSparkleAtTile(where: this, tile: tile);
			}
		}

		public override void updateEvenIfFarmerIsntHere(GameTime time, bool ignoreWasUpdatedFlush = false)
		{
			base.updateEvenIfFarmerIsntHere(time, ignoreWasUpdatedFlush);

			this.CrowTradeMutex.Update(this);
		}

		public override void performTenMinuteUpdate(int timeOfDay)
		{
			base.performTenMinuteUpdate(timeOfDay);

			if (this.BabyCrows is ShrineBabyCrowController c)
				c.roosting = Game1.isDarkOut(this);
		}

		public override void DayUpdate(int dayOfMonth)
		{
			base.DayUpdate(dayOfMonth);

            this.UpdateFestivals();

			// TODO: METHOD: caats spawn conditions

			this.SpawnForage();

			this.IsCrowTradeUsedToday = false;
		}

		protected override void resetLocalState()
		{
			base.resetLocalState();

			// Properties
			var tiles = Utils.GetTilesWithProperty(where: this, layer: "Buildings", property: "Action", value: new(ModEntry.ModData.ActionCrowTrade), onlyOne: true);
			this.CrowTradeTile = tiles.FirstOrDefault();

			// Critters
			this.critters.Clear();
			if (this.ShouldCrowsSpawnToday)
				this.TrySpawnDailyCrows();
			if (this.WhatAboutCatsCanTheySpawnToday)
				this.TrySpawnDailyCats();
			if (this.IsCrowTradeAvailableToday && this.CrowTradeItem.Value is null)
				this.addCritter(new ShrineCrowTradeCrow(this.CrowTradeTile * Game1.tileSize + new Vector2(Game1.tileSize, -Game1.tileSize / 2f)));

			Utils.ApplyCustomSharedMapProperties(this);

			// Effects
			Game1.background = new ShrineBackground(location: this);
		}

		protected override void resetSharedState()
		{
			base.resetSharedState();

			this.ShouldCrowsSpawnToday = (!Game1.IsWinter && !Game1.isRaining) || (Game1.IsWinter && Game1.random.NextDouble() < 0.3d);
			this.WhatAboutCatsCanTheySpawnToday = false;
			this.ShouldChickenSpawnToday = false;
			this.IsCrowTradeAvailableToday = !Game1.IsWinter && !Game1.isRaining;

            if (Utils.IsItObonYet())
			{
				// Nice one

				// TODO: ASSETS: Obon decorations for the shrine

				// SpawnPerchedCrows(where, Vector2.Zero, Vector2.Zero);
			}
			else if (false)
			{
				// Eerie effects
				ModEntry.OverlayEffectControl.Enable(OverlayEffectControl.Effect.Mist);
				this.SpawnGenericCrowsAt(
				new Vector2(
					x: this.Map.Layers[0].LayerWidth / 2 - 1,
					y: this.Map.Layers[0].LayerHeight / 10 * 9),
				new Vector2(
					x: this.Map.Layers[0].LayerWidth / 2 + 1,
					y: this.Map.Layers[0].LayerHeight / 10 * 9));
				if (!Game1.isRaining)
				{
					Game1.changeMusicTrack("communityCenter");
				}
			}
			else
			{
				this.SpawnAnimals();
			}
		}

		public override void cleanupBeforePlayerExit()
		{
			Utils.ResetCustomSharedMapProperties(this);
			Game1.background = null;
			this.BabyCrows = null;
			this.CrowTradeMutex.ReleaseLock();

			base.cleanupBeforePlayerExit();
		}

		public override void cleanupBeforeSave()
		{
			this.ClearTempCharacters();

			base.cleanupBeforeSave();
		}

		public override bool checkAction(xTile.Dimensions.Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
		{
			if (tileLocation.X == ModEntry.SaveData.LostJewelryQuestTile.X && tileLocation.Y == ModEntry.SaveData.LostJewelryQuestTile.Y)
			{
				Game1.playSound("getNewSpecialItem");
				who.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create(ModEntry.ModData.ItemLostJewelry));
			}
			else if (tileLocation.X == ModEntry.SaveData.LostGlassesQuestTile.X && tileLocation.Y == ModEntry.SaveData.LostGlassesQuestTile.Y)
			{
				Game1.playSound("getNewSpecialItem");
				who.addItemByMenuIfNecessaryElseHoldUp(ItemRegistry.Create(ModEntry.ModData.ItemLostGlasses));
			}
			return base.checkAction(tileLocation, viewport, who);
		}

		public override void tryToAddCritters(bool onlyIfOnScreen = false)
		{
			base.tryToAddCritters(onlyIfOnScreen);

			// Replace clouds with custom clouds to fade out over open sky
			foreach (Critter c in this.critters.Where(c => c is Cloud and not ShrineCloud).ToList())
			{
				this.critters.Remove(c);
				this.critters.Add(new ShrineCloud(c.position / Game1.tileSize));
			}
		}

		#endregion

		#region Shrine methods

		public bool IsCrowTradeItemReady => this.CrowTradeTile != default && !this.IsCrowTradeUsedToday && this.CrowTradeItem.Value is not null;

		public bool HandleCrowTradeAction(Farmer who)
		{
			if (this.IsCrowTradeItemReady)
			{
				// who.addItemByMenuIfNecessaryElseHoldUp(this.CrowTradeItem.Value);
				if (who.addItemToInventoryBool(this.CrowTradeItem.Value))
				{
					Game1.playSound("getNewSpecialItem");
					this.CrowTradeItem.Set(null);
				}
				else
				{
					Game1.playSound("cancel");
					Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
				}
			}
			else
			{
				this.CrowTradeMutex.RequestLock(() =>
				{
					Game1.playSound("grassyStep");
					CrowTradeMenu menu = new(shrine: this);
					Game1.activeClickableMenu = menu;
					menu.exitFunction += () =>
					{
						this.CrowTradeMutex.ReleaseLock();
					};
				});
			}

			return true;
		}

		public void FinaliseCrowTrade()
		{
			this.IsCrowTradeUsedToday = false;
			if (this.CrowTradeItem.Value is Item input)
			{
				ItemQueryContext context = new(
					location: this,
					player: Game1.MasterPlayer,
					random: null,
					sourcePhrase: $"location '{this.Name}' > tile action '{ModEntry.ModData.ActionCrowTrade}' > field '{nameof(this.CrowTradeItem)}'");
				context.CustomFields = new() { { "Input", input } };
				foreach (GenericSpawnItemDataWithCondition rule in ModEntry.CrowTradeRulesData.Value.CrowTradeRules)
				{
					if (GameStateQuery.CheckConditions(
							queryString: rule.Condition,
							location: context.Location,
							player: context.Player,
							random: context.Random,
							inputItem: input)
						&& ItemQueryResolver.TryResolveRandomItem(
							data: rule,
							context: context,
							inputItem: input,
							avoidItemIds: [input.ItemId]) is Item output)
					{
						this.CrowTradeItem.Set(output);
						break;
					}
				}
			}
		}

		public void StartBellSequence(Farmer who)
		{
			if (who.FacingDirection == Game1.down)
				who.FacingDirection = Game1.up;

			// Pay tribute
			Vector2 from = Game1.player.StandingPixel.ToVector2() - new Vector2(Game1.tileSize * 0.5f, Game1.tileSize);
			Vector2 to = new Vector2(34.5f + 2f / Game1.smallestTileSize, 34f + 2f / Game1.smallestTileSize) * Game1.tileSize;

			int cost = 50;
			who.Money -= cost;
			int coins = cost / 8 + 2;
			for (int j = 0; j < coins; j++)
			{
				float speed = 6f;
				int range = 14;
				var offset = new Vector2(x: Game1.random.Next(-range, range), y: 0) * Game1.pixelZoom;
				var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
					textureName: "TileSheets\\debris",
					sourceRect: new Rectangle(x: Game1.random.Next(2) * 16, y: 64, width: 16, height: 16),
					position: from,
					flipped: false,
					alphaFade: 0f,
					color: Color.White);
				sprite.alpha = 4f;
				sprite.alphaFadeFade = -speed / 5000f;
				sprite.scale = 4f;
				sprite.delayBeforeAnimationStart = j * 50;
				sprite.motion = Utility.getVelocityTowardPoint(startingPoint: from.ToPoint(), endingPoint: to + offset, speed: speed);
				sprite.acceleration = -Utility.getVelocityTowardPoint(startingPoint: from.ToPoint(), endingPoint: to + offset, speed: speed / 50f);
				sprite.drawAboveAlwaysFront = who.FacingDirection != Game1.up;
				this.TemporarySprites.Add(sprite);
			}

			// Animate player
			int[] frames = new int[][] { [62, 62, 63, 46], [58, 58, 59, 45], [54, 54, 55, 25], [58, 58, 59, 45] }[who.FacingDirection];
			int[] durations = [0, 75, 100, 500];
			int delay = 1500;
			who.freezePause = durations.Sum() + delay;
			bool flip = who.FacingDirection == Game1.left;
			who.FarmerSprite.animateOnce([
				new(frames[0], 0, secondaryArm: false, flip: flip),
				new(frames[1], 75, secondaryArm: false, flip: flip),
				new(frames[2], 100, secondaryArm: false, flip: flip),
				new(frames[3], 500, secondaryArm: true, flip: flip),
				new(who.FarmerSprite.CurrentFrame, delay, secondaryArm: false, flip: flip, frameBehavior: this.ContinueBellSequence, behaviorAtEndOfFrame: true)
			]);
		}

		public void ContinueBellSequence(Farmer who)
		{
			// Play sounds
			for (int i = 0; i < 7; ++i)
				DelayedAction.functionAfterDelay(() => who.playNearbySoundAll("skeletonHit"), this.BellTimerMax / 8 * (i + 1));

			// Animate player
			who.freezePause = this.BellTimer = this.BellTimerMax;
			who.FarmerSprite.animateOnce(
			[
				new FarmerSprite.AnimationFrame(
					frame: 57,
					milliseconds: this.BellTimerMax,
					secondaryArm: false,
					flip: false),
				new FarmerSprite.AnimationFrame(
					frame: (short)who.FarmerSprite.CurrentFrame,
					milliseconds: 0,
					secondaryArm: false,
					flip: false,
					frameBehavior: this.EndBellSequence,
					behaviorAtEndOfFrame: true)
			]);
		}

		public void EndBellSequence(Farmer who)
		{
			who.stats.Increment($"{ModEntry.ModData.ContentPrefix}_BellRingCount");

			// TODO: Apply shrine effects
		}

		public void UpdateWindEffects(long ticks)
		{
			// Idle windy weather
			var initialWind = WeatherDebris.globalWind;

			float baseWind = -0.25f;
			float startChance = 0.01f;
			float endChance = 0.007f;
			if (WeatherDebris.globalWind == 0f)
			{
				WeatherDebris.globalWind = baseWind;
			}
			if (Game1.windGust == 0f && WeatherDebris.globalWind >= baseWind && Game1.random.NextDouble() < startChance)
			{
				Game1.windGust += Game1.random.Next(-5, -1) / 100f;
			}
			else if (Game1.windGust != 0f)
			{
				Game1.windGust = Math.Max(-5f, Game1.windGust * 1.02f);
				WeatherDebris.globalWind = baseWind + Game1.windGust;
				if (Game1.windGust < -0.2f && Game1.random.NextDouble() < endChance)
				{
					Game1.windGust = 0f;
				}
			}
			if (WeatherDebris.globalWind < baseWind)
			{
				WeatherDebris.globalWind = Math.Min(baseWind, WeatherDebris.globalWind + 0.015f);
			}

            // started wind gust
            if (initialWind < -1.5f && initialWind > WeatherDebris.globalWind)
			{
				if (this.WindChimeCue?.IsPlaying != true)
                    Game1.sounds.PlayLocal(
                        cueName: initialWind < -2.5f ? $"{ModEntry.ModData.ContentPrefix}_Chime_Big" : $"{ModEntry.ModData.ContentPrefix}_Chime_Small",
                        location: this,
                        position: new Vector2(53, 31),
                        pitch: null,
                        context: SoundContext.Default,
                        cue: out this.WindChimeCue
                    );
            }

			// House chimney smoke puffs
			if (// Poll rate
				ticks % 125 == 0
				// World state
				&& Game1.timeOfDay > 1100
				&& House.Get() is GameLocation house && house.characters.Any())
			{
				// StardewValley.Building.cs:Update
				var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
					textureName: "LooseSprites/Cursors",
					sourceRect: new Rectangle(372, 1956, 10, 10),
					position: ModEntry.DecorSpawnsData.Value.HouseChimneyTile * Game1.tileSize,
					flipped: false,
					alphaFade: 0.002f,
					color: Color.Gray);
				sprite.alpha = 0.95f;
				sprite.motion = new Vector2(
					x: WeatherDebris.globalWind * 0.25f,
					y: -0.5f);
				sprite.acceleration = new Vector2(
					x: sprite.motion.X / 250f,
					y: 0f);
				sprite.interval = 99999f;
				sprite.layerDepth = 1f;
				sprite.scale = 3f;
				sprite.scaleChange = 0.03f;
				sprite.rotationChange = (float)(Game1.random.Next(-3, 4) * Math.PI / 256f);
				sprite.drawAboveAlwaysFront = true;
				this.TemporarySprites.Add(sprite);
			}
		}

		public NPC GetShopPerson()
		{
			return Game1.getCharacterFromName(ModEntry.ModData.NpcRei);

			var tiles = Utils.GetTilesWithProperty(
				where: this,
				layer: "Buildings",
				property: ModEntry.ModData.ActionShrineShop, 
				onlyOne: true);
			var tile = tiles.FirstOrDefault();
			return this.isCharacterAtTile(tile);
		}

		public void UpdateFestivals()
        {
            bool wasFestival = this.IsSummerFestival;
			bool isFestival = Game1.netWorldState.Value.ActivePassiveFestivals.Any(s => s.StartsWith(ModEntry.ModData.ContentPrefix));

			// Festival ended
            if (wasFestival && !isFestival)
            {
                // Leave Charcoal after bonfire
                var area = new Rectangle(48, 49, 4, 3);
                Utils.SpawnObjectsInArea(
                    where: this,
                    area: area,
                    itemIds: [ModEntry.ModData.ItemCharcoal],
                    attempts: area.Width * area.Height,
                    max: 3);
            }

			this.IsSummerFestival = isFestival;
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
			if (this.ShouldChickenSpawnToday)
			{
				this.characters.RemoveWhere(c => c is ShrineChicken);
				if (Game1.timeOfDay < 1800)
				{
					this.addCharacter(new ShrineChicken(where: this, position: new Vector2(53, 33) * Game1.tileSize));
					this.addCharacter(new ShrineChicken(where: this, position: new Vector2(47, 49) * Game1.tileSize));
					this.addCharacter(new ShrineChicken(where: this, position: new Vector2(52, 50) * Game1.tileSize, isBrown: true));
				}
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
				CrowSpawnData entry = ModEntry.DecorSpawnsData.Value.CrowPerches[ModEntry.DecorSpawnsData.Value.CrowPerches.Keys.First(key => roll < key)];
				this.SpawnPerchedCrowsAt(phobos: entry.V1, deimos: entry.V2, Game1.IsWinter ? 0 : entry.R);

				// Spawn little crows
				this.SpawnBabyCrows();
			}
		}

		/// <summary>
		/// Attempts to add twin crows to the map as critters.
		/// </summary>
		public void TrySpawnGenericCrows()
		{
			const int retries = 25;
			Point radius = ModEntry.DecorSpawnsData.Value.CrowSpawnRadius.ToPoint();
			Point diameter = radius + radius;
			Rectangle spawnArea = ModEntry.DecorSpawnsData.Value.CrowSpawnArea;

			for (int attempts = 0; attempts < retries; ++attempts)
			{
				// Identify two separate nearby spawn positions for the crows around the map's middle
				Vector2 target = new Vector2(
					x: spawnArea.X + Game1.random.Next(spawnArea.Width),
					y: spawnArea.Y + Game1.random.Next(spawnArea.Height));
				Vector2 phobos = target - radius.ToVector2() + new Vector2(
					x: Game1.random.Next(diameter.X),
					y: Game1.random.Next(diameter.Y));
				Vector2 deimos = target - radius.ToVector2() + new Vector2(
					x: Game1.random.Next(diameter.X),
					y: Game1.random.Next(diameter.Y));

				if (phobos == deimos || !this.isTileLocationOpen(phobos) || !this.isTileLocationOpen(deimos))
					continue;

				this.SpawnGenericCrowsAt(phobos, deimos);
				return;
			}
		}

		public void ClearCrows()
		{
			this.critters.RemoveAll(critter => critter is ShrineCrow or Crow);
		}

		public void ClearCats()
		{
			this.critters.RemoveAll(critter => critter is ShrineCat);
		}

		public void ClearTempCharacters()
		{
			this.characters.RemoveWhere(chara => chara is Monster);
		}

		/// <summary>
		/// Attempts to add twin crows to the map as default Crow critters.
		/// </summary>
		public void SpawnGenericCrowsAt(Vector2 phobos, Vector2 deimos)
		{
			this.addCritter(new Crow((int)phobos.X, (int)phobos.Y));
			this.addCritter(new Crow((int)deimos.X, (int)deimos.Y));
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
		public void SpawnPerchedCrowsAt(Vector2 phobos, Vector2 deimos, float hopRange = 2)
		{
			this.ClearCrows();

			bool isDeimos = Game1.dayOfMonth % 3 == 0;   // Swap crow roles once every few days
			this.addCritter(new ShrineCrow(isDeimos: isDeimos, position: new Vector2(phobos.X, phobos.Y), hopRange: hopRange));
			this.addCritter(new ShrineCrow(isDeimos: !isDeimos, position: new Vector2(deimos.X, deimos.Y), hopRange: hopRange));
		}

		public void SpawnBabyCrows()
		{
			this.BabyCrows = new ShrineBabyCrowController(
				count: Game1.random.Next(0, 5),
				perches: ModEntry.DecorSpawnsData.Value.BabyCrowPerches,
				roosts: ModEntry.DecorSpawnsData.Value.BabyCrowRoosts);
		}

		public void TrySpawnDailyCats()
		{
			double roll = Game1.random.NextDouble();
			Vector2 position = Vector2.Zero;
			int baseFrame = ShrineCat.StandingBaseFrame;
			int scareRange = 0;
			bool flip = false;

			if (roll < 1f) // TODO: roll chance
			{
				// Test animation: Grooming
				//position = new Vector2(28, 48);
				position = new Vector2(23, 45);
				baseFrame = ShrineCat.StandingBaseFrame;
				scareRange = 3;
				flip = false;
			}

			this.SpawnCatsAt(position, baseFrame, scareRange, flip);
		}

		public void SpawnCatsAt(Vector2 position, int baseFrame, int scareRange, bool flip)
		{
			this.ClearCats();
			this.addCritter(new ShrineCat(position: position, baseFrame: baseFrame, scareRange: scareRange, flip: flip));
		}

		#endregion

		#region Festival methods

		public static void SetupFestival()
		{

		}

		public static void CleanupFestival()
		{

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
			Utility.getDefaultWarpLocation(ModEntry.ModData.MapShrine, ref tileLocation.X , ref tileLocation.Y);
			Game1.warpFarmer(locationName: ModEntry.ModData.MapShrine, tileX: tileLocation.X, tileY: tileLocation.Y, flip: false);
			Game1.fadeToBlackAlpha = 0.99f;
			Game1.screenGlow = false;
			Game1.player.temporarilyInvincible = false;
			Game1.player.temporaryInvincibilityTimer = 0;
			Game1.displayFarmer = true;
		}

		#endregion
    }
}
