using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Match3;

public class Match3MainMenu : IClickableMenu
{
    public List<ClickableComponent> ClickableComponents; // automatic iclickablemenu navigation impl
    public List<ClickableTextureComponent> MenuButtons;
    public ClickableTextureComponent TutorialButton;
    public ClickableTextureComponent StoryButton;
    public ClickableTextureComponent EndlessButton;
    public ClickableTextureComponent StatsButton;

    public readonly Match3Data Data;

    public readonly Texture2D Sprites;

    public readonly string InitialStage;

    private ClickableComponent _hoveredButton;

    public int Scale => Game1.pixelZoom;

    public Match3MainMenu(string stage)
        : base()
    {
        this.InitialStage = stage;

        this.Data = Match3.GetData();

        this.Sprites = Game1.content.Load<Texture2D>(AssetManager.Match3SpritesAssetName);

        this.InitComponents();
        this.UpdateComponentLayout();
    }

    public void InitComponents()
    {
        this.initializeUpperRightCloseButton();

        this.ClickableComponents = [];
        this.MenuButtons = [];

        Rectangle source = new(128, 0, 32, 32);
        this.TutorialButton = new("tutorial", Rectangle.Empty, null, "Basics", this.Sprites, source, Scale, true);
        this.StoryButton = new("story", Rectangle.Empty, null, "Story", this.Sprites, source, Scale, true);
        this.EndlessButton = new("endless", Rectangle.Empty, null, "Zen", this.Sprites, source, Scale, true);
        this.StatsButton = new("stats", Rectangle.Empty, null, "Stats", this.Sprites, source, Scale, true);

        this.MenuButtons.AddRange([this.TutorialButton, this.StoryButton, this.EndlessButton, this.StatsButton]);
        this.ClickableComponents.AddRange(this.MenuButtons);
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
        this.TutorialButton.bounds = new(bounds.Left - origin.X * Scale, bounds.Center.Y - origin.Y * Scale, source.Width * Scale, source.Height * Scale);
        this.StoryButton.bounds = new(bounds.Center.X - origin.X * Scale, bounds.Center.Y - origin.Y * Scale, source.Width * Scale, source.Height * Scale);
        this.EndlessButton.bounds = new(bounds.Right - origin.X * Scale, bounds.Center.Y - origin.Y * Scale, source.Width * Scale, source.Height * Scale);
        this.StatsButton.bounds = new(bounds.Width / 4 + bounds.Left - origin.X * Scale, bounds.Height / 4 + bounds.Center.Y - origin.Y * Scale, source.Width * Scale, source.Height * Scale);

        this.upperRightCloseButton.bounds.Location = new(bounds.Right, bounds.Top);
    }

    public void StartGame(string stageId, string storyId)
    {
        var menu = Match3.StartGame(data: this.Data, stageId: stageId, storyId: storyId);
        this.SetChildMenu(menu);
    }

    public void OpenStory(string storyId)
    {
        this.SetChildMenu(new Match3StoryMenu(this.Data, storyId));
    }

    public void OpenStats()
    {
        this.SetChildMenu(new Match3StatsMenu());
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);

        this.UpdateComponentLayout();

        this._childMenu?.gameWindowSizeChanged(oldBounds, newBounds);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (this.TutorialButton.visible && this.TutorialButton.containsPoint(x: x, y: y))
        {
            this.StartGame("T-1", "Tutorial");
        }
        else if (this.StoryButton.visible && this.StoryButton.containsPoint(x: x, y: y))
        {
            this.OpenStory("Main");
        }
        else if (this.EndlessButton.visible && this.EndlessButton.containsPoint(x: x, y: y))
        {
            this.StartGame("Endless", "Endless");
        }
        else if (this.StatsButton.visible && this.StatsButton.containsPoint(x: x, y: y))
        {
            this.OpenStats();
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);

        const float scaleTo = 0.5f;

        foreach (ClickableTextureComponent c in this.MenuButtons)
        {
            c.tryHover(x: x, y: y, maxScaleIncrease: scaleTo);
        }

        this._hoveredButton = this.ClickableComponents.FirstOrDefault(c => c.visible && c.containsPoint(x, y));
    }

    protected override void cleanupBeforeExit()
    {
        base.cleanupBeforeExit();

        Match3.StopMusic();
    }

    public override void update(GameTime time)
    {
        base.update(time);

        if (this._childMenu is not null)
            return;

        Match3.PlayMusic(this.Data.AudioData.MainMenuMusic);
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

        List<ClickableTextureComponent> buttons = this.MenuButtons;
        string text;
        Vector2 textSize;
        SpriteFont font = Game1.dialogueFont;
        foreach (ClickableTextureComponent c in buttons)
        {
            if (c.visible)
            {
                c.draw(b: b);
                text = c.hoverText;
                textSize = font.MeasureString(text);
                b.DrawString(font, text, c.bounds.Center.ToVector2() + new Vector2(0, 2) * Scale, Color.Black, 0, textSize / 2, 0.75f + (c.scale - Scale) / 2f, SpriteEffects.None, 1);
            }
        }

        if (this._hoveredButton is not null)
        {
            ClickableComponent c = this._hoveredButton;

            Vector2 position = c.bounds.Center.ToVector2();

            Rectangle star = new Rectangle(128, 0, 32, 32);
            Vector2 origin = star.Size.ToVector2() / 2;

            // hehe stoloe ur smoke code
            int interval = 1600 + 256 * 6666 % 200;
            Vector2[] offsets = [new(-10, 2), new(1, 12), new(10, 6)];
            for (int i = 0; i < offsets.Length; ++i)
            {
                b.Draw(
                    texture: this.Sprites,
                    position: position
                        + offsets[i] * Scale
                        + new Vector2(0f, (float)((0f - Game1.currentGameTime.TotalGameTime.TotalMilliseconds + interval * i) % 2000f) * 0.03f),
                    sourceRectangle: star,
                    color: Color.White
                        * (c.scale - Scale)
                        * (1f - (float)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds + interval * i) % 2000f) / 2000f),
                    rotation: (float)((0f - Game1.currentGameTime.TotalGameTime.TotalMilliseconds) % 2000f)
                        * 0.001f,
                    origin: origin,
                    scale: (c.scale - Scale),
                    effects: SpriteEffects.None,
                    layerDepth: 1);
            }
        }

        this.drawMouse(b);
    }
}
