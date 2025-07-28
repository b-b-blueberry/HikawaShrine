using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib; // el diavolo nuevo
using Hikawa.Modules;
using StardewValley;
using Hikawa.Objects.Items;
using Hikawa.Objects.Menus;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace Hikawa
{
	[HarmonyPatch]
	public static class HarmonyPatches
	{
		#region Evil

		[HarmonyPrefix]
		[HarmonyPatch(typeof(DialogueBox))]
		[HarmonyPatch("drawPortrait")]
		public static void DialogueBox_DrawPortrait_Prefix()
		{
			DialogueEffects.State.Value.IsDialogue = false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DialogueBox))]
		[HarmonyPatch("drawPortrait")]
		public static void DialogueBox_DrawPortrait_Postfix()
		{
			DialogueEffects.State.Value.IsDialogue = true;
		}

		/// <summary>
		/// Add custom text effects.
		/// </summary>
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(SpriteText))]
		[HarmonyPatch("drawString")]
		private static IEnumerable<CodeInstruction> SpriteText_DrawString_Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> il)
		{
			List<CodeInstruction> ilOut = il.ToList();

			MethodInfo charMethod = AccessTools.Method(
				type: typeof(SpriteText),
				name: "getSourceRectForChar");
			MethodInfo charOffsetMethod = AccessTools.Method(
				type: typeof(DialogueEffects),
				name: nameof(DialogueEffects.ApplyCharOffset));
			MethodInfo stringOffsetMethod = AccessTools.Method(
				type: typeof(DialogueEffects),
				name: nameof(DialogueEffects.ApplyStringOffset));

			int i = 0, j = 0;

			/* Find for-loop on string after ♡ replace call:
			 * IL_099e: ldc.i4 9825
			 * ...
			 * IL_0b1e: IL_09ac: ldc.i4.0
			 * ...
			 * IL_09af: br IL_0e9b
			 */

			i = ilOut.FindIndex(ci => ci.opcode == OpCodes.Ldc_I4 && ci.OperandIs(9825));
			j = i < 0 ? 0 : ilOut.FindIndex(i, ci => ci.opcode == OpCodes.Ldc_I4_0);

			if (j <= 0)
			{
				Log.E($"Failed to apply harmony patch in {nameof(SpriteText_DrawString_Transpiler)}");
				return il;
			}

			ilOut.InsertRange(j - 1, [
				// public static void DialogueEffects.ApplyStringOffset(ref Vector2 v, string text);
				new CodeInstruction(OpCodes.Ldloca_S, 2), // Vector2 position
				new CodeInstruction(OpCodes.Ldarg_S, 1), // string text
				new CodeInstruction(OpCodes.Call, stringOffsetMethod), // ApplyStringOffset
			]);

			/* Find character draw call:
			 * IL_0b17: call valuetype [MonoGame.Framework]Microsoft.Xna.Framework.Rectangle StardewValley.BellsAndWhistles.SpriteText::getSourceRectForChar
			 * ...
			 * IL_0b1e: ldarg.0
			 * ...
			 * IL_0b80: callvirt instance void [MonoGame.Framework]Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw
			 */

			i = ilOut.FindIndex(ci => ci.opcode == OpCodes.Call && ci.OperandIs(charMethod));
			j = i < 0 ? 0 : ilOut.FindIndex(i, ci => ci.opcode == OpCodes.Ldc_I4_0);
			
			if (j <= 0)
			{
				Log.E($"Failed to apply harmony patch in {nameof(SpriteText_DrawString_Transpiler)}");
				return il;
			}

			ilOut.InsertRange(i - 1, [
				// public static void DialogueEffects.ApplyCharOffset(ref Vector2 v, int i, int scroll);
				new CodeInstruction(OpCodes.Ldloca_S, 2), // Vector2 position
				new CodeInstruction(OpCodes.Ldloc_S, 12), // int i
				new CodeInstruction(OpCodes.Call, charOffsetMethod), // ApplyCharOffset
			]);

			return ilOut;
		}

        #endregion

        #region Item behaviours
		
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Object))]
        [HarmonyPatch(nameof(Object.drawWhenHeld))]
        private static void Object_DrawWhenHeld_Postfix(Object __instance, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
        {
            if (!__instance.isTemporarilyInvisible && __instance.HasContextTag("particle_smoke"))
            {
                Utils.DrawSmokeParticles(spriteBatch, objectPosition, 1f, f.getDrawLayer() + 1E-05f);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Object))]
        [HarmonyPatch(nameof(Object.drawInMenu))]
        [HarmonyPatch([typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool)])]
        private static void Object_DrawInMenu_Postfix(Object __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth)
        {
            if (!__instance.isTemporarilyInvisible && __instance.HasContextTag("particle_smoke"))
            {
                Utils.DrawSmokeParticles(spriteBatch, location, scaleSize, layerDepth, transparency);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Object))]
        [HarmonyPatch(nameof(Object.draw))]
        [HarmonyPatch([typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)])]
        private static void Object_Draw_Postfix(Object __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
        {
            if (!__instance.isTemporarilyInvisible && __instance.HasContextTag("particle_smoke"))
            {
				Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y) * Game1.tileSize);
                Rectangle bounds = __instance.GetBoundingBoxAt(x, y);
                float scale = (__instance.scale.Y > 1f) ? __instance.getScale().Y : 1f;
                float layerDepth = (__instance.isPassable() ? bounds.Top : bounds.Center.Y) / 10000f;
                Utils.DrawSmokeParticles(spriteBatch, position, scale, layerDepth, alpha);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Object))]
        [HarmonyPatch(nameof(Object.draw))]
        [HarmonyPatch([typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(float)])]
        private static void Object_Draw_1_Postfix(Object __instance, SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha)
        {
            if (!__instance.isTemporarilyInvisible && __instance.HasContextTag("particle_smoke"))
            {
                Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile, yNonTile));
                float scale = (__instance.scale.Y > 1f) ? __instance.getScale().Y : Game1.pixelZoom;
                Utils.DrawSmokeParticles(spriteBatch, position, scale, layerDepth, alpha);
            }
        }

		#endregion

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
				if (behaviorName.StartsWith(ModEntry.ModData.ContentPrefix))
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
		[HarmonyPatch("loadCurrentDialogue")]
		public static void NPC_LoadCurrentDialogue_Postfix(NPC __instance, ref Stack<Dialogue> __result)
		{
			if (Modules.DialoguePicker.GetDailyUnique(npc: __instance, who: Game1.player) is Dialogue dialogue)
			{
				__result.Clear();
				__result.Push(dialogue);
			}
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
