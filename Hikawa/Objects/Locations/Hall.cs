using System.Xml.Serialization;

namespace Hikawa.Objects.Locations
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(Hall)}")] // SpaceCore serialisation signature
	public class Hall : GameLocation
	{
		public Hall() : base() {}

		public Hall(string filename, string locationName) : base(filename, locationName) {}

		public static GameLocation Get()
		{
			return Game1.getLocationFromName(ModEntry.ModData.MapHall);
		}

		protected override void resetLocalState()
		{
			base.resetLocalState();

			Game1.player.setRunning(isRunning: false);
			Game1.player.canOnlyWalk = true;
		}

		protected override void resetSharedState()
		{
			base.resetSharedState();
		}

		public override void cleanupBeforePlayerExit()
		{
			Utils.ResetCustomSharedMapProperties(this);
			Game1.player.canOnlyWalk = false;

			base.cleanupBeforePlayerExit();
		}

		public override void performTenMinuteUpdate(int timeOfDay)
		{
			base.performTenMinuteUpdate(timeOfDay);
		}
	}
}
