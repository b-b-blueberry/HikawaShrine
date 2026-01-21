using Hikawa.Data;
using Hikawa.Modules;
using Hikawa.Objects.Items;
using Hikawa.Objects.Locations;
using Hikawa.Volleyball;
using StardewModdingAPI;
using System;
using System.Linq;
using System.Reflection;

namespace Hikawa
{
	public static class ConsoleCommands
	{
		[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
		private sealed class ConsoleCommandAttribute(string alias, string description) : Attribute
		{
			public readonly string Alias = alias;
			public readonly string Description = description;
		}

		public static void RegisterAll(IModHelper helper, string prefix)
		{
			foreach (var method in typeof(ConsoleCommands).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
			{
				if (method.GetCustomAttribute<ConsoleCommandAttribute>() is ConsoleCommandAttribute attribute)
				{
					foreach (string name in new string[] { method.Name, attribute.Alias })
						if (!string.IsNullOrWhiteSpace(name))
							helper.ConsoleCommands.Add(
								name: prefix + name,
								documentation: attribute.Description,
								callback: (Action<string, string[]>)method.CreateDelegate(typeof(Action<string, string[]>)));
				}
			}
		}

		[ConsoleCommandAttribute("b", "Test")]
		private static void bbb(string s, string[] args)
		{
			// Fix hanging sprites
			// foreach (HangingSprite sprite in Game1.currentLocation.critters.Where(c => c is HangingSprite)) sprite.ResetRotation();

			// Spawn crows
			// Game1.getFarm().addCrows();

			//Game1.player.addItemToInventory(new BugTool());

			//Game1.activeClickableMenu = new BugCollectionMenu();

			return;
		}

		[ConsoleCommandAttribute("bg", "Catch a bug (name, qty) at the current date")]
        private static void bug(string s, string[] args)
        {
            if (!ArgUtility.TryGet(args, 0, out string bugId, out string error))
            {
                Log.E($"No bug ID provided: {error}");
                return;
            }
            if (!ModEntry.BugsData.Value.BugData.ContainsKey(bugId))
            {
                Log.E($"No bug data matching ID '{bugId}'");
                return;
            }
            BugTool.CatchBug(bugId, ArgUtility.GetInt(args, 1, 1));
            Log.D($"{bugId} total: {ModEntry.SaveData.BugCollection[bugId].Count}");
        }

		[ConsoleCommandAttribute("v", "Volleyball starter")]
        private static void volleyball(string s, string[] args)
		{
			VolleyballLocation location = VolleyballLocation.MakeTemp();
			location.SetUpLocation(rules:
				false ? new VolleyballRules(
					players: new Character[]
					{
						Game1.player,
						VolleyballNPC.MakeFor(ModEntry.ModData.NpcRei)
					},
					scoreGoal: 3,
					isDoubles: false)
				: null,
				umpireName: ModEntry.ModData.NpcCat,
				style: Volleyball.Volleyball.Style.Volleyball);
		}

		[ConsoleCommandAttribute("3", "Match3 starter")]
        private static void match3(string s, string[] args)
		{
			// Create game
			string stage = args.Length > 0 ? args[0] : null;
			Match3.Match3MainMenu menu = new Match3.Match3MainMenu(stage);
			Game1.activeClickableMenu = menu;
		}

		[ConsoleCommandAttribute("bc", "Play island boat transition")]
        private static void boat(string s, string[] args)
		{
			Game1.currentMinigame = new Objects.Events.BoatCutscene();
		}

		[ConsoleCommandAttribute("s", "Warp to Hikawa Shrine")]
        private static void shrine(string s, string[] args)
		{
			warpTo(locationName: ModEntry.ModData.MapShrine);
		}

		[ConsoleCommandAttribute("h", "Warp to Hikawa House")]
        private static void house(string s, string[] args)
		{
			warpTo(locationName: ModEntry.ModData.MapHouse);
		}

        [ConsoleCommandAttribute("l", "Warp to Hikawa Hall")]
        private static void hall(string s, string[] args)
        {
            warpTo(locationName: ModEntry.ModData.MapHall);
        }

        [ConsoleCommandAttribute("g", "Warp to Hikawa Grove")]
        private static void grove(string s, string[] args)
        {
            warpTo(locationName: ModEntry.ModData.MapGrove);
        }

		[ConsoleCommandAttribute("o", "Manage screen overlays: use [0~num]")]
        private static void overlay(string s, string[] args)
		{
			if (args.Length < 1)
			{
				Log.D($"Current effect: {ModEntry.OverlayEffectControl.CurrentEffect()}");
			}
			else
			{
				try
				{
					ModEntry.OverlayEffectControl.Enable((OverlayEffectControl.Effect)int.Parse(args[0]));
					return;
				}
				catch (FormatException) { }
				ModEntry.OverlayEffectControl.Toggle();
			}
		}

		[ConsoleCommandAttribute("c", "Respawn twin crows at the shrine")]
        private static void crows(string s, string[] args)
		{
			Shrine shrine = Shrine.Get();
			shrine.ClearCrows();
			shrine.TrySpawnGenericCrows();
		}

		[ConsoleCommandAttribute("cc", "Respawn perched crows at the shrine")]
        private static void crows2(string s, string[] args)
		{
			Shrine shrine = Shrine.Get();
			int which = args.Length > 0 ? int.Parse(args[0]) : Game1.random.Next(0, ModEntry.DecorSpawnsData.Value.CrowPerches.Keys.Count);
			CrowSpawnData entry = ModEntry.DecorSpawnsData.Value.CrowPerches[ModEntry.DecorSpawnsData.Value.CrowPerches.Keys.ToArray()[which]];
			shrine.ClearCrows();
			shrine.SpawnPerchedCrowsAt(phobos: entry.V1, deimos: entry.V2, hopRange: entry.R);
		}

		[ConsoleCommandAttribute("cb", "Play CrystalBall event")]
        private static void crystalball(string s, string[] args)
		{
			Game1.globalFadeToBlack(afterFade: () =>
			{
				Game1.viewport.X = -64000;
				Game1.viewport.Y = -64000;

				// Note: Save this script for the grandpa roof and moon cutscene
				string who = ModEntry.ModData.NpcGramps;
				string script = $"nightTime/-1000 -1000/farmer 0 0 0 {who} 1 0 0/skippable/pause 1000/changeToTemporaryMap {ModEntry.ModData.MapRoof}/warp farmer 14 32/warp {who} 16 32/faceDirection farmer 2/faceDirection {who} 2/pause 1000/viewport move 0 1 5500/{ModEntry.ModData.EventCommandCrystalBall}/pause 1000/globalFade/viewport -1000 -1000/pause 1000/end";
				script = $"nightTime/-1000 -1000/farmer -100 -100 0 {who} -101 -100 0/skippable/pause 1000/{ModEntry.ModData.EventCommandCrystalBall}/pause 1000/end";
				Game1.currentLocation.currentEvent = new(eventString: script)
				{
					onEventFinished = () => Game1.player.stopGlowing()
				};
				Game1.eventUp = true;
			});
		}

		private static void warpTo(string locationName)
		{
			Point tile = Point.Zero;
			Utility.getDefaultWarpLocation(locationName: locationName, x: ref tile.X, y: ref tile.Y);
			Game1.warpFarmer(locationName: locationName, tileX: tile.X, tileY: tile.Y, flip: false);
		}
	}
}
