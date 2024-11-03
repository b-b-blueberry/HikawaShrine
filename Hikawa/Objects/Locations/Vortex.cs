using System;
using Hikawa.Modules;
using StardewModdingAPI.Events;
using StardewValley;

namespace Hikawa.Objects.Locations
{
	public class Vortex : GameLocation
    {
		protected override void resetLocalState()
		{
			base.resetLocalState();

			ModEntry.OverlayEffectControl.Enable(OverlayEffectControl.Effect.Dark, 1f);
		}

		public override void cleanupBeforePlayerExit()
		{
			ModEntry.OverlayEffectControl.Disable();

			base.cleanupBeforePlayerExit();
		}

		public static void TouchVortexWarp(Point fromTile, Point toTile, string toLocation = null)
        {
            ModEntry.State.Value.WarpFrom = fromTile;
			ModEntry.State.Value.WarpTo = toTile;
			ModEntry.State.Value.WarpLocation = toLocation;

            Game1.freezeControls = true;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += Vortex.UpdateVortexWarp;
            Utils.ResetAnimationVars();
            ModEntry.State.Value.AnimationExtraFloat = Game1.player.FacingDirection;
            ModEntry.State.Value.AnimationTarget = Utility.PointToVector2(fromTile);
        }

        private static void UpdateVortexWarp(object sender, UpdateTickedEventArgs e)
        {
            const int halfwayStage = 32; // When AnimationStage reaches this value, set AnimationFlag
            const int animRate = 10; // Overall speed of animation
            const int numOfBeeps = 3;
            const int inVelocity = 1;
            const int outVelocity = 2;

            ModEntry.State.Value.AnimationExtraInt = halfwayStage * 2 * animRate;

            // Spin the player with acceleration and deceleration
            // AnimationStage: Linearly increasing/decreasing rate to turn the player with FacingDirection
            // AnimationFlag: Whether we've passed the middle of the animation
            // AnimationExtraFloat: Stores original facing direction
            // AnimationExtraInt: Stores duration of animation for the dizzy cooldown timer to add onto
            // After Flag is set; fade to black, warp the player, double the counter speed, and run it in reverse

            ModEntry.State.Value.AnimationTimer += animRate;
            if (ModEntry.State.Value.AnimationTimer % Math.Max(halfwayStage * 2 - ModEntry.State.Value.AnimationStage, 1)
                > animRate * (ModEntry.State.Value.AnimationFlag ? outVelocity : inVelocity))
                return;

            // Turn the player around 90° for each stage
            ModEntry.State.Value.AnimationStage += ModEntry.State.Value.AnimationFlag ? -1 : 1;
            Game1.player.FacingDirection = (int)(ModEntry.State.Value.AnimationExtraFloat + ModEntry.State.Value.AnimationStage) % 4;

            // Play sounds as you warp out/in
            if (ModEntry.State.Value.AnimationStage > 0 && ModEntry.State.Value.AnimationStage % (halfwayStage / numOfBeeps) == 0 || ModEntry.State.Value.AnimationStage == 1)
            {
                Game1.playSound(ModConsts.ContentPrefix + "vortex" + Math.Min(4, Math.Max(0, ModEntry.State.Value.AnimationStage / 10)));
            }
            // Warp after spin-in
            if (ModEntry.State.Value.AnimationStage == halfwayStage && !ModEntry.State.Value.AnimationFlag)
                Game1.globalFadeToBlack(VortexWarpActuallyHappens, 0.04f);
            // Exit after spin-out
            if (ModEntry.State.Value.AnimationStage <= 0 && ModEntry.State.Value.AnimationFlag)
                Vortex.EndVortexWarp();
        }

        private static void VortexWarpActuallyHappens()
        {
            if (Game1.currentLocation is not null && Game1.currentLocation.Name != ModEntry.State.Value.WarpLocation

				&& Game1.getLocationFromName(ModEntry.State.Value.WarpLocation) != null)
            {
                Game1.currentLocation = Game1.getLocationFromName(ModEntry.State.Value.WarpLocation);
            }
            Game1.player.Position = new Vector2(ModEntry.State.Value.WarpTo.X * Game1.tileSize, (ModEntry.State.Value.WarpTo.Y + 1) * Game1.tileSize);
            Game1.globalFadeToClear(null, 0.04f);
            ModEntry.State.Value.AnimationFlag = true; // Start running counter in reverse
        }

        private static void EndVortexWarp()
        {
            const int dizzyExtraCooldownTime = 22000;

            Game1.freezeControls = false;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= Vortex.UpdateVortexWarp;
            Utils.ResetAnimationVars();

            ++ModEntry.State.Value.WarpCount;
            Log.W($"Dizzy up to {ModEntry.State.Value.WarpCount}");
            if (ModEntry.State.Value.WarpCount > 2)
            {
                Game1.freezeControls = true;

                Game1.currentLocation.localSound("croak");
                Game1.player.FacingDirection = 2;
                Game1.player.FarmerSprite.animateOnce(224, 400f, 4, who =>
                {
                    --ModEntry.State.Value.WarpCount;
                    Game1.freezeControls = false;
                });
                Game1.player.doEmote(12);
            }
            else
            {
                var cooldownBeforeDizzyClears = ModEntry.State.Value.AnimationExtraInt + dizzyExtraCooldownTime;
                Game1.delayedActions.Add(new DelayedAction(cooldownBeforeDizzyClears, () =>
                {
                    --ModEntry.State.Value.WarpCount;
                    Log.W($"Dizzy down to {ModEntry.State.Value.WarpCount}");
                }));
            }

			ModEntry.State.Value.WarpFrom = Point.Zero;
			ModEntry.State.Value.WarpTo = Point.Zero;
			ModEntry.State.Value.WarpLocation = null;
        }
    }
}
