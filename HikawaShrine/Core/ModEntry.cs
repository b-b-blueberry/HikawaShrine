
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using xTile.ObjectModel;

using StardewValley;

using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace HikawaShrine
{
	public class ModEntry : Mod
	{
		internal static ModEntry Instance;

		internal Config Config;
		internal ITranslationHelper i18n => Helper.Translation;
		
		private bool _isJapaneseNames;
		private bool _isJapanesePortraits;

		private List<string> _maps;

		private enum NPCDirection {
			Up,
			Right,
			Down,
			Left
		}

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			// Setup from Config.
			Config = helper.ReadConfig<Config>();
			_isJapaneseNames = !Config.Names.Equals(Const.ConfigRoman);
			_isJapanesePortraits = !Config.Portraits.Equals(Const.ConfigRoman);
			
			// Inject NPC data.
			helper.Content.AssetEditors.Add(new Editors.NPCDataEditor());
			// Inject events data.
			helper.Content.AssetEditors.Add(new Editors.EventEditor());

			// Setup events.
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saved += OnSaved;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.Input.ButtonReleased += OnButtonReleased;

			// Handle custom layer drawing.
			helper.Events.Player.Warped += OnWarped;
		}
		
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Load maps.
			var maps = new List<string>();
			foreach (var file in Directory.EnumerateFiles(Path.Combine(Helper.DirectoryPath, Const.MapsPath)))
			{
				var ext = Path.GetExtension(file);
				if (ext == null || !ext.Equals(Const.MapExtension))
					continue;
				var map = Path.GetFileName(file);
				if (map == null)
					continue;
				try
				{
					Helper.Content.Load<Map>(Path.Combine(Const.MapsPath, map));
					maps.Add(map);
					continue;
				}
				catch (Exception err)
				{
					Log.E("Unable to load " + map);
					Log.E(err.ToString());
				}
				Log.E("Did not add " + map);
			}
			_maps = maps;

			// Load NPCs.
			/*
			try
			{
				var npcRei = new NPC(
				new AnimatedSprite(Helper.Content.GetActualAssetKey(Path.Combine(Const.ASSETS_PATH, "Characters", "Rei.png"))),
				new Vector2(50, 50),
				(int)NPCDirection.Down,
				i18n.Get("npc.name.rei"));
			}
			catch (Exception err)
			{
				Log.E("Unable to load NPCs.\n" + err);
			}
			*/
		}

		private void Setup()
		{
			/* Location Patching */

			// Add new locations.

			foreach (var map in _maps)
			{
				try
				{
					Log.D("Adding new location: " + Path.GetFileNameWithoutExtension(map), Config.DebugMode);

					var mapAssetKey = Helper.Content.GetActualAssetKey(
						Path.Combine(Const.MapsPath, map));
					var loc = new GameLocation(
						mapAssetKey,
						Path.GetFileNameWithoutExtension(map))
						{ IsOutdoors = true, IsFarm = false };

					SetupLocation(loc);
					Game1.locations.Add(loc);
				}
				catch (Exception e)
				{
					Log.E("Unable to add '" + map + "'.\n" + e);
				}
			}

			GameLocation locShrine = Game1.getLocationFromName(Const.ShrineMapName);
			if (locShrine == null)
			{
				Log.E("Failed to load new maps.");
				return;
			}

			// Patch exiting locations.
			try {
				// Saloon - Sailor V Arcade Machine.
				var locSaloon = Game1.getLocationFromName("Saloon");
				var tileSheetPath = Helper.Content.GetActualAssetKey(
					Path.Combine(Const.MapsPath, Const.MiscSpritesFile + ".png"));

				Log.D("Patching Saloon.", Config.DebugMode);

				const BlendMode mode = BlendMode.Additive;
				var tileSheet = new TileSheet(
					Const.MiscSpritesFile,
					locSaloon.Map,
					tileSheetPath,
					new Size(48, 16),
					new Size(16, 16));
				locSaloon.Map.AddTileSheet(tileSheet);
				locSaloon.Map.LoadTileSheets(Game1.mapDisplayDevice);
				var layer = locSaloon.Map.GetLayer("Front");
				StaticTile[] tileAnim = {
					new StaticTile(layer, tileSheet, mode, 0),
					new StaticTile(layer, tileSheet, mode, 2)
				};

				layer.Tiles[40, 16] = new AnimatedTile(layer, tileAnim, 1000);
				layer = locSaloon.Map.GetLayer("Buildings");
				layer.Tiles[40, 17] = new StaticTile(layer, tileSheet, mode, 1);
				layer.Tiles[40, 17].Properties.Add(Const.TileActionID, new PropertyValue(Const.ArcadeMinigameName));
				layer = locSaloon.Map.GetLayer("Back");
				layer.Tiles[40, 18] = layer.Tiles[35, 18];

				// Bus Stop - Shrine Entrance.
				
				/*
				 * 
				 * todo
				 * 
				 */
			}
			catch (Exception e)
			{
				Log.E("Failed to patch map files.");
				Log.E(e.ToString());
				return;
			}

			// Generate NPCs.
			/*
			try
			{
				var npcRei = new NPC(
				new AnimatedSprite(Helper.Content.GetActualAssetKey(Path.Combine(Const.AssetsPath, "Rei.png"))),
				new Vector2(50, 50),		// position
				Const.ShrineMap,			// defaultMap
				(int)NPCDirection.Down,		// facingDir
				i18n.Get("npc.name.rei"),	// name
				false,						// datable
				//npc.Schedule = Helper.Reflection.GetMethod(npc, "parseMasterSchedule").Invoke<Dictionary<int, SchedulePathDescription>>("610 80 20 2/630 23 20 2/710 80 20 2/730 23 20 2/810 80 20 2/830 23 20 2/910 80 20 2/930 23 20 2");
				Helper.Content.Load<Texture2D>(assetsPath + "/Portraits/Rei.png")
				);

				Game1.getLocationFromName(Const.ShrineMap).addCharacter(npcRei);
			}
			catch (Exception err)
			{
				Log.XXXXXXXXXXXXXXXX("Unable to load new NPCs.\n" + err.ToString());
				return;
			}
				*/

			// Debug warping.
			//Game1.getLocationFromName("Farm").warps.Add(new Warp(70, 9, Const.ShrineMap, 32, 59, false));

			// Return warping.
			/*
			hubloc.setTileProperty(30, 60, "Buildings", "Action", "Warp 70 10 Farm");
			hubloc.setTileProperty(31, 60, "Buildings", "Action", "Warp 70 10 Farm");
			hubloc.setTileProperty(32, 60, "Buildings", "Action", "Warp 70 10 Farm");
			hubloc.setTileProperty(33, 60, "Buildings", "Action", "Warp 70 10 Farm");
			hubloc.setTileProperty(34, 60, "Buildings", "Action", "Warp 70 10 Farm");
			*/

			// Enter warping.

			/*
			 * 
			 * todo
			 * 
			*/
		}

		private void SetupLocation(GameLocation location)
		{
			if (!location.map.Properties.ContainsKey(Const.MapID))
				location.map.Properties.Add(Const.MapID, true);
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
			foreach (var location in Game1.locations.Where(_ => _.map.Properties.ContainsKey(Const.MapID)).ToArray())
				Game1.locations.Remove(location);
		}

		private void OnWarped(object s, WarpedEventArgs e)
		{
			if (e.IsLocalPlayer)
			{
				// Draw layers behind the Back layer.
				e.OldLocation.map.GetLayer("Back").BeforeDraw -= DrawFarBack;
				e.NewLocation.map.GetLayer("Back").BeforeDraw += DrawFarBack;

				// Draw layers appaering between the Back layer and the player.
				e.OldLocation.map.GetLayer("Back").AfterDraw -= DrawNearBack;
				e.NewLocation.map.GetLayer("Back").AfterDraw += DrawNearBack;

				// Draw layers appearing between the player and the Front layer.
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
			e.Button.TryGetKeyboard(out var keyPressed);

			if (Game1.activeClickableMenu == null) return;

			// Debug - Arcade Machine popup hotkey.
			if (keyPressed.ToSButton().Equals(Config.DebugArcadePlayGame))
			{
				Log.D("Arcade hotkey pressed", Config.DebugMode);
				Game1.currentMinigame = new LightGunGame.LightGunGame();
			}
		}

		private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
		{
			if (Game1.activeClickableMenu != null || Game1.player.UsingTool || Game1.pickingTool || Game1.menuUp ||
			    (Game1.eventUp && !Game1.currentLocation.currentEvent.playerControlSequence) || Game1.nameSelectUp ||
			    Game1.numberOfSelectedItems != -1) return;

			// Additional world interactions.
			if (e.Button.IsActionButton())
			{
				// Dodge wrong button presses
				var grabTile = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;
				if (!Utility.tileWithinRadiusOfPlayer((int)grabTile.X, (int)grabTile.Y, 1, Game1.player))
					grabTile = Game1.player.GetGrabTile();
				var tile = Game1.currentLocation.map.GetLayer("Buildings").PickTile(
					new Location((int)grabTile.X * Game1.tileSize, (int)grabTile.Y * Game1.tileSize), Game1.viewport.Size);
				var action = (PropertyValue) null;
				tile?.Properties.TryGetValue(Const.TileActionID, out action);
				if (action == null) return;

				// Enter the arcade machine minigame if used in the world
				var strArray = ((string)action).Split(' ');
				var args = new string[strArray.Length - 1];
				Array.Copy(strArray, 1, args, 0, args.Length);
				switch (strArray[0])
				{
					case Const.ArcadeMinigameName:
						Game1.currentMinigame = new LightGunGame.LightGunGame();
						break;
				}
			}
		}
	}
}

// nice code

/*
 
public override bool isCollidingPosition(Microsoft.Xna.Framework.Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character)
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
    if (this.isCharacterAtTile(new Vector2((float) tileLocation.X, (float) (tileLocation.Y - 2))) != null || this.isCharacterAtTile(new Vector2((float) tileLocation.X, (float) (tileLocation.Y - 1))) != null || this.isCharacterAtTile(new Vector2((float) tileLocation.X, (float) (tileLocation.Y - 3))) != null)
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
