using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using System;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class UI : GameElement
    {
        public SpriteAnimator PortraitAnimator;
        public static readonly Point Margin = new Point(2);

        private static int StageTimeHudDigits = 2;

        private int _screenFlashTimer;
        private Color _screenFlashColor;


        public UI()
            : base()
        {
            PortraitAnimator = new SpriteAnimator()
            {
                Position = new Vector2(2, Margin.Y),
                AlignX = AlignX.Left,
                AlignY = AlignY.Bottom
                // SpriteMirror = i % 2 == 0
            };
        }

        public void HandleInput(Keys k)
        {

        }

        public void ScreenFlash(Color colour, int milliseconds)
        {
            _screenFlashColor = colour;
            _screenFlashTimer = milliseconds;
        }

        public void DrawBread(SpriteBatch b, Rectangle viewport)
        {
            // debug makito
            // very important
            Game.Draw(
                b: b,
                viewport: viewport,
                position: Vector2.Zero,
                sourceRectangle: new Rectangle(0, 0, TD, TD),
                origin: new Vector2(TD / 2),
                alignX: AlignX.Centre,
                alignY: AlignY.Centre);
            // very important
            // debug makito
        }

        /// <summary>
        /// DEBUG: render line from start to end of bullet target trail
        /// </summary>
        public void DrawLine(SpriteBatch b, Vector2 origin, Vector2 target)
        {
            // create 1x1 white texture for line drawing
            Texture2D t = new Texture2D(graphicsDevice: Game1.graphics.GraphicsDevice, width: 2, height: 2);
            t.SetData(new[] { Color.White, Color.White, Color.White, Color.White });

            Vector2 line = target - origin;
            float angle = Utils.Vector.RadiansBetween(origin, target);
            b.Draw(
                texture: Game.ArcadeTexture,
                destinationRectangle: new Rectangle(
                    (int)origin.X,
                    (int)origin.Y,
                    (int)line.Length(),
                    1),
                sourceRectangle: null,
                color: Color.Red,
                rotation: angle,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: 1);
        }

        /// <summary>
        /// Renders a number digit-by-digit on screen to the target rectangle using the arcade number sprites.
        /// </summary>
        /// <param name="number">Number to draw.</param>
        /// <param name="maxDigits">Number of digits to draw. Will draw leading zeroes if number is shorter.</param>
        /// <param name="drawLeftToRight">Whether the X origin is a start or end point. Number will not be reversed.</param>
        /// <param name="position">Target rectangle to draw to.</param>
        /// <param name="origin">Offset of x,y coordinates to draw to.</param>
        /// <param name="layerDepth">Occlusion value between 0f and 1f.</param>
        public void DrawDigits(
            SpriteBatch b,
            Rectangle viewport,
            int number,
            int maxDigits,
            bool drawLeftToRight,
            Vector2 position,
            Vector2? origin = null,
            AlignX alignX = AlignX.Left,
            AlignY alignY = AlignY.Top,
            float layerDepth = 1f)
        {
            origin ??= Vector2.Zero;
            if (drawLeftToRight)
                position.X += maxDigits * HudDigitSprite.Width;
            int digits = 1;
            int divisor = 1;
            while (digits <= maxDigits)
            {
                int index = number >= divisor
                    ? number % (divisor * 10) / divisor
                    : 0;
                Game.Draw(
                    b: b,
                    viewport: viewport,
                    position: AlignToViewport(viewport: viewport, position: position, alignX: alignX, alignY: alignY),
                    sourceRectangle: new Rectangle(
                        HudDigitSprite.X + HudDigitSprite.Width * index,
                        HudDigitSprite.Y,
                        HudDigitSprite.Width,
                        HudDigitSprite.Height),
                    origin: origin.Value,
                    layerDepth: layerDepth);

                divisor *= 10;
                digits++;
            }
        }

        private void DrawStageHud(SpriteBatch b, Rectangle viewport, Stage stage)
        {
            Rectangle area;

            // Stage time remaining countdown
            Game.Draw(
                b: b,
                viewport: viewport,
                position: new Vector2(
                    x: -(Margin.X + HudDigitSprite.Width * (StageTimeHudDigits + 2)),
                    y: -(Margin.Y + HudDigitSprite.Height)),
                sourceRectangle: HudTimeSprite,
                alignX: AlignX.Right,
                alignY: AlignY.Top);

            // Flash timer when less than 10 seconds remain
            if (stage.Timer >= 10000 || stage.Timer < 10000 && stage.Timer % 400 < 200)
            {
                DrawDigits(
                    b: b,
                    viewport: viewport,
                    number: stage.Timer / 1000,
                    maxDigits: StageTimeHudDigits,
                    drawLeftToRight: false,
                    position: new Vector2(0, -(Margin.Y + HudDigitSprite.Height)),
                    alignX: AlignX.Right,
                    alignY: AlignY.Top);
            }

            // Stage enemy health
            {
                // Label
                Point position = new Point(0, Margin.Y);
                Game.Draw(
                    b: b,
                    viewport: viewport,
                    position: position.ToVector2(),
                    sourceRectangle: HudHealthTextSprite,
                    origin: HudHealthTextSprite.Size.ToVector2() / 2,
                    alignX: AlignX.Centre,
                    alignY: AlignY.Top);

                const int healthBarWidth = 120;
                const int healthBarHeight = 14;
                const int frameWidth = 1;

                // Frame outer
                area = new Rectangle(
                        position.X - healthBarWidth - frameWidth * 2,
                        position.Y - frameWidth * 2,
                        healthBarWidth + frameWidth * 4,
                        healthBarHeight + frameWidth * 4);
                Game.DrawColour(
                    b: b,
                    viewport: viewport,
                    colour: PaletteColour.Black,
                    area: area,
                    origin: area.Center.ToVector2(),
                    alignX: AlignX.Centre,
                    alignY: AlignY.Top,
                    layerDepth: 1f - 1f / 10000f - 1f / 10000f - 1f / 10000f);

                // Frame inner
                area = new Rectangle(
                        position.X - healthBarWidth - frameWidth,
                        position.Y,
                        healthBarWidth + frameWidth * 2,
                        healthBarHeight);
                Game.DrawColour(
                    b: b,
                    viewport: viewport,
                    colour: PaletteColour.LightBlue,
                    area: area,
                    origin: area.Center.ToVector2(),
                    alignX: AlignX.Centre,
                    alignY: AlignY.Top,
                    layerDepth: 1f - 1f / 10000f - 1f / 10000f);

                // Health bar in frame
                float healthPercentage = (float)stage.ScoreCur / stage.Score;
                area = new Rectangle(
                    position.X - healthBarWidth / 2,
                    position.Y,
                    (int)(healthBarWidth * healthPercentage),
                    healthBarHeight);
                Game.DrawColour(
                    b: b,
                    viewport: viewport,
                    colour: PaletteColour.Red,
                    area: area,
                    alignX: AlignX.Centre,
                    alignY: AlignY.Top,
                    layerDepth: 1f - 1f / 10000f);
            }
        }

        private void DrawPlayerHud(SpriteBatch b, Rectangle viewport, Stage stage, Player player)
        {
            // Player portrait
            int whichPortrait = 0; // Default
            int whichBackdrop = (int)PaletteColour.Blue;

            if (player.HealthCur == 0 && stage.Timer % 1000 < 400)
            {
                whichPortrait = 7; // Out 2
                whichBackdrop = (int)PaletteColour.Blue;
            }
            else if (player.HealthCur == 0)
            {
                whichPortrait = 6; // Out 1
                whichBackdrop = (int)PaletteColour.Blue;
            }
            else if (Game.ActivePowerPhase == PowerPhase.AfterActive2)
            {
                whichPortrait = 5; // Power end 2
                whichBackdrop = (int)PaletteColour.LightBlue;
            }
            else if (Game.ActivePowerPhase == PowerPhase.AfterActive1)
            {
                whichPortrait = 4; // Power end 1
                whichBackdrop = (int)PaletteColour.LightBlue;
            }
            else if (Game.ActivePowerPhase == PowerPhase.BeforeActive1
                     || Game.ActivePowerPhase == PowerPhase.BeforeActive2)
            {
                whichPortrait = 3; // Power start
                whichBackdrop = (int)PaletteColour.LightBlue;
            }
            else if (player.InvincibleTimer > 0)
            {
                whichPortrait = 2; // Hurt
                whichBackdrop = (int)PaletteColour.LightRed;
            }
            else if (player.RespawnTimer > 0)
            {
                whichPortrait = 1; // Respawn and end-of-stage pose
                whichBackdrop = (int)PaletteColour.LightBlue;
            }

            {
                const int borderWidth = 1;

                // Frame
                Game.DrawColour(
                    b: b,
                    viewport: viewport,
                    colour: PaletteColour.White,
                    area: new Rectangle(
                        0,
                        Margin.Y,
                        HudPortraitSprite.Width,
                        HudPortraitSprite.Height),
                    alignX: AlignX.Left,
                    alignY: AlignY.Bottom,
                    layerDepth: 1f - 1f / 10000f - 1f / 10000f);
                // Backdrop
                Game.DrawColour(
                    b: b,
                    viewport: viewport,
                    colour: (PaletteColour)whichBackdrop,
                    area: new Rectangle(
                        borderWidth,
                        Margin.Y + borderWidth,
                        HudPortraitSprite.Width - borderWidth * 2,
                        HudPortraitSprite.Height - borderWidth * 2),
                    alignX: AlignX.Left,
                    alignY: AlignY.Bottom,
                    layerDepth: 1f - 1f / 10000f);
                // Character
                Game.Draw(
                    b: b,
                    viewport: viewport,
                    position: new Vector2(0, Margin.Y),
                    sourceRectangle: new Rectangle(
                        HudPortraitSprite.X + HudPortraitSprite.Width * whichPortrait,
                        HudPortraitSprite.Y,
                        HudPortraitSprite.Width,
                        HudPortraitSprite.Height),
                    texture: player.IsPlayerOne
                        ? Game.ArcadeTexture
                        : Game.Player2Texture,
                    alignX: AlignX.Left,
                    alignY: AlignY.Bottom,
                    effects: player.IsPlayerOne ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            }

            for (int i = 0; i < player.Health; ++i)
            {
                // Player health icons, lost health greyed out
                Game.Draw(
                    b: b,
                    viewport: viewport,
                    position: new Vector2(
                        x: HudPortraitSprite.Width + HudLifeSprite.Width * i * (player.IsPlayerOne ? 1 : -1),
                        y: Margin.Y),
                    sourceRectangle: HudLifeSprite,
                    alignX: AlignX.Left,
                    alignY: AlignY.Bottom,
                    colour: i < player.HealthCur
                     && (player.HealthCur > 1 || player.HealthCur == 1 && stage.Timer % 400 < 200)
                        ? Color.White
                        : Color.DarkSlateBlue);
            }

            for (int i = 0; i < player.Energy; ++i)
            {
                // Player energy icons, inactive ones greyed out
                Game.Draw(
                    b: b,
                    viewport: viewport,
                    position: new Vector2(
                        HudPortraitSprite.Width + HudEnergySprite.Width * i * (player.IsPlayerOne ? 1 : -1),
                        Margin.Y + HudLifeSprite.Height),
                    sourceRectangle: HudEnergySprite,
                    colour: i < player.EnergyCur
                        ? Color.White
                        : Color.DarkSlateBlue);
            }
        }

        public override State Update(TimeSpan time)
        {
            if (_screenFlashTimer > 0)
                _screenFlashTimer -= time.Milliseconds;

            return State.IsAlive;
        }

        public void Draw(SpriteBatch b, Rectangle viewport, Scene scene, Player player, Stats stats)
        {
            // TODO: ASSETS: Draw extra flair around certain hud elements
            // see 1581165827502.jpg of viking with hud

            // Letterboxing
            // top and bottom
            Game.DrawColour(b: b, colour: Color.Black, area: new Rectangle(0, 0, Game1.viewport.Width, viewport.Top));
            Game.DrawColour(b: b, colour: Color.Black, area: new Rectangle(0, viewport.Bottom, Game1.viewport.Width, viewport.Top));
            // left and right
            Game.DrawColour(b: b, colour: Color.Black, area: new Rectangle(0, viewport.Top, viewport.Left, viewport.Height));
            Game.DrawColour(b: b, colour: Color.Black, area: new Rectangle(viewport.Right, viewport.Top, viewport.Left, viewport.Height));

            // Screen flash
            if (_screenFlashTimer > 0)
            {
                Game.DrawColour(b: b, viewport: viewport, colour: _screenFlashColor, layerDepth: 1f - 1f / 10000f);
            }

            if (scene is Stage stage)
            {
                // Player info
                DrawPlayerHud(b: b, viewport: viewport, stage: stage, player: player);

                // Stage info
                DrawStageHud(b: b, viewport: viewport, stage: stage);

                // Total score
                DrawDigits(
                    b: b,
                    viewport: viewport,
                    number: stats.Score + stage.Stats.Score,
                    maxDigits: 8,
                    drawLeftToRight: true,
                    position: new Vector2(0, -(Margin.Y + HudDigitSprite.Height)),
                    alignX: AlignX.Left,
                    alignY: AlignY.Top);
            }
        }
    }
}
