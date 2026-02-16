using System.Linq;
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

            Game1.player.modData[ModEntry.ModData.ContentPrefix + "_InWater"] = null;

			base.cleanupBeforePlayerExit();
		}

		public override void performTenMinuteUpdate(int timeOfDay)
		{
			base.performTenMinuteUpdate(timeOfDay);
		}

        public override void spawnObjects()
        {
            base.spawnObjects();

			foreach(var key in this.map.Properties.Keys.Where(key => key.StartsWith(ModEntry.ModData.ContentPrefix + "_IslandSpawn")))
				if (this.GetMapPropertySplitBySpaces(key) is string[] args)
					if (ArgUtility.TryGetRectangle(args, 0, out Rectangle area, out string error)
						&& ArgUtility.TryGet(args, 4, out string type, out error)
						&& ArgUtility.TryGetInt(args, 5, out int attempts, out error)
                        && ArgUtility.TryGetInt(args, 6, out int max, out error))
						Utils.SpawnObjectsInArea(this, area, args[7..], attempts, max, type);
        }
	}
}
