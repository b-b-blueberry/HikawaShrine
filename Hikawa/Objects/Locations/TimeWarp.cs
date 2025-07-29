using Hikawa.Modules;

namespace Hikawa.Objects.Locations
{
	public class TimeWarp : GameLocation
	{
		protected override void resetLocalState()
		{
			base.resetLocalState();

			ModEntry.OverlayEffectControl.Enable(OverlayEffectControl.Effect.Haze);
		}

		public override void cleanupBeforePlayerExit()
		{
			ModEntry.OverlayEffectControl.Disable();

			base.cleanupBeforePlayerExit();
		}
	}
}
