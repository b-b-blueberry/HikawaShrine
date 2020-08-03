using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.Tools;

using Harmony; // el diavolo

namespace Hikawa.Core
{
	public static class Patches
	{
		public static bool MeleeWeapon_drawInMenu_Prefix(MeleeWeapon __instance, SpriteBatch spriteBatch, Vector2 location,
			float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
		{
			try
			{
				var cooldown = 0;
				var cooldownLimit = 0f;
				var coolDownLevel = 0f;
				var addedScale = 0f;
				switch (__instance.type.Value)
				{
					case 0:
					case 3:
						cooldownLimit = 1500f;
						if (MeleeWeapon.defenseCooldown > 0)
						{
							cooldown = MeleeWeapon.defenseCooldown;
						}
						addedScale = ModEntry.Instance.Helper.Reflection.GetField<float>
							(typeof(MeleeWeapon), "addedSwordScale").GetValue();
						break;

					case 2:
						cooldownLimit = 4000f;
						if (MeleeWeapon.clubCooldown > 0)
						{
							cooldown = MeleeWeapon.clubCooldown;
						}
						addedScale = ModEntry.Instance.Helper.Reflection.GetField<float>
							(typeof(MeleeWeapon), "addedClubScale").GetValue();
						break;

					case 1:
						cooldownLimit = 6000f;
						if (MeleeWeapon.daggerCooldown > 0)
						{
							cooldown = MeleeWeapon.daggerCooldown;
						}
						addedScale = ModEntry.Instance.Helper.Reflection.GetField<float>
							(typeof(MeleeWeapon), "addedDaggerScale").GetValue();
						break;
				}

				coolDownLevel = cooldown / cooldownLimit;
				if (cooldown > 0)
					Log.D($"Cooldown: {cooldown}/{cooldownLimit}, Level: {coolDownLevel}");

				var drawingAsDebris = drawShadow && drawStackNumber == StackDrawType.Hide;
				if (!drawShadow | drawingAsDebris)
				{
					addedScale = 0f;
				}

				spriteBatch.Draw(
					Tool.weaponsTexture,
					location + (__instance.type.Value == 1
						? new Vector2(42f, 21f)
						: new Vector2(32f, 32f)),
					Game1.getSourceRectForStandardTileSheet(
						Tool.weaponsTexture, __instance.IndexOfMenuItemView, 16, 16),
					color * transparency,
					0f,
					new Vector2(8f, 8f),
					4f * (scaleSize + addedScale),
					SpriteEffects.None,
					layerDepth);
				if (coolDownLevel > 0f && drawShadow && !drawingAsDebris)
				{
					spriteBatch.Draw(
						Game1.staminaRect,
						new Rectangle(
							(int) location.X,
							(int) location.Y + (64 - (int) (coolDownLevel * 64f)),
							64,
							(int) (coolDownLevel * 64f)),
						Color.Red * 0.66f);
				}

				return false;
			}
			catch (Exception e)
			{
				Log.E($"Exception in {nameof(MeleeWeapon_drawInMenu_Prefix)}:\n{e}");
				return true;
			}
		}

		public static IEnumerable<CodeInstruction> MeleeWeapon_drawInMenu_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var il = instructions.ToList();
			for (var i = 0; i < il.Count; ++i)
			{
				var instruction = il[i];

				//if (i < il.Count - 1 && il[i + 1].)

				yield return instruction;
			}
		}
	}
}
