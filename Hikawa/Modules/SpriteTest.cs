using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Hikawa.Modules
{
    internal static class SpriteTest
    {
        private static Texture2D _texture => ModEntry.Sprites;
        private static readonly int X = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Center.X;
        private static readonly int Y = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Center.Y;
        private static float _yOffset;
        private static readonly Rectangle SourceRectGlare = new Rectangle(
            96, 144, 112, 32);
        private static readonly List<Rectangle> SourceRects = new List<Rectangle>
        {
			// 0 4 3 1 2
			new Rectangle(64, 32, 128, 38),
            new Rectangle(160, 74, 48, 64),
            new Rectangle(112, 74, 48, 64),
            new Rectangle(0, 80, 64, 64),
            new Rectangle(64, 80, 48, 64),
        };
        private static readonly Rectangle DestRectGlare = new Rectangle(
            X, Y - Y / 3 * 2, SourceRectGlare.Width * 4, SourceRectGlare.Height * 4);
        private static readonly List<Rectangle> DestRects = new List<Rectangle>
        {
			// 0 4 3 1 2
			new Rectangle(X - 16 * 4, Y + Y / 4 - 16 * 4, 128 * 4, 38 * 4),
            new Rectangle(X + 32 * 4, Y + Y / 4, 48 * 4, 64 * 4),
            new Rectangle(X + 16 * 4, Y + Y / 4, 48 * 4, 64 * 4),
            new Rectangle(X - 24 * 4, Y + Y / 4, 64 * 4, 64 * 4),
            new Rectangle(X + 00 * 4, Y + Y / 4, 48 * 4, 64 * 4),
        };


        internal static void Init(IModHelper helper)
        {
            // helper.Events.Display.Rendering += OnRendering;
        }

        private static void OnRendering(object sender, RenderingEventArgs e)
        {
            _yOffset = 6f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / (Math.PI * 300f));

            // Crystal glare
            e.SpriteBatch.Draw(_texture,
                new Rectangle(DestRectGlare.X,
                    DestRectGlare.Y + (int)Math.Ceiling(_yOffset),
                    DestRectGlare.Width,
                    DestRectGlare.Height),
                SourceRectGlare,
                Color.White,
                0f,
                new Vector2(SourceRectGlare.Width / 2, SourceRectGlare.Height / 2),
                SpriteEffects.None,
                1f);

            // Crystal ball
            for (int i = 0; i < SourceRects.Count; ++i)
            {
                e.SpriteBatch.Draw(_texture,
                    new Rectangle(
                        DestRects[i].X,
                        DestRects[i].Y + (int)Math.Ceiling(_yOffset + _yOffset * Math.Abs(DestRects.Count / 2 - i) / 2),
                        //DestRects[i].Y + (int)Math.Ceiling(_yOffset),
                        DestRects[i].Width,
                        DestRects[i].Height),
                    SourceRects[i],
                    Color.White,
                    0f,
                    new Vector2(SourceRects[i].Width / 2, SourceRects[i].Height / 2),
                    SpriteEffects.None,
                    0.9f - i / 10000f);
            }
        }
    }
}
