using StardewModdingAPI;

namespace HikawaShrine
{
    class Config
    {
		public string Names { get; set; } = "sub";
		public string Portraits { get; set; } = "dub";

		public bool DebugArcadeCheats { get; set; } = false;
		public bool DebugArcadeSkipIntro { get; set; } = false;
		public SButton DebugArcadePlayGame { get; set; } = SButton.P;
    }
}
