using System;
using StardewValley;
using HarmonyLib; // el diavolo nuevo

namespace Hikawa
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		#region NPC behaviours

		/// <summary>
		/// Add end-of-route behaviours for custom characters.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(NPC))]
		[HarmonyPatch("finishRouteBehavior")]
		public static bool NPC_finishRouteBehavior_Prefix(NPC __instance, string behaviorName)
		{
			try
			{
				if (behaviorName.StartsWith(ModConsts.ContentPrefix))
				{
					// Perform custom end-of-route behaviours
					// . . .
				}
			}
			catch (Exception e)
			{
				Log.E($"Exception in {nameof(NPC_finishRouteBehavior_Prefix)}:{Environment.NewLine}{e}");
			}
			return true;
		}

		/// <summary>
		/// Psych behaviours.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(NPC))]
		[HarmonyPatch(nameof(NPC.CurrentDialogue))]
		[HarmonyPatch(MethodType.Getter)]
		public static bool NPC_CurrentDialogue_Get_Postfix(NPC __instance)
		{
			Dialogue dialogue = Modules.DialoguePicker.GetDailyUnique(who: __instance);
			return dialogue is null;
		}

		#endregion

		#region Volleyball behaviours

		/// <summary>
		/// Volleyball game behaviours.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Game1))]
		[HarmonyPatch(nameof(Game1.shouldTimePass))]
		public static void Game1_ShouldTimePass_Postfix(ref bool __result)
		{
			__result &= Game1.currentLocation is not Volleyball.VolleyballLocation;
		}

		/// <summary>
		/// Volleyball game behaviours.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(StardewValley.Object))]
		[HarmonyPatch(nameof(StardewValley.Object.IsHeldOverHead))]
		public static void Object_IsHeldOverHead_Postfix(StardewValley.Object __instance, ref bool __result)
		{
			__result &= Game1.currentLocation is not Volleyball.VolleyballLocation;
		}

		/// <summary>
		/// Volleyball game behaviours.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Farmer))]
		[HarmonyPatch(nameof(Farmer.showSwordSwipe))]
		public static bool Farmer_ShowSwordSwipe_Prefix()
		{
			return Game1.currentLocation is not Volleyball.VolleyballLocation;
		}

		#endregion
	}
}
