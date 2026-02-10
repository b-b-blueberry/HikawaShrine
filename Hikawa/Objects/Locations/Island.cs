using System.Xml.Serialization;

namespace Hikawa.Objects.Locations
{
	[XmlType($"{ModConsts.SpaceCoreXmlPrefix}{nameof(Island)}")] // SpaceCore serialisation signature
	public class Island : GameLocation
	{
		public Island() : base() {}

		public Island(string filename, string locationName) : base(filename, locationName) {}

		public static GameLocation Get()
		{
			return Game1.getLocationFromName(ModEntry.ModData.MapIsland);
		}

		protected override void resetLocalState()
		{
			base.resetLocalState();
		}

		protected override void resetSharedState()
		{
			base.resetSharedState();
		}

		public override void cleanupBeforePlayerExit()
		{
			Utils.ResetCustomSharedMapProperties(this);

			base.cleanupBeforePlayerExit();
		}

		public override void performTenMinuteUpdate(int timeOfDay)
		{
			base.performTenMinuteUpdate(timeOfDay);
		}
	}
}
