using StardewValley.Menus;
using System.Collections.Generic;

namespace Hikawa.Match3;

public class Match3MainMenu : IClickableMenu
{
    public readonly List<ClickableComponent> ClickableComponents; // automatic iclickablemenu navigation impl

    public ClickableTextureComponent TutorialButton;
    public ClickableTextureComponent StoryButton;
    public ClickableTextureComponent EndlessButton;

    public readonly Texture2D Sprites;

    public readonly string InitialStage;

    public int Scale => Game1.pixelZoom;

    public Match3MainMenu(string stage)
        : base()
    {
        this.InitialStage = stage;

        this.Sprites = Game1.content.Load<Texture2D>("Mods/blueberry/Hikawa/Match3/Sprites");

        this.ClickableComponents = [];

        this.InitComponents();
        this.UpdateComponentLayout();
    }

    public void InitComponents()
    {
        this.initializeUpperRightCloseButton();

        Rectangle source = new(128, 0, 32, 32);
        this.TutorialButton = new(Rectangle.Empty, this.Sprites, source, Scale, true);
        this.StoryButton = new(Rectangle.Empty, this.Sprites, source, Scale, true);
        this.EndlessButton = new(Rectangle.Empty, this.Sprites, source, Scale, true);

        this.ClickableComponents.AddRange([this.TutorialButton, this.StoryButton, this.EndlessButton]);
    }

    public void UpdateComponentLayout()
    {
        this.width = 720;
        this.height = 480;

        Vector2 position = Utility.getTopLeftPositionForCenteringOnScreen(this.width, this.height, 0, 0);

        this.xPositionOnScreen = (int)position.X;
        this.yPositionOnScreen = (int)position.Y;

        Rectangle bounds = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);

        Rectangle source = new(128, 0, 32, 32);
        Point origin = new(source.Width / 2, source.Height / 2);
        this.TutorialButton.bounds = new(bounds.Left - origin.X, bounds.Center.Y - origin.Y, source.Width * Scale, source.Height * Scale);
        this.StoryButton.bounds = new(bounds.Center.X - origin.X, bounds.Center.Y - origin.Y, source.Width * Scale, source.Height * Scale);
        this.EndlessButton.bounds = new(bounds.Right - origin.X, bounds.Center.Y - origin.Y, source.Width * Scale, source.Height * Scale);

        this.upperRightCloseButton.bounds.Location = new(bounds.Right, bounds.Top);
    }

    public void StartGame(string stage)
    {
        Match3Data data = Game1.content.Load<Match3Data>("Mods/blueberry/Hikawa/Match3/Data");
        Match3Game game = new(data: data, random: Game1.random, stage: stage);
        Match3UI ui = new(game: game);
        Match3SDVMenu menu = new(ui: ui);
        ModEntry.State.Value.Match3 = game;

        this.SetChildMenu(menu);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);

        this.UpdateComponentLayout();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (this.TutorialButton.containsPoint(x: x, y: y))
        {
            this.StartGame("1");
        }
        else if (this.StoryButton.containsPoint(x: x, y: y))
        {
            this.StartGame("3");
        }
        else if (this.EndlessButton.containsPoint(x: x, y: y))
        {
            this.StartGame("Endless");
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);

        const float scaleTo = 0.5f;

        this.TutorialButton.tryHover(x: x, y: y, maxScaleIncrease: scaleTo);
        this.StoryButton.tryHover(x: x, y: y, maxScaleIncrease: scaleTo);
        this.EndlessButton.tryHover(x: x, y: y, maxScaleIncrease: scaleTo);
    }

    public override void update(GameTime time)
    {
        base.update(time);

        if (this._childMenu is not null)
            return;
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);

        if (this._childMenu is not null)
            return;

        b.Draw(
            texture: Game1.fadeToBlackRect,
            destinationRectangle: Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea,
            color: new Color(10, 3, 5, 232));

        if (this.shouldDrawCloseButton())
            this.upperRightCloseButton.draw(b: b);

        this.TutorialButton.draw(b: b);
        this.StoryButton.draw(b: b);
        this.EndlessButton.draw(b: b);

        this.drawMouse(b);
    }
}
