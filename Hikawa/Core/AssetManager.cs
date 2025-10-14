using System.IO;
using StardewModdingAPI.Events;

namespace Hikawa
{
	internal static class AssetManager
    {
        internal static readonly string RootAssetDir = Path.Combine("Mods", "blueberry", "Hikawa");

        internal static readonly string DataAssetName = Path.Combine(RootAssetDir, "Data", "Data");
        internal static readonly string BowsDataAssetName = Path.Combine(RootAssetDir, "Data", "Bows");
        internal static readonly string BugsDataAssetName = Path.Combine(RootAssetDir, "Data", "Bugs");
        internal static readonly string CrowTradeRulesDataAssetName = Path.Combine(RootAssetDir, "Data", "CrowTradeRules");
        internal static readonly string DecorSpawnsDataAssetName = Path.Combine(RootAssetDir, "Data", "DecorSpawns");
        internal static readonly string KiteDataAssetName = Path.Combine(RootAssetDir, "Data", "Kites");
        internal static readonly string ShrineTreesDataAssetName = Path.Combine(RootAssetDir, "Data", "ShrineTrees");
        internal static readonly string ShrubsDataAssetName = Path.Combine(RootAssetDir, "Data", "Shrubs");

		internal static readonly string StringsAssetName = Path.Combine(RootAssetDir, "Strings", "Strings");

        internal static readonly string EventSpritesAssetName = Path.Combine(RootAssetDir, "Locations", "Events");
        internal static readonly string ExtraSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Extras");
        internal static readonly string IndoorsSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Indoors");
        internal static readonly string OutdoorsSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Outdoors");
        internal static readonly string CrowSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Crows");
        internal static readonly string CatSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Cats");
        internal static readonly string LightSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Lights");

        internal static readonly string ItalicsFontAssetName = Path.Combine(RootAssetDir, "Fonts", "Italics");

		internal static readonly Rectangle ExtraSpritesFeathersArea = new Rectangle(x: 176, y: 0, width: 48, height: 16);
        internal static readonly Rectangle ExtraSpritesVolleyballArea = new Rectangle(x: 80, y: 16, width: 17, height: 17);
        internal static readonly Rectangle ExtraSpritesVolleyballPlayerTagArea = new Rectangle(x: 112, y: 16, width: 16, height: 16);
        internal static readonly Rectangle ExtraSpritesVolleyballAimpointArea = new Rectangle(x: 176, y: 16, width: 16, height: 16);
        internal static readonly Rectangle ExtraSpritesVolleyballVersusArea = new Rectangle(x: 288, y: 48, width: 32, height: 32);

		internal static void TryEdit(object sender, AssetRequestedEventArgs e)
        {
            /*
            if (e.NameWithoutLocale.IsEquivalentTo(""))
            {
                e.Edit(apply: (asset) =>
                {
                });
            }
            */
        }
    }
}
