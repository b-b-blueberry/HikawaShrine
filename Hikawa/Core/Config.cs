using StardewModdingAPI;

namespace Hikawa
{
    internal class Config
    {
		public bool JapaneseNames { get; set; } = true;
		public bool AnimePortraits { get; set; } = false;
		public bool SeasonalClothes { get; set; } = true;

		public bool DebugMode { get; set; } = true;
		public bool DebugArcadeCheats { get; set; } = true;
		public bool DebugArcadeSkipIntro { get; set; } = false;
		public bool DebugArcadeMusic { get; set; } = true;
		public SButton DebugPlayArcade { get; set; } = SButton.OemCloseBrackets;
		public SButton DebugWarpShrine { get; set; } = SButton.OemOpenBrackets;
    }
}
