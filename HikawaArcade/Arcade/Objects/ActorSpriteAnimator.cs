using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HikawaArcade.Arcade.Objects
{
    public class ActorSpriteAnimator : GenericSpriteAnimator
    {
        Actor Actor;


        public ActorSpriteAnimator()
            : base()
        {
        }

        public void Animate(Actor actor, SpriteFrame[] animation, Strategy endStrategy,
            int repeats = 0, int startingFrame = 0, int defaultFrame = 0, bool startPaused = false, float layerDepth = 1f)
        {
            Actor = actor;

            Animation = animation;
            EndStrategy = endStrategy;
            RepeatsCurrent = 0;
            Repeats = repeats;
            LayerDepth = layerDepth;

            Set(index: startingFrame);
            Animating = !startPaused;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            Game.Draw(
                b: b,
                viewport: viewport,
                position: Actor.Position,
                sourceRectangle: Animation[FrameIndex].SpriteSource,
                effects: Actor.SpriteMirror,
                layerDepth: LayerDepth);
        }

        public override void Reset()
        {
            Actor = null;
            base.Reset();
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is ActorSpriteAnimator t)
            {
                t.Actor = Actor;
            }
            return base.CopyTo(target);
        }
    }
}
