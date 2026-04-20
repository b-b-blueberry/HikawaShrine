using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hikawa.Match3;

public class Match3StoryMenu : IClickableMenu
{
    public List<ClickableComponent> ClickableComponents; // automatic iclickablemenu navigation impl
    public List<ClickableTextureComponent> StageButtons;

    public readonly Match3Data Data;

    public readonly string StoryId;
    public readonly StoryData StoryData;

    public Rectangle MapBounds;
    public int MapIndex;
    public float MapTimer;

    private ClickableComponent _hoveredButton;
    private Vector2 _hoverPosition;
    private float _hoverAlpha;
    private string _hoverText;

    public int Scale => Game1.pixelZoom;

    public Match3StoryMenu(Match3Data data, string storyId)
        : base()
    {
        this.Data = data;

        this.StoryId = storyId;
        this.StoryData = Match3.GetData().WorldData.Stories[this.StoryId];
        this.StoryData.Texture = Game1.content.Load<Texture2D>(this.StoryData.TextureId);

        ModEntry.SaveData.Match3.StoryStageComplete.TryAdd(storyId, []);

        this.InitComponents();
        this.UpdateComponentLayout();
    }

    public void InitComponents()
    {
        this.initializeUpperRightCloseButton();

        this.StageButtons = [];
        this.ClickableComponents = [];

        foreach ((string stageId, StoryStageData stageData) in this.StoryData.Stages)
            if (stageData.Position != default)
                this.StageButtons.Add(new(stageId, Rectangle.Empty, null, null, this.StoryData.Texture, this.StoryData.StageTextureRegion, Scale, true));

        this.ClickableComponents.AddRange(this.StageButtons);

        this.UpdateButtonsForStoryProgress();
    }

    public void UpdateComponentLayout()
    {
        this.width = 720;
        this.height = 480;

        Vector2 position = Utility.getTopLeftPositionForCenteringOnScreen(this.width, this.height, 0, 0);

        this.xPositionOnScreen = (int)position.X;
        this.yPositionOnScreen = (int)position.Y;

        Rectangle bounds = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);

        // Map
        {
            Rectangle source = this.StoryData.BackgroundTextureRegion.FirstOrDefault();
            this.MapBounds = new Rectangle(new(bounds.Left + (bounds.Width - source.Width * Scale) / 2, bounds.Top + (bounds.Height - source.Height * Scale) / 2), new (source.Width * Scale, source.Height * Scale));
        }
        // Buttons
        foreach (ClickableTextureComponent c in this.StageButtons)
        {
            string stageId = c.name;
            StoryStageData stageData = this.StoryData.Stages[stageId];
            Point origin = new(c.sourceRect.Width / 2, c.sourceRect.Height / 2);
            c.bounds = new(this.MapBounds.Left + (stageData.Position.X - origin.X) * Scale, this.MapBounds.Top + (stageData.Position.Y - origin.Y) * Scale, c.sourceRect.Width * Scale, c.sourceRect.Height * Scale);
        }

