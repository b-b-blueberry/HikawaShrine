using System.IO;

namespace HikawaShrine
{
	class Const
	{
		internal const string ConfigJapan = "sub";
		internal const string ConfigRoman = "dub";

		internal const string AssetsPath = "Assets";
		internal const string MiscSpritesFile = "zhs_misc";
		internal const string ArcadeSpritesFile = "zhs_arcade";

		internal static readonly string MapsPath = Path.Combine(AssetsPath, "Maps");
		internal const string MapExtension = ".tbin";
		internal const string ShrineMapName = "HikawaShrine";

		internal const string ArcadeMinigameName = "Hikawa_LightGun";
		internal const string ArcadeObjectName = "Sailor V Arcade System";
		internal const string TileActionID = "Action";
		internal const string MapID = "ModEntry";
	}
}
