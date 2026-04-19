using StardewValley.GameData;

namespace Hikawa.Match3;

public static class Match3
{
    public static Match3SDVMenu StartGame(Match3Data data, string stageId, string storyId)
    {
        Match3Game game = new(data: data, random: Game1.random, stage: stageId);
        Match3UI ui = new(game: game, storyId: storyId);
        Match3SDVMenu menu = new(ui: ui);
        return menu;
    }

    public static void PlaySound(string id)
    {
        //if (!.IsMute)
        {
            Game1.playSound(id);
        }
    }

    public static void PlayMusic(string id)
    {
        if (id is not null && Game1.getMusicTrackName(MusicContext.MiniGame) != id)
        {
            Game1.changeMusicTrack(
                newTrackName: id,
                track_interruptable: false,
                music_context: MusicContext.MiniGame);
        }
    }

    public static void StopMusic()
    {
        Game1.stopMusicTrack(MusicContext.MiniGame);
    }

    public static Match3Data GetData()
    {
        return Game1.content.Load<Match3Data>(AssetManager.Match3DataAssetName);
    }
}