        this.upperRightCloseButton.bounds.Location = new(this.MapBounds.Right, this.MapBounds.Top);
    }

    public void UpdateButtonsForStoryProgress()
    {
        foreach (ClickableTextureComponent c in this.StageButtons)
        {
            string stageId = c.name;
            StoryStageData stageData = this.StoryData.Stages[stageId];
            bool isUnlocked = stageData.UnlockedBy?.All(other => this.IsStageComplete(other, allowNull: true)) ?? true;
            bool isComplete = this.IsStageComplete(stageId, allowNull: false);
            c.visible = isUnlocked || isComplete;
            c.sourceRect = isComplete ? this.StoryData.StageCompleteTextureRegion : this.StoryData.StageTextureRegion;
        }
    }

    public bool IsStageComplete(string stageId, bool allowNull)
    {
        // Stages which continue into other stages (ie. StoryStageData.NextStage) require ALL stages to be completed
        return (allowNull && stageId is null)
            || (ModEntry.SaveData.Match3.StoryStageComplete[StoryId].Contains(stageId)
                && (!this.StoryData.Stages.TryGetValue(stageId, out StoryStageData stageData)
                    || this.IsStageComplete(stageData.NextStage, allowNull: true)));
    }

    public void StartGame(string stageId)
    {
        Match3SDVMenu menu = Match3.StartGame(data: this.Data, stageId: stageId, storyId: this.StoryId);
        menu.UI.Game.Stage.OnStateChanged += this.OnStageStateChanged;
        this.SetChildMenu(menu);
    }

    public void OnStageStateChanged(Stage stage, StageState current, StageState next)
    {
        if (next is StageState.End)
        {
            this.ResetHover();

            if (stage.IsWon)
            {
                ModEntry.SaveData.Match3.StoryStageComplete[StoryId].Add(stage.Id);

                this.UpdateButtonsForStoryProgress();
            }
        }
    }

    public void ResetHover()
    {
        this._hoveredButton = default;
        this._hoverPosition = default;
        this._hoverAlpha = default;
        this._hoverText = default;
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

        foreach (ClickableTextureComponent c in this.StageButtons)
        {
            if (c.visible && c.containsPoint(x: x, y: y))
            {
                this.StartGame(c.name);
            }
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);

        const float scaleTo = 0.5f;

        foreach (ClickableTextureComponent c in this.StageButtons)
        {
            c.tryHover(x: x, y: y, maxScaleIncrease: scaleTo);
        }

        this._hoveredButton = this.ClickableComponents.FirstOrDefault(c => c.visible && c.containsPoint(x, y));

        if (this._hoveredButton is not null)
        {
            this._hoverText = this._hoveredButton.name;
            this._hoverPosition = this._hoveredButton.bounds.Center.ToVector2();
        }
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

        var ms = (float)time.ElapsedGameTime.TotalMilliseconds;

        this.MapTimer += ms;
        if (this.MapTimer > this.StoryData.BackgroundFrameTime)
        {
            this.MapTimer = 0;
            ++this.MapIndex;
            this.MapIndex %= this.StoryData.BackgroundTextureRegion.Count;
        }

        this._hoverAlpha = Math.Clamp(this._hoverAlpha + ms / 250f * (this._hoveredButton is null ? -1 : 1), 0, 1);

        Match3.PlayMusic(id: this.StoryData.Music);
    }

    public override bool shouldDrawCloseButton()
    {
        return this._childMenu is null;
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);

        if (this._childMenu is not null)
            return;

        Rectangle bounds = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);

        // Screen overlay
        b.Draw(
            texture: Game1.fadeToBlackRect,
            destinationRectangle: Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea,
            color: new Color(10, 3, 5, 232));

        // World background
        {
            Rectangle source = this.StoryData.BackgroundTextureRegion[this.MapIndex];
            Vector2 origin = source.Size.ToVector2() / 2;
            Vector2 position = bounds.Center.ToVector2();
            b.Draw(
                texture: this.StoryData.Texture,
                position: position,
                sourceRectangle: source,
                color: Color.White,
                rotation: 0,
                origin: origin,
                scale: Scale,
                effects: SpriteEffects.None,
                layerDepth: 1);
        }

        // Buttons
        {
            if (this.shouldDrawCloseButton())
                this.upperRightCloseButton.draw(b: b);

            List<ClickableTextureComponent> buttons = this.StageButtons;

            foreach (ClickableTextureComponent c in buttons)
                if (c.visible)
                    c.draw(b: b);

            // text bubble
            if (this._hoverAlpha > 0)
            {
                Vector2 position = this._hoverPosition;
                float yOffset = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) / 2 * Scale;
                float alpha = this._hoverAlpha;
                position += new Vector2(0, -24 + yOffset) * Scale;
                Rectangle source = new(141, 465, 20, 24);

                // Bubble
                b.Draw(
                    texture: Game1.mouseCursors,
                    position: position - new Vector2(x: 0, y: -1) * Scale,
                    sourceRectangle: source,
                    color: Color.White * alpha,
                    rotation: 0,
                    origin: source.Size.ToVector2() / 2,
                    scale: Scale * alpha,
                    effects: SpriteEffects.None,
                    layerDepth: 1);

                string text = this._hoverText;
                SpriteFont font = Game1.dialogueFont;
                Vector2 textSize = font.MeasureString(text);
                b.DrawString(font, text, position, Color.Black, 0, textSize / 2f, Scale / 4f * alpha, SpriteEffects.None, 1);

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
                        texture: this.StoryData.Texture,
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
        }

        this.drawMouse(b, ignore_transparency: true, cursor: Game1.cursor_gamepad_pointer);
    }
}
