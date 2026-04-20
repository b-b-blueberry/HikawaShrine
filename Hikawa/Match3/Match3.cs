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

    public static void LoadAssets(Match3Data data)
    {
        data.UIData.MenuTexture = Game1.content.Load<Texture2D>(data.UIData.MenuTextureId);
        data.UIData.CursorTexture = Game1.content.Load<Texture2D>(data.UIData.CursorTextureId);
        foreach (TokenData tokenData in data.TokenData.Values)
            if (tokenData.TextureId is not null)
                tokenData.Texture = Game1.content.Load<Texture2D>(tokenData.TextureId);
        foreach (CutsceneData cutsceneData in data.CutsceneData.Values)
            if (cutsceneData.TextureId is not null)
                cutsceneData.Texture = Game1.content.Load<Texture2D>(cutsceneData.TextureId);
    }

    public static Match3Data GetData()
    {
        var data = Game1.content.Load<Match3Data>(AssetManager.Match3DataAssetName);

        Match3.LoadAssets(data);

        return data;
    }
}
