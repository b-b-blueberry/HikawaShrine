
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using xTile;
using xTile.Dimensions;
using xTile.Layers;

using StardewValley;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using xTile.Tiles;
using xTile.ObjectModel;

namespace HikawaShrine
{
	public class Hikawa : Mod
	{
		internal static IModHelper SHelper;
		internal static IMonitor SMonitor;
		internal static IAssetLoader SLoader;
		internal static IAssetEditor SEditor;

		internal Config mConfig;
		internal ITranslationHelper i18n => Helper.Translation;
		
		private bool isJapaneseNames;
		private bool isJapanesePortraits;

		private List<string> mMaps;

		enum eDir {
			UP,
			RIGHT,
			DOWN,
			LEFT
		}

		public override void Entry(IModHelper helper)
		{
			string startingMessage = i18n.Get("template.start", new { mod = helper.ModRegistry.ModID, folder = helper.DirectoryPath });
			Monitor.Log(startingMessage, LogLevel.Debug);

			mConfig = helper.ReadConfig<Config>();
			isJapaneseNames = mConfig.Names.Equals(Const.CONFIG_ROMAN) ? false : true;
			isJapanesePortraits = mConfig.Portraits.Equals(Const.CONFIG_ROMAN) ? false : true;

			// Define internals
			SHelper = helper;
			SMonitor = Monitor;

			// Reload assets customised by the arcade game
			// ie. LooseSprites/Cursors
			Hikawa.SHelper.Content.AssetEditors.Add(new HikawaAssetEditor());

			// Setup events
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saved += OnSaved;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.Input.ButtonReleased += OnButtonReleased;

			// Handle custom layer drawing
			helper.Events.Player.Warped += OnWarped;
		}
		
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			var helper = Helper;

			// Load maps
			List<string> maps = new List<string>();
			foreach (string file in Directory.EnumerateFiles(Path.Combine(Helper.DirectoryPath, Const.ASSETS_PATH, Const.MAPS_PATH)))
			{
				string ext = Path.GetExtension(file);
				if (ext == null || !ext.Equals(Const.MAP_EXT))
					continue;
				string map = Path.GetFileName(file);
				if (map == null)
					continue;
				try
				{
					Helper.Content.Load<Map>(Path.Combine(Const.ASSETS_PATH, Const.MAPS_PATH, map), ContentSource.ModFolder);
					maps.Add(map);
					continue;
				}
				catch (Exception err)
				{
					Monitor.Log("[hi] Unable to load " + map, LogLevel.Error);
					Monitor.Log(err.ToString(), LogLevel.Error);
				}
				Monitor.Log("[hi] Did not add " + map, LogLevel.Error);
			}
			mMaps = maps;

			// Load NPCs
			/*
			try
			{
				var npcRei = new NPC(
				new AnimatedSprite(Helper.Content.GetActualAssetKey(Path.Combine(Const.ASSETS_PATH, "Characters", "Rei.png"))),
				new Vector2(50, 50),	// position
				(int)eDir.DOWN,			// facingDir
				"Rei"					// name
				);
			}
			catch (Exception err)
			{
				Monitor.Log("[hi] Unable to load NPCs.\n" + err.ToString(), LogLevel.Error);
			}
			*/
		}

