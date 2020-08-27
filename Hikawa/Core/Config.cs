using StardewModdingAPI;

namespace Hikawa
{
    internal class Config
    {
		public bool JapaneseNames { get; set; } = false;
		public bool AnimePortraits { get; set; } = false;
		public bool SeasonalOutfits { get; set; } = false;

		public bool DebugMode { get; set; } = true;
		public bool DebugArcadeCheats { get; set; } = true;
		public bool DebugArcadeSkipIntro { get; set; } = false;
		public bool DebugArcadeMusic { get; set; } = true;
		public bool DebugShowRainInTheNight { get; set; } = false;
    }
}
