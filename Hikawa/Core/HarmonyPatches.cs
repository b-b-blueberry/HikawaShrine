using HarmonyLib; // el diavolo nuevo
using Hikawa.Data;
using Hikawa.Modules;
using Hikawa.Objects.Items;
using Hikawa.Objects.Locations;
using Hikawa.Objects.Menus;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Projectiles;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FarmerRenderer))]
        [HarmonyPatch(nameof(FarmerRenderer.draw), [typeof(SpriteBatch), typeof(FarmerSprite.AnimationFrame), typeof(int), typeof(Rectangle), typeof(Vector2), typeof(Vector2), typeof(float), typeof(int), typeof(Color), typeof(float), typeof(float), typeof(Farmer)])]
        public static void FarmerRenderer_Draw_Prefix(SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, ref Rectangle sourceRect, ref Vector2 position, Vector2 origin, float layerDepth, int facingDirection, Color overrideColor, float rotation, float scale, Farmer who)
        {
            if (!FarmerRenderer.isDrawingForUI && who.modData[ModEntry.ModData.ContentPrefix + "_InWater"] is not null && who.yJumpOffset == 0)
            {
				// crop player sprite
                sourceRect.Height -= (int)who.yOffset / Game1.pixelZoom;
                position.Y += Game1.tileSize;

				// water
				var drawPosition = Vector2.Floor(position);
				var addedSize = currentFrame % 2 == 1 ? 2 : 6;
                var size = new Vector2(sourceRect.Width / 2 + addedSize, 1);
                b.Draw(
                    Game1.staminaRect,
                    new Rectangle(
						(int)drawPosition.X + 4 * Game1.pixelZoom - addedSize / 2 * Game1.pixelZoom,
						(int)drawPosition.Y - 33 * Game1.pixelZoom + sourceRect.Height * Game1.pixelZoom + (int)origin.Y - (int)who.yOffset,
                        (int)size.X * Game1.pixelZoom,
						(int)size.Y * Game1.pixelZoom),
                    Game1.staminaRect.Bounds,
                    Color.White * 0.75f,
                    0,
                    Vector2.Zero,
                    SpriteEffects.None,
                    FarmerRenderer.GetLayerDepth(layerDepth, FarmerRenderer.FarmerSpriteLayers.SwimWaterRing));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character))]
        [HarmonyPatch(nameof(Character.GetShadowOffset))]
        public static void Character_GetShadowOffset_Postfix(Character __instance, ref Vector2 __result)
        {
            if (__instance is Farmer player && player.modData[ModEntry.ModData.ContentPrefix + "_InWater"] is not null)
            {
				__result += new Vector2(0, Game1.tileSize / 4 + player.yOffset);
            }
        }

		#endregion

		#region Item behaviours

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Slingshot), nameof(Slingshot.GetRequiredChargeTime))]
		private static bool Slingshot_GetRequiredChargeTime_Prefix(Slingshot __instance, ref float __result)
		{
			if (__instance is Bow bow && Bow.TryGetMaxDrawTime(bow, ref __result))
			{
				__result /= bow.SpeedMultiplier(bow.getLastFarmerToUse());
                return false;
			}

			return true;
		}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FarmerRenderer))]
        [HarmonyPatch(nameof(FarmerRenderer.draw))]
        [HarmonyPatch([typeof(SpriteBatch), typeof(FarmerSprite.AnimationFrame), typeof(int), typeof(Rectangle), typeof(Vector2), typeof(Vector2), typeof(float), typeof(int), typeof(Color), typeof(float), typeof(float), typeof(Farmer)])]
        private static void FarmerRenderer_Draw_Postfix(FarmerRenderer __instance, Texture2D ___baseTexture, SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, int facingDirection, Color overrideColor, float rotation, float scale, Farmer who)
        {
            float scaledPixelZoom = Game1.pixelZoom * scale;
            Bow.TryDrawWhenUsing(
                b: b,
                playerTexture: ___baseTexture,
                player: who,
                position: position,
                direction: facingDirection,
                layerDepth: layerDepth,
                scaledPixelZoom: scaledPixelZoom);
        }

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

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Game1))]
		[HarmonyPatch("drawTool")]
		[HarmonyPatch([typeof(Farmer), typeof(int)])]
		public static bool Game1_DrawTool_Prefix(Farmer f)
		{
			return f.CurrentTool is not ShrubTool;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Furniture))]
		[HarmonyPatch("checkForAction")]
		public static bool Furniture_CheckForAction_Prefix(Furniture __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
		{
			// Open BugCollectionMenu from BugCollection item in world
			if (!justCheckingForActivity && __instance.Location is not null && __instance.ItemId == ModEntry.ModData.ItemBugCollection)
			{
				Game1.activeClickableMenu = new BugCollectionMenu();
				__result = true;
				return false;
			}
			return true;
		}

		#endregion

		#region Buff behaviours

		public static int GetDamageAfterCrackerDefence(int damage, Farmer who)
		{
			return (int)(who.hasBuff(ModEntry.ModData.BuffCrackersDefence)
				? damage * ModEntry.ModData.BuffCrackersDefenceModifier
				: damage);
		}

		/// <summary>
		/// Burnt Fire Crackers buff reduces fire projectile damage on colliding with players.
		/// </summary>
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(DinoMonster.BreathProjectile))]
		[HarmonyPatch(nameof(DinoMonster.BreathProjectile.Update))]
		public static IEnumerable<CodeInstruction> BreathProjectile_Update_Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> il)
		{
			//Game1.currentLocation.characters.Add(new StardewValley.Monsters.DinoMonster(new Vector2(24, 48) * Game1.tileSize));
			//Game1.player.ClearBuffs();

			List<CodeInstruction> ilOut = il.ToList();

			MethodInfo method = AccessTools.Method(
				type: typeof(HarmonyPatches),
				name: nameof(HarmonyPatches.GetDamageAfterCrackerDefence));

			int i = 0;

			// Change damage value based on player buffs

			// Seek to hardcoded damage value and player getter
			i = ilOut.FindIndex(ci => ci.opcode == OpCodes.Ldc_I4_S);

			if (i <= 0)
			{
				Log.E($"Failed to apply harmony patch in {nameof(BreathProjectile_Update_Transpiler)}");
				return il;
			}

			// Replace hardcoded value with method call
			var ilNew = new CodeInstruction[]
			{
				ilOut[i], // damage
				ilOut[i - 1], // player
				new (OpCodes.Call, method)
			};
			ilOut.RemoveAt(i);
			ilOut.InsertRange(i, ilNew);

			return ilOut;
		}

		/// <summary>
		/// Burnt Fire Crackers buff reduces fire damage from explosions.
		/// </summary>
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(GameLocation))]
		[HarmonyPatch("performDamagePlayers")]
		public static IEnumerable<CodeInstruction> GameLocation_performDamagePlayers_Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> il)
		{
			List<CodeInstruction> ilOut = il.ToList();

			MethodInfo method = AccessTools.Method(
				type: typeof(HarmonyPatches),
				name: nameof(HarmonyPatches.GetDamageAfterCrackerDefence));

			int i = 0;

			// Change damage value based on player buffs

			// Seek to final damage value and player getter
			i = ilOut.FindLastIndex(ci => ci.opcode == OpCodes.Ldloc_0);

			if (i <= 0)
			{
				Log.E($"Failed to apply harmony patch in {nameof(GameLocation_performDamagePlayers_Transpiler)}");
				return il;
			}

			// Replace value with method call
			var ilNew = new CodeInstruction[]
			{
				ilOut[i], // damage
				ilOut[i - 1], // player
				new (OpCodes.Call, method)
			};
			ilOut.RemoveAt(i);
			ilOut.InsertRange(i, ilNew);

			return ilOut;
		}

		/// <summary>
		/// Burnt Fire Crackers buff reduces fire projectile damage on colliding with players.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(BasicProjectile))]
		[HarmonyPatch("behaviorOnCollisionWithPlayer")]
		public static void BasicProjectile_BehaviorOnCollisionWithPlayer_Prefix(ref BasicProjectile __instance, Farmer player)
		{
			// If a projectile has PiercesLeft > 1 this will reduce damage on each successive hit
			// Not that we care that much
			bool isPlayerHit = !__instance.damagesMonsters.Value
				&& player.CanBeDamaged();
			bool isFire = __instance.currentTileSheetIndex.Value is 10; // Fire projectiles only
			bool isCrackerDefence = player.hasBuff(ModEntry.ModData.BuffCrackersDefence); // Burnt Fire Crackers buff
			if (isPlayerHit && isFire && isCrackerDefence)
			{
				__instance.damageToFarmer.Value = HarmonyPatches.GetDamageAfterCrackerDefence(__instance.damageToFarmer.Value, player);
			}
		}

		/// <summary>
		/// Burnt Fire Crackers buff reduces fire monster damage on colliding with players.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Farmer))]
		[HarmonyPatch("takeDamage")]
		public static void Farmer_TakeDamage_Prefix(ref Farmer __instance, ref int damage, Monster damager)
		{
			bool isFire = damager is HotHead or LavaLurk || damager is Bat bat && bat.magmaSprite.Value; // Fire enemies only
			bool isCrackerDefence = __instance.hasBuff(ModEntry.ModData.BuffCrackersDefence); // Burnt Fire Crackers buff
			if (isFire && isCrackerDefence)
			{
				damage = HarmonyPatches.GetDamageAfterCrackerDefence(damage, __instance);
			}
		}

		/// <summary>
		/// Fire Crackers buff increases player fire damage to monsters.
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameLocation))]
		[HarmonyPatch("damageMonster")]
		[HarmonyPatch([typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool)])]
		public static void GameLocation_DamageMonster_Prefix(ref GameLocation __instance, ref int minDamage, ref int maxDamage, bool isBomb, Farmer who)
		{
			bool isFire = isBomb; // Fire attacks only
			bool isCrackerAttack = who is not null && who.hasBuff(ModEntry.ModData.BuffCrackersAttack); // Fire Crackers buff
			if (isFire && isCrackerAttack)
			{
				minDamage = (int)(minDamage * ModEntry.ModData.BuffCrackersAttackModifier);
				maxDamage = (int)(maxDamage * ModEntry.ModData.BuffCrackersAttackModifier);
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

		#region UI behaviours

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DayTimeMoneyBox), nameof(DayTimeMoneyBox.draw))]
		public static void DayTimeMoneyBox_Draw_Postfix(DayTimeMoneyBox __instance, SpriteBatch b)
		{
            if (Game1.currentLocation is Grove)
            {
                b.Draw(ModEntry.Sprites, __instance.position + new Vector2(116f, 68f), new Rectangle(64, 0, 12, 8), Color.White, 0, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.9f);
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