		private void Setup()
		{
			/* Location Patching */

			// Add new locations

			foreach (string map in mMaps)
			{
				try
				{
					Monitor.Log("[hi] Adding new location: " + Path.GetFileNameWithoutExtension(map), LogLevel.Debug);
					var mapAssetKey = Helper.Content.GetActualAssetKey(Path.Combine(Const.ASSETS_PATH, Const.MAPS_PATH, map), ContentSource.ModFolder);
					var loc = new GameLocation(mapAssetKey, Path.GetFileNameWithoutExtension(map)) {
						IsOutdoors = true, IsFarm = false };
					SetupLocation(loc);
					Game1.locations.Add(loc);
				}
				catch (Exception e)
				{
					Monitor.Log("[hi] Unable to add '" + map + "'.\n", LogLevel.Error);
					Monitor.Log(e.ToString(), LogLevel.Error);
				}
			}

			GameLocation locShrine = Game1.getLocationFromName(Const.MAP_SHRINE);
			if (locShrine == null)
			{
				Monitor.Log("[hi] Failed to load new maps.", LogLevel.Error);
				return;
			}

			// Patch exiting locations
			try {

				// DEBUG
				// todo remove

				// FarmHouse - Sailor V Arcade Machine
				GameLocation locFarmhouse = Game1.getLocationFromName("FarmHouse");
				string tileSheetPathFarmhouse = Helper.Content.GetActualAssetKey(
					Path.Combine(Const.ASSETS_PATH, Const.MAPS_PATH, Const.ASSET_MISC + Const.ASSET_EXT),
					ContentSource.ModFolder);

				Monitor.Log("[hi] Patching : Farmhouse. !! DEBUG", LogLevel.Debug);

				TileSheet tileSheetFarmhouse = new TileSheet(
					Const.ASSET_MISC,
					locFarmhouse.Map,
					tileSheetPathFarmhouse,
					new Size(48, 16),
					new Size(16, 16));
				locFarmhouse.Map.AddTileSheet(tileSheetFarmhouse);
				locFarmhouse.Map.LoadTileSheets(Game1.mapDisplayDevice);
				Layer layerFarmhouse = locFarmhouse.map.GetLayer("Front");
				StaticTile[] tileAnimFarmhouse = {
					new StaticTile(layerFarmhouse, tileSheetFarmhouse, BlendMode.Additive, 0),
					new StaticTile(layerFarmhouse, tileSheetFarmhouse, BlendMode.Additive, 2)
				};

				layerFarmhouse.Tiles[8, 7] = new AnimatedTile(layerFarmhouse, tileAnimFarmhouse, 1000);
				layerFarmhouse = locFarmhouse.map.GetLayer("Buildings");
				layerFarmhouse.Tiles[8, 8] = new StaticTile(layerFarmhouse, tileSheetFarmhouse, BlendMode.Additive, 1);
				layerFarmhouse.Tiles[8, 8].Properties.Add(Const.ACTION_KEY, new PropertyValue(Const.ARCADE_MINIGAME));


				// Saloon - Sailor V Arcade Machine
				GameLocation locSaloon = Game1.getLocationFromName("Saloon");
				string tileSheetPath = Helper.Content.GetActualAssetKey(
					Path.Combine(Const.ASSETS_PATH, Const.MAPS_PATH, Const.ASSET_MISC + Const.ASSET_EXT),
					ContentSource.ModFolder);

				Monitor.Log("[hi] Patching Saloon.", LogLevel.Debug);

				TileSheet tileSheet = new TileSheet(
					Const.ASSET_MISC,
					locSaloon.Map,
					tileSheetPath,
					new Size(48, 16),
					new Size(16, 16));
				locSaloon.Map.AddTileSheet(tileSheet);
				locSaloon.Map.LoadTileSheets(Game1.mapDisplayDevice);
				Layer layer = locSaloon.map.GetLayer("Front");
				StaticTile[] tileAnim = {
					new StaticTile(layer, tileSheet, BlendMode.Additive, 0),
					new StaticTile(layer, tileSheet, BlendMode.Additive, 2)
				};

				layer.Tiles[40, 16] = new AnimatedTile(layer, tileAnim, 1000);
				layer = locSaloon.map.GetLayer("Buildings");
				layer.Tiles[40, 17] = new StaticTile(layer, tileSheet, BlendMode.Additive, 1);
				layer.Tiles[40, 17].Properties.Add(Const.ACTION_KEY, new PropertyValue(Const.ARCADE_MINIGAME));
				layer = locSaloon.map.GetLayer("Back");
				layer.Tiles[40, 18] = layer.Tiles[35, 18];

				// Bus Stop - Shrine Entrance

				// . . .
			}
			catch (Exception e)
			{
				Monitor.Log("[hi] Failed to patch map files.", LogLevel.Error);
				Monitor.Log(e.ToString(), LogLevel.Error);
				return;
			}

			// Generate NPCs
			/*
			try
			{
				var npcRei = new NPC(
				new AnimatedSprite(Helper.Content.GetActualAssetKey(assetsPath + "/Rei.png")),
				new Vector2(50, 50),    // position
				"Shrine",               // defaultMap
				(int)eDir.down,         // facingDir
				"Rei",                  // name
				false,                  // datable
				//npc.Schedule = Helper.Reflection.GetMethod(npc, "parseMasterSchedule").Invoke<Dictionary<int, SchedulePathDescription>>("610 80 20 2/630 23 20 2/710 80 20 2/730 23 20 2/810 80 20 2/830 23 20 2/910 80 20 2/930 23 20 2");
				Helper.Content.Load<Texture2D>(assetsPath + "/Portraits/Rei.png")
				);

				Game1.getLocationFromName("Shrine").addCharacter(npcRei);
			}
			catch (Exception err)
			{
				Monitor.Log("[hi] Hikawa was unable to load new NPCs.\n" + err.ToString(), LogLevel.Error);
				return;
			}
				*/

			// Debug warping
			//Game1.getLocationFromName("Farm").warps.Add(new Warp(70, 9, LOC_SHRINE, 32, 59, false));

			// Return warping
			/*
			hubloc.setTileProperty(30, 60, "Buildings", "Action", "Warp 70 10 Farm");
			hubloc.setTileProperty(31, 60, "Buildings", "Action", "Warp 70 10 Farm");
			hubloc.setTileProperty(32, 60, "Buildings", "Action", "Warp 70 10 Farm");
			hubloc.setTileProperty(33, 60, "Buildings", "Action", "Warp 70 10 Farm");
			hubloc.setTileProperty(34, 60, "Buildings", "Action", "Warp 70 10 Farm");
			*/

			// Enter warping

			/*
			 * 
			 * todo
			 * 
			*/
		}

