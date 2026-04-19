namespace Hikawa.Match3;

public static class Match3
{
    public static Match3SDVMenu StartGame(string stageId, string storyId)
    {
        Match3Data data = Match3.GetData();
        Match3Game game = new(data: data, random: Game1.random, stage: stageId);
        Match3UI ui = new(game: game, storyId: storyId);
        Match3SDVMenu menu = new(ui: ui);
        ModEntry.State.Value.Match3 = game;
        return menu;
    }

    public static Match3Data GetData()
    {
        return Game1.content.Load<Match3Data>(AssetManager.Match3DataAssetName);
    }
}
