using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class SpriteAnimator : GenericSpriteAnimator
    {
        public Texture2D SpriteSheet;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Origin = Vector2.Zero;
        public AlignX AlignX = AlignX.Left;
        public AlignY AlignY = AlignY.Top;
        public SpriteEffects SpriteMirror = SpriteEffects.None;

        public SpriteAnimator()
            : base()
        {
        }

        public void Animate(SpriteFrame[] frames, Strategy endStrategy,
            Texture2D spriteSheet = null, Vector2? position = null, Vector2? origin = null, AlignX alignX = AlignX.Left, AlignY alignY = AlignY.Top, SpriteEffects? spriteMirror = null,
            int repeats = 0, int startingFrame = 0, bool startPaused = false, float layerDepth = 1f)
        {
            Animation = frames;

            SpriteSheet = spriteSheet ?? Game.ArcadeTexture;
            Position = position ?? Position;
            EndStrategy = endStrategy;
            RepeatsCurrent = 0;
            Repeats = repeats;
            Origin = origin ?? Vector2.Zero;
            AlignX = alignX;
            AlignY = alignY;
            SpriteMirror = spriteMirror ?? SpriteMirror;
            LayerDepth = layerDepth;

            Set(index: startingFrame);
            Animating = !startPaused;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            Game.Draw(
                b: b,
                viewport: viewport,
                position: Position,
                sourceRectangle: Animation[FrameIndex].SpriteSource,
                texture: SpriteSheet,
                effects: SpriteMirror,
                layerDepth: LayerDepth);
        }

        public override void Reset()
        {
            SpriteSheet = null;
            Position = Origin = default;
            SpriteMirror = default;
            base.Reset();
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is SpriteAnimator t)
            {
                t.SpriteSheet = SpriteSheet;
            }
            return base.CopyTo(target);
        }
    }
}
