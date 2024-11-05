namespace Hikawa
{
	public class ModConsts
	{
		public static string CoreModID => ModEntry.Instance.ModManifest.UniqueID;
		public static string ContentModID => string.Join(".", CoreModID, "CP");
		public static string ArcadeModID => string.Join(".", CoreModID, "Arcade");

		public const string SpaceCoreXmlPrefix = "Mods_Blueberry_Hikawa_";
        public const int GenericID = 87008;
    }
}
