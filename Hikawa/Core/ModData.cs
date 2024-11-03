using System.Collections.Generic;
using Hikawa.Objects.Locations;

namespace Hikawa
{
	public record class ModData
	{
		public Vector2 HouseChimneyTile;
		public Point CrowSpawnRadius;
		public Rectangle CrowSpawnArea;
		/// <summary>
		/// Keyed by chance to appear, chance is measured by whether key is higher than the random roll
		/// </summary>
		public Dictionary<float, CrowSpawnEntry> CrowPerches;
		public Point[] BabyCrowPerches;
		public Point[] BabyCrowRoosts;
		public Dictionary<string, ShrineTreeDefinitionsEntry> ShrineTreeDefinitions;
		public Dictionary<string, List<ShrineTreesEntry>> ShrineTrees;
		public Dictionary<string, List<HangingSpriteEntry>> HangingSprites;
		public Dictionary<string, List<LightTileEntry>> LightTiles;
		public Dictionary<string, List<LightEntry>> Lights;
	}
}
