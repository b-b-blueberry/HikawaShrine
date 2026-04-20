using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace Hikawa.Match3
{
    public class Match3CutsceneMenu : IClickableMenu
    {
        public readonly Match3Data Data;
        public readonly CutsceneData CutsceneData;
        public readonly List<CutsceneItem> CutsceneItems;

        public UIData MenuData => this.Data.UIData;

        public int CutsceneIndex;
        public double CutsceneTimer;
        public double CutsceneInputTimer;

        public int Scale => Game1.pixelZoom;

        public class CutsceneItem
        {
            public CutsceneItemData Data;

            public CutsceneItem(CutsceneItemData data)
            {
                this.Data = data;
            }
        }

        public Match3CutsceneMenu(Match3Data data, string cutsceneId, int index = 0)
            : base()
        {
            this.Data = data;

            this.CutsceneData = data.CutsceneData[cutsceneId];

            Match3.PlayMusic(this.CutsceneData.Music);

            this.CutsceneItems = [];
            this.AddCutsceneItems(index);

            this.InitComponents();
            this.UpdateComponentLayout();
        }

        public void InitComponents()
        {
            this.initializeUpperRightCloseButton();
        }

        public void UpdateComponentLayout()
        {
            this.width = 720;
            this.height = 480;

            Vector2 position = Utility.getTopLeftPositionForCenteringOnScreen(this.width, this.height, 0, 0);

            this.xPositionOnScreen = (int)position.X;
            this.yPositionOnScreen = (int)position.Y;

            Rectangle bounds = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);

            this.upperRightCloseButton.bounds.Location = new(bounds.Right, bounds.Top);
        }

        public void AddCutsceneItems(int index)
        {
            foreach (var data in this.CutsceneData.Items[index])
            {
                this.CutsceneItems.Add(new CutsceneItem(data));
            }
        }

        public void ContinueCutscene()
        {
            if (++this.CutsceneIndex >= this.CutsceneData.Items.Count)
            {
                this.exitThisMenuNoSound();
            }
            else
            {
                this.AddCutsceneItems(this.CutsceneIndex);
            }
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

            if (this.CutsceneInputTimer > this.MenuData.DialogueIgnoreInputTime)
            {
                this.CutsceneInputTimer = 0;

                this.ContinueCutscene();
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);

            var ms = time.ElapsedGameTime.TotalMilliseconds;

            this.CutsceneTimer += ms;
            this.CutsceneInputTimer += ms;
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);

            if (this._childMenu is not null)
                return;

            Rectangle bounds = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
            Vector2 position = bounds.Center.ToVector2();
            float scale = this.MenuData.Scale;

            // Screen overlay
            b.Draw(
                texture: Game1.fadeToBlackRect,
                destinationRectangle: Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea,
                color: new Color(10, 3, 5, 232));

            if (this.shouldDrawCloseButton())
                this.upperRightCloseButton.draw(b: b);

            SpriteFont font = Game1.dialogueFont;
            Vector2 textSize;
            string text;

            // Cutscene items (drawn at origin centre)
            foreach (var item in this.CutsceneItems)
            {
                if (!item.Data.TextureRegion.IsEmpty)
                {
                    b.Draw(this.CutsceneData.Texture, position + (item.Data.Position.ToVector2() - item.Data.TextureRegion.Size.ToVector2() / 2) * scale, item.Data.TextureRegion, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1);
                }
                if (item.Data.Text is not null)
                {
                    text = item.Data.Text;
                    textSize = font.MeasureString(text);
                    b.DrawString(font, text, position + item.Data.Position.ToVector2() * scale - textSize / 2, Utility.StringToColor(item.Data.TextColor) ?? Color.White);
                }
            }

            // advance or end dialogue prompt
            if (this.CutsceneInputTimer > this.MenuData.DialogueIgnoreInputTime)
            {
                Rectangle region;
                Vector2 promptPosition = new Vector2(x: bounds.Right, y: bounds.Bottom);
                float offset = (float)Math.Cos(this.CutsceneTimer * Math.PI / 512d) * scale;
                if (this.CutsceneIndex < this.CutsceneData.Items.Count - 1)
                {
                    promptPosition.X += offset;
                    region = this.MenuData.DialogueAdvanceTextureRegion;
                }
                else
                {
                    promptPosition.Y += offset;
                    region = this.MenuData.DialogueEndTextureRegion;
                }
                b.Draw(
                    texture: this.MenuData.MenuTexture,
                    position: promptPosition,
                    sourceRectangle: region,
                    color: Color.White,
                    rotation: 0,
                    origin: region.Size.ToVector2() / 2,
                    scale: scale,
                    effects: SpriteEffects.None,
                    layerDepth: 1);
            }

            this.drawMouse(b, ignore_transparency: true, cursor: Game1.cursor_gamepad_pointer);
        }
    }
}
