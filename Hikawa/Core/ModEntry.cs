using System;
using Hikawa.Core;
using Microsoft.Xna.Framework;
using xTile.Dimensions;
using xTile.ObjectModel;

using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.BellsAndWhistles;

namespace Hikawa
{
	public class ModEntry : Mod
	{
		internal static ModEntry Instance;
		internal ModSaveData SaveData;
		private readonly OverlayEffectControl _overlayEffectControl = new OverlayEffectControl();

		internal Config Config;
		internal ITranslationHelper i18n => Helper.Translation;

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

			//helper.Content.AssetEditors.Add(new Editors.TestEditor());
			helper.Content.AssetEditors.Add(new Editors.WorldEditor());
			helper.Content.AssetEditors.Add(new Editors.EventEditor());
			helper.Content.AssetEditors.Add(new Editors.ArcadeEditor());

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.GameLoop.DayEnding += OnDayEnding;
			helper.Events.GameLoop.Saved += OnSaved;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

			helper.Events.Input.ButtonReleased += OnButtonReleased;
			helper.Events.Player.Warped += OnWarped;
		}

		#region Game Events

		/// <summary>
		/// Pre-game
		/// </summary>
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
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
		}

		/// <summary>
		/// Per-frame checks
		/// </summary>
		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
		}

		/// <summary>
		/// Button check
		/// </summary>
		private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
		{
			if (Game1.eventUp && !Game1.currentLocation.currentEvent.playerControlSequence
			    || Game1.activeClickableMenu != null || Game1.menuUp || Game1.nameSelectUp
				|| Game1.player.UsingTool || Game1.pickingTool
			    || Game1.numberOfSelectedItems != -1)
				return;

			var btn = e.Button;

			// Additional world interactions
			CheckAction(btn);

			// Debug functions
			if (Config.DebugMode)
				DebugCommands(btn);
		}

		/// <summary>
		/// Location changed
		/// </summary>
		private void OnWarped(object sender, WarpedEventArgs e)
		{
			if (e.NewLocation.Name.Equals("FarmHouse", StringComparison.CurrentCultureIgnoreCase))
				Game1.currentMinigame = new ArcadeGunGame();

			if (e.OldLocation.Name.Equals(e.NewLocation.Name)) return;

			if (_overlayEffectControl.IsEnabled())
				_overlayEffectControl.Disable();

			SetUpLocationSpecificFlair(Game1.currentLocation);
		}

		#endregion

		#region Manager Methods

		/// <summary>
		/// Adds unique elements to maps on entry.
		/// </summary>
		public void SetUpLocationSpecificFlair(GameLocation location)
		{
			switch (location.Name)
			{
				case ModConsts.ShrineMapId:
				{
					// Hikawa Shrine

					if (SaveData.StoryMist != (int)ModConsts.Progress.Started)
					{
						// Eerie effects

						_overlayEffectControl.Enable(OverlayEffectControl.Effect.Mist);
						SpawnCrows(
							location,
							new Location(
								location.Map.Layers[0].LayerWidth / 2 - 1,
								location.Map.Layers[0].LayerHeight / 10 * 9),
							new Location(
								location.Map.Layers[0].LayerWidth / 2 + 1,
								location.Map.Layers[0].LayerHeight / 10 * 9));
						if (!Game1.isRaining)
							Game1.changeMusicTrack("communityCenter");
					}
					else if (!Game1.isRaining)
					{
						// Crows on regular days

						if (Game1.timeOfDay < 1100)
						{
							// Spawn crows as critters
							SpawnCrows(location);
						}
						else if (!Game1.isDarkOut())
						{
							// Add crows as temp sprites
						}
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
		private static void SpawnCrows(GameLocation location)
		{
			if (!location.IsOutdoors)
				return;
			var rand = new Random();
			const int timeout = 5;
			for (var attempts = 0; attempts < timeout; ++attempts)
			{
				// Identify two separate nearby spawn positions for the crows around the map's middle
				var w = location.Map.Layers[0].LayerWidth;
				var h = location.Map.Layers[0].LayerHeight;
				var vTarget = new Location(
					rand.Next(w / 4, w / 4 * 3),
					rand.Next(h / 4, h / 4 * 3));
				var phobos = Location.Origin;
				var deimos = Location.Origin;
				for (var y = -1; y < 1; ++y)
				{
					for (var x = -1; x < 1; ++x)
					{
						if (location.isTilePassable(
							new Location(vTarget.X + x, vTarget.Y + y), Game1.viewport)) 
							phobos = new Location(vTarget.X + x, vTarget.X + y);
						if (location.isTilePassable(
							new Location(vTarget.X - x, vTarget.Y - y), Game1.viewport))
							deimos = new Location(vTarget.X - x, vTarget.X - y);
						if (phobos == deimos && phobos != Location.Origin)
							break;
						if (phobos == deimos || phobos == Location.Origin || deimos == Location.Origin)
							continue;
						SpawnCrows(location, phobos, deimos);
						return;
					}
				}
			}
			Log.D($"Failed to add crows after {timeout} attempts.");
		}

		/// <summary>
		/// Attempts to add twin crows to the map as critters.
		/// </summary>
		private static void SpawnCrows(GameLocation location, Location phobos, Location deimos)
		{
			Log.W($"Adding crows at {phobos.ToString()} and {deimos.ToString()}");
			location.addCritter(new Crow(phobos.X, phobos.Y));
			location.addCritter(new Crow(deimos.X, deimos.Y));
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
		
		private void CheckAction(SButton btn)
		{
			if (btn.IsActionButton())
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

				// Enter the arcade machine minigame if used in the world
				var strArray = ((string)action).Split(' ');
				var args = new string[strArray.Length - 1];
				Array.Copy(
					strArray, 1, 
					args, 0, 
					args.Length);
				switch (strArray[0])
				{
					case ModConsts.ArcadeMinigameId:
						Game1.currentMinigame = new ArcadeGunGame();
						break;
				}
			}
		}

		#endregion

		#region Debug Methods

		private void DebugCommands(SButton btn)
		{
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
				Log.D($"Pressed {btn} : Warping to {ModConsts.ShrineMapId}",
					Config.DebugMode);
				if (true)
					Game1.player.warpFarmer(
						new Warp(0, 0, "Town",
							20, 5, true));
				else
					Game1.player.warpFarmer(
						new Warp(0, 0, ModConsts.ShrineMapId,
							19, 60, false));
			}
		}

		#endregion
	}
}

#region Nice Code

// nice code

/*

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