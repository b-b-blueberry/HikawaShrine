using StardewValley;
using StardewValley.Extensions;
using StardewValley.Mods;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Modules
{
    public static class DialoguePicker
    {
        private static ModDataDictionary Data => Game1.player.modData;

		private static readonly Dictionary<long, string> Sessions = [];
		private static readonly HashSet<string> UsedKeys = [];
		private static long PreviousSessionId;

		private const string DataKeyUsedKeys = ModConsts.SaveDataKey + "UsedKeys";
		private const string DataKeyDaysWhenLastTalkedToRei = ModConsts.SaveDataKey + "DaysWhenLastTalkedToRei";
        private const string DataKeyDaysWhenLastTalkedToAmi = ModConsts.SaveDataKey + "DaysWhenLastTalkedToAmi";
        private const string DataKeyDaysWhenLastPlayed = ModConsts.SaveDataKey + "DaysWhenLastPlayed";

		public static void LoadData()
		{
			Sessions[Game1.player.UniqueMultiplayerID] = Game1.player.Name;

			UsedKeys.Clear();
			UsedKeys.AddRange(Data[DataKeyUsedKeys]?.Split(',') ?? []);
		}

        public static void SaveData()
        {
			PreviousSessionId = Game1.player.UniqueMultiplayerID;

            Data[DataKeyDaysWhenLastPlayed] = Game1.Date.TotalDays.ToString();
			Data[DataKeyUsedKeys] = string.Join(',', UsedKeys);
        }
        
		public static Dialogue GetDailyUnique(NPC who)
		{
			Dialogue dialogue = null;
			string key = null;
			object tokens = null;

			int i;
			string s;

			if (who.Name == ModConsts.NpcRei)
			{
				// Locations
				if (key is null
					&& Game1.player.locationsVisited.Contains(ModConsts.MapHall)
					&& !UsedKeys.Contains("psych.place.hall"))
				{
					key = "psych.place.hall";
				}

				// Absence
				if (key is null
					&& Data.TryGetValue(DataKeyDaysWhenLastTalkedToRei, out s)
					&& int.TryParse(s, out i))
				{
					if (i > WorldDate.DaysPerMonth * WorldDate.MonthsPerYear
						&& !UsedKeys.Contains("psych.away.years"))
					{
						key = "psych.away.years";
					}
					else if (i > WorldDate.DaysPerMonth
						&& !UsedKeys.Contains("psych.away.months"))
					{
						key = "psych.away.months";
					}
					else if (i > 7
						&& !UsedKeys.Contains("psych.away.weeks"))
					{
						key = "psych.away.weeks";
					}
				}

				// Viewers
				if (key is null
					&& Steamworks.SteamVideo.IsBroadcasting(out i) && i > 0
					&& !UsedKeys.Contains("psych.system.broadcast"))
				{
					key = "psych.system.broadcast";
				}

				// Splitscreen
				if (key is null
					&& StardewModdingAPI.Context.IsSplitScreen)
				{
					if (Game1.player.IsMainPlayer
						&& !UsedKeys.Contains("psych.coop.main"))
					{
						key = "psych.coop.main";
					}
					else if (!Game1.player.IsMainPlayer
						&& !UsedKeys.Contains("psych.coop.other"))
					{
						key = "psych.coop.other";
					}
				}

				// Real time
				if (key is null)
				{
					if (DateTime.Now.Hour is > 2 and < 5
						&& !UsedKeys.Contains("psych.time.night"))
					{
						key = "psych.time.night";
					}
					else if (DateTime.Now.Hour is < 8
						&& !UsedKeys.Contains("psych.time.morning"))
					{
						key = "psych.time.morning";
					}
				}

				// Long session
				if (key is null
					&& Data.TryGetValue(DataKeyDaysWhenLastPlayed, out s)
					&& int.TryParse(s, out i))
				{
					if (Game1.Date.TotalDays - i > WorldDate.DaysPerMonth
						&& !UsedKeys.Contains("psych.session.months"))
					{
						key = "psych.session.months";
					}
					else if (Game1.Date.TotalDays - i > WorldDate.DaysPerMonth / 2
						&& !UsedKeys.Contains("psych.session.weeks"))
					{
						key = "psych.session.weeks";
					}
				}

				// Restart
				if (key is null
					&& Data.TryGetValue(DataKeyDaysWhenLastPlayed, out s)
					&& int.TryParse(s, out i) && i < Game1.Date.TotalDays
					&& !UsedKeys.Contains("psych.session.restart"))
				{
					key = "psych.session.restart";
				}

				// Multiple farms
				if (key is null
					&& Sessions.Count > 1)
				{
					if (Sessions.Count > Sessions.DistinctBy(pair => pair.Value).Count()
						&& !UsedKeys.Contains("psych.session.samename"))
					{
						key = "psych.session.samename";
					}
					else if (!UsedKeys.Contains("psych.session.differentname"))
					{
						key = "psych.session.differentname";
					}
					else if (ModEntry.I18n.Get($"psych.session.{Sessions.Count}").HasValue()
						&& !UsedKeys.Contains($"psych.session.{Sessions.Count}"))
					{
						key = $"psych.session.{Sessions.Count}";
					}
				}
			}

			if (key is not null)
			{
				UsedKeys.Add(key);
				var otherNames = Sessions.Values.Where(s => s != Game1.player.Name).ToList();
				tokens = new
				{
					Name = Game1.player.Name,
					HostName = Game1.MasterPlayer.Name,
					PreviousName = Sessions.GetValueOrDefault(PreviousSessionId),
					OtherName = otherNames?.Count > 0 ? otherNames[Game1.random.Next(otherNames.Count)] : string.Empty,
					FarmName = Game1.player.farmName
				};
				var k = ModEntry.I18n.Get(key);
				if (k.HasValue())
				{
					key = k;
				}
				else
				{
					var matches =  ModEntry.I18n.GetTranslations().Where(t => t.Key.StartsWith(key)).ToList();
					key = matches[Game1.random.Next(matches.Count)];
				}
				dialogue = new Dialogue(speaker: who, translationKey: null, dialogueText: ModEntry.I18n.Get(key, tokens));
				who.setNewDialogue(dialogue);
			}
			return dialogue;
		}
    }
}
