
using StardewModdingAPI;
using StardewValley;
using System.IO;

namespace HikawaArcade
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static Config Config;

        // Arcade
        internal const string ModName = "blueberry.Hikawa.Arcade";
        internal const string TilesheetPrefix = "z_hikawa";
        internal const string ContentPrefix = ModName + ".";
        internal const string CommandPrefix = "bb";

        internal const string ArcadeMinigameId = ContentPrefix + "ArcadeGunGame";
        internal const string ArcadeObjectName = ContentPrefix + "ArcadeGunGame";

		internal static readonly string RootAssetDir = Path.Combine("Mods", "blueberry", "Hikawa");
		internal static readonly string ArcadeSpritesAssetName = Path.Combine(RootAssetDir, "Sprites", "Arcades");

		public override void Entry(IModHelper helper)
        {
            ModEntry.Instance = this;
            ModEntry.Config = helper.ReadConfig<Config>();

			helper.ConsoleCommands.Add(
                name: CommandPrefix + "arcade",
                documentation: "Start the arcade.",
                callback: (s, p) =>
                {
                    if (p.Length < 1 || p[0].ToLower() == "start")
                    {
                        Arcade.ArcadeGame.Start();
                        return;
                    }

                    string action = p[0].ToLower();
                    if (Game1.currentMinigame is Arcade.ArcadeGame game && game.minigameId() == ModEntry.ArcadeMinigameId)
                    {
                        if (action == "reset")
                        {
                            ((Arcade.ArcadeGame)Game1.currentMinigame).ResetGame();
                            return;
                        }
                    }
                    Log.E("Not a valid arcade action.");
                });
        }
    }
}