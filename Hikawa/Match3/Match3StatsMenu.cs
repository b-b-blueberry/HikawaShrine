using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace Hikawa.Match3
{
    public class Match3StatsMenu : IClickableMenu
    {
        public Dictionary<string, string> Stats;

        public int Scale => Game1.pixelZoom;

        public Match3StatsMenu()
            : base()
        {
            this.InitComponents();
            this.UpdateComponentLayout();
        }

        public void InitComponents()
        {
            this.initializeUpperRightCloseButton();

            this.Stats = new Dictionary<string, string>
            {
                { "Time played", ModEntry.SaveData.Match3.TotalTime.ToString() },
                { "Moves played", ModEntry.SaveData.Match3.TotalMoves.ToString() },
                { "Matches", ModEntry.SaveData.Match3.TotalMatches.ToString() },
                { "Power matches", ModEntry.SaveData.Match3.TotalPowerMatches.ToString() },
                { "Super power matches", ModEntry.SaveData.Match3.TotalSuperPowerMatches.ToString() },
                { "Powers used", ModEntry.SaveData.Match3.TotalPowers.ToString() },
                { "Super powers used", ModEntry.SaveData.Match3.TotalSuperPowers.ToString() },
            };
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

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
        }

        public override void update(GameTime time)
        {
            base.update(time);
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

            // Buttons
            {
                if (this.shouldDrawCloseButton())
                    this.upperRightCloseButton.draw(b: b);
            }

            var offset = new Vector2(0, 24) * Scale;
            var colour = Color.White;

            // Stats
            foreach (var pair in this.Stats)
            {
                SpriteFont font = Game1.dialogueFont;
                string textL = pair.Key;
                string textR = pair.Value;
                Vector2 textSizeL = font.MeasureString(textL);
                Vector2 textSizeR = font.MeasureString(textR);
                b.DrawString(font, textL, new Vector2(bounds.Left, bounds.Top) + offset, colour, 0, new Vector2(0, textSizeL.Y / 2), Scale / 4f, SpriteEffects.None, 1);
                b.DrawString(font, textR, new Vector2(bounds.Right, bounds.Top) + offset, colour, 0, new Vector2(textSizeR.X, textSizeR.Y / 2), Scale / 4f, SpriteEffects.None, 1);
                offset += new Vector2(0, Math.Max(textSizeL.Y, textSizeR.Y));
            }

            this.drawMouse(b, ignore_transparency: true, cursor: Game1.cursor_gamepad_pointer);
        }
    }
}
