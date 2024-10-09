using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HikawaArcade.Arcade.Objects
{
    public class GenericSpriteAnimator : GameElement
    {
        public enum Strategy
        {
            Clear,
            Clamp,
            Reset,
            Repeat
        }

        public SpriteFrame[] Animation;
        protected int FrameDuration = 0;
        protected int FrameIndex = 0;
        protected int Repeats = 0;
        protected int RepeatsCurrent = 0;
        protected float LayerDepth = 1f;
        protected bool Animating = false;
        protected Strategy EndStrategy = Strategy.Clear;


        public GenericSpriteAnimator()
            : base()
        {
        }

        protected void Still(int index)
        {
            Set(index: index);
            Animating = false;
        }

        protected void Set(int index)
        {
            Visible = true;

            FrameIndex = index;
            FrameDuration = 0;
        }

        public override State Update(TimeSpan time)
        {
            FrameDuration += time.Milliseconds;
            if (FrameDuration > Animation[FrameIndex].Duration)
            {
                if (++FrameIndex >= Animation.Length && ++RepeatsCurrent >= Repeats)
                {
                    FrameDuration = 0;
                    if (EndStrategy == Strategy.Clamp)
                    {
                        Still(index: FrameIndex - 1);
                    }
                    else if (EndStrategy == Strategy.Clear)
                    {
                        Animating = false;
                        Visible = false;
                    }
                    else if (EndStrategy == Strategy.Repeat)
                    {
                        Set(index: 0);
                    }
                    else if (EndStrategy == Strategy.Reset)
                    {
                        Still(index: 0);
                    }
                    return State.IsAlive;
                }
            }
            return State.IsAlive;
        }

        public override void Reset()
        {
            FrameIndex = 0;
            Repeats = RepeatsCurrent = 0;
            LayerDepth = 1f;
            base.Reset();
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is GenericSpriteAnimator t)
            {
                t.Animation = Animation;
            }
            return base.CopyTo(target);
        }
    }
}