		private void SetupLocation(GameLocation location)
		{
			if (!location.map.Properties.ContainsKey(Const.MAP_KEY))
				location.map.Properties.Add(Const.MAP_KEY, true);
		}

		private void OnSaveLoaded(object s, EventArgs e)
		{
			Setup();
		}

		private void OnSaved(object s, EventArgs e)
		{
			Setup();
		}

		private void OnSaving(object s, EventArgs e)
		{
			foreach (var location in Game1.locations.Where(_ => _.map.Properties.ContainsKey(Const.MAP_KEY)).ToArray())
				Game1.locations.Remove(location);
		}

		private void OnWarped(object s, WarpedEventArgs e)
		{
			if (e.IsLocalPlayer)
			{
				// Draw layers behind the Back layer
				e.OldLocation.map.GetLayer("Back").BeforeDraw -= DrawFarBack;
				e.NewLocation.map.GetLayer("Back").BeforeDraw += DrawFarBack;

				// Draw layers appaering between the Back layer and the player
				e.OldLocation.map.GetLayer("Back").AfterDraw -= DrawNearBack;
				e.NewLocation.map.GetLayer("Back").AfterDraw += DrawNearBack;

				// Draw layers appearing between the player and the Front layer
				e.OldLocation.map.GetLayer("Front").BeforeDraw -= DrawNearFront;
				e.NewLocation.map.GetLayer("Front").BeforeDraw += DrawNearFront;
			}
		}

		private void DrawFarBack(object s, LayerEventArgs e)
		{
			Game1.currentLocation.map.GetLayer("FarBack")?.Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
		}

		private void DrawNearBack(object s, LayerEventArgs e)
		{
			Game1.currentLocation.map.GetLayer("NearBack")?.Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
		}

		private void DrawNearFront(object s, LayerEventArgs e)
		{
			Game1.currentLocation.map.GetLayer("NearFront")?.Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			e.Button.TryGetKeyboard(out Keys keyPressed);

			// Debug - Arcade Machine popup hotkey
			if (keyPressed.ToSButton().Equals(mConfig.Debug_ArcadePlayGame))
			{
				Monitor.Log("hhhhh", LogLevel.Debug);
				Game1.currentMinigame = new LightGunGame.LightGunGame();
			}
		}

		private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
		{
			if (Game1.activeClickableMenu == null && !Game1.player.UsingTool && !Game1.pickingTool && !Game1.menuUp && (!Game1.eventUp || Game1.currentLocation.currentEvent.playerControlSequence) && !Game1.nameSelectUp && Game1.numberOfSelectedItems == -1)
			{
				// Additional world interactions
				if (e.Button.IsActionButton())
				{
					// Sundrop
					Vector2 grabTile = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;
					if (!Utility.tileWithinRadiusOfPlayer((int)grabTile.X, (int)grabTile.Y, 1, Game1.player))
						grabTile = Game1.player.GetGrabTile();
					Tile tile = Game1.currentLocation.map.GetLayer("Buildings").PickTile(new Location((int)grabTile.X * Game1.tileSize, (int)grabTile.Y * Game1.tileSize), Game1.viewport.Size);
					PropertyValue action = null;
					tile?.Properties.TryGetValue(Const.ACTION_KEY, out action);
					if (action != null)
					{
						string[] strArray = ((string)action).Split(' ');
						string[] args = new string[strArray.Length - 1];
						Array.Copy(strArray, 1, args, 0, args.Length);
						switch (strArray[0])
						{
							case Const.ARCADE_MINIGAME:
								Game1.currentMinigame = new LightGunGame.LightGunGame();
								break;
						}
					}
				}
			}
		}
	}
}
