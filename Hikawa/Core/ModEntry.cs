using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;

using xTile;
using xTile.Dimensions;
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
		
		private List<string> _maps;

		private enum NpcDir {
			Up,
			Right,
			Down,
			Left
		}

		private string GetI18nJp(string str)
		{
			return i18n.Get(Config.JapaneseNames ? "jp." : "" + str);
		}

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<Config>();

			// Testing
			//helper.Content.AssetEditors.Add(new Editors.TestEditor());

			// Data
			helper.Content.AssetEditors.Add(new Editors.NpcDataEditor());
			helper.Content.AssetEditors.Add(new Editors.EventEditor());

			// Locations
			helper.Content.AssetEditors.Add(new Editors.WorldEditor());
			helper.Content.AssetLoaders.Add(new Editors.MapLoader());

			// Events
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saved += OnSaved;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.Input.ButtonReleased += OnButtonReleased;

			helper.Events.Player.Warped += OnWarped;
		}

		#region Game Events

		private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
		{
			if (Game1.activeClickableMenu != null || Game1.player.UsingTool || Game1.pickingTool || Game1.menuUp
			    || (Game1.eventUp && !Game1.currentLocation.currentEvent.playerControlSequence)
			    || Game1.nameSelectUp || Game1.numberOfSelectedItems != -1)
				return;

			var btn = e.Button;

			// Additional world interactions
			CheckAction(btn);

			// Debug functions
			if (Config.DebugMode)
				DebugCommands(btn);
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
			foreach (var location in Game1.locations.Where(_ => _.map.Properties.ContainsKey(Const.ModId)).ToArray())
				Game1.locations.Remove(location);
		}

		private void OnWarped(object s, WarpedEventArgs e)
		{
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Preload maps
			var maps = new List<string>();
			foreach (var file in Directory.EnumerateFiles(
				Path.Combine(Helper.DirectoryPath, "assets", "Maps")))
			{
				var ext = Path.GetExtension(file);
				if (ext == null || !ext.Equals(".tbin"))
					continue;
				var map = Path.GetFileName(file);
				if (map == null)
					continue;
				try
				{
					Helper.Content.Load<Map>(Path.Combine("assets", "Maps", map));
					maps.Add(map);
					continue;
				}
				catch (Exception ex)
				{
					Log.E($"Unable to load {map}.\n" + ex);
				}
				Log.E($"Did not add {map}");
			}
			_maps = maps;

			// Preload NPCs
			/*
			try
			{
				var npc = new NPC();

				npc = new NPC(
					new AnimatedSprite(Helper.Content.GetActualAssetKey(
						$@"assets/Characters/{Const.ReiId + (Config.AnimePortraits ? "_jp" : "")}.png")),
					new Vector2(50, 50),
					(int)NpcDir.Right,
					GetI18nJp("npc.rei"));
			}
			catch (Exception ex)
			{
				Log.E("Unable to load NPCs.\n" + ex);
			}
			*/
		}

		#endregion

		/// <summary>
		/// Adds preloaded locations and NPCs into the game.
		/// </summary>
		private void Setup()
		{
			// Add new locations
			foreach (var map in _maps)
			{
				try
				{
					Log.D($"Adding new location: {Path.GetFileNameWithoutExtension(map)}",
						Config.DebugMode);

					var mapAssetKey = Helper.Content.GetActualAssetKey(
						Path.Combine("assets", "Maps", map));
					var loc = new GameLocation(
						mapAssetKey,
						Path.GetFileNameWithoutExtension(map))
						{ IsOutdoors = true, IsFarm = false };

					SetupLocation(loc);
					Game1.locations.Add(loc);
				}
				catch (Exception ex)
				{
					Log.E($"Unable to add {map}\n" + ex);
				}
			}

			var locShrine = Game1.getLocationFromName(Const.ModId);
			if (locShrine == null)
			{
				Log.E("Failed to load maps.");
				return;
			}
			
			// Generate NPCs
			/*
			try
			{
				var npcRei = new NPC(
					new AnimatedSprite(Helper.Content.GetActualAssetKey(
						Path.Combine("assets", "Characters", "{Const.ReiId + (Config.AnimePortraits ? "_jp" : "")}.png"))),
					new Vector2(50, 50),
					Const.ShrineId,
					(int)NpcDir.Right,
					GetI18nJp("npc.rei"),
					false,
					null,
					Helper.Content.Load<Texture2D>(
						Path.Combine("Portraits", $"{Const.ReiId}.png")));
				LoadNpcSchedule(npcRei);

				Game1.getLocationFromName(Const.ShrineId).addCharacter(npcRei);
			}
			catch (Exception ex)
			{
				Log.E("Unable to load NPCs.\n" + ex);
				return;
			}
			*/
		}

		private void SetupLocation(GameLocation location)
		{
			if (!location.map.Properties.ContainsKey(Const.ModId))
				location.map.Properties.Add(Const.ModId, true);
		}

		/// <summary>
		/// Forces an NPC into a custom schedule for the day.
		/// </summary>
		private void LoadNpcSchedule(NPC npc)
		{
			npc.Schedule = npc.getSchedule(Game1.dayOfMonth);
			npc.scheduleTimeToTry = 9999999;
			npc.ignoreScheduleToday = false;
			npc.followSchedule = true;
		}
		
		private void CheckAction(SButton btn)
		{
			if (btn.IsActionButton())
			{
				var grabTile = new Vector2(Game1.getOldMouseX() + Game1.viewport.X, 
					Game1.getOldMouseY() + Game1.viewport.Y) / Game1.tileSize;
				if (!Utility.tileWithinRadiusOfPlayer((int)grabTile.X, (int)grabTile.Y, 1, Game1.player))
					grabTile = Game1.player.GetGrabTile();
				var tile = Game1.currentLocation.map.GetLayer("Buildings").PickTile(
					new Location((int)grabTile.X * Game1.tileSize, (int)grabTile.Y * Game1.tileSize), 
					Game1.viewport.Size);
				var action = (PropertyValue)null;
				tile?.Properties.TryGetValue("Action", out action);
				if (action == null) return;

				// Enter the arcade machine minigame if used in the world
				var strArray = ((string)action).Split(' ');
				var args = new string[strArray.Length - 1];
				Array.Copy(strArray, 1, args, 0, args.Length);
				switch (strArray[0])
				{
					case Const.ArcadeMinigameId:
						Game1.currentMinigame = new LightGunGame.LightGunGame();
						break;
				}
			}
		}

		private void DebugCommands(SButton btn)
		{
			if (btn.Equals(Config.DebugPlayArcade))
			{
				Log.D($"Pressed {btn} : Playing {Const.ArcadeMinigameId}",
					Config.DebugMode);
				Game1.currentMinigame = new LightGunGame.LightGunGame();
			}
			else if (btn.Equals(Config.DebugWarpShrine))
			{
				Log.D($"Pressed {btn} : Warping to {Const.ShrineId}",
					Config.DebugMode);
				Game1.player.warpFarmer(new Warp(0, 0, Const.ShrineId, 19, 60, false));
			}
		}
	}
}

#region Nice Code

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

#endregion