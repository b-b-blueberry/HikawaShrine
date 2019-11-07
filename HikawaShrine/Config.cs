using StardewModdingAPI;

namespace HikawaShrine
{
    class Config
    {
		public string Names { get; set; } = "sub";
		public string Portraits { get; set; } = "dub";

		public bool Debug_ArcadeCheats { get; set; } = false;
		public bool Debug_ArcadeSkipIntro { get; set; } = false;
		public SButton Debug_ArcadePlayGame { get; set; } = SButton.P;
    }
}
