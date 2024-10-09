using HikawaArcade.Arcade.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Particle : Actor
    {
        /// <summary>
        /// Vector of motion between origin and target.
        /// </summary>
        public Vector2 Motion;
        /// <summary>
        /// Local position when created.
        /// </summary>
        public Vector2 Origin;
        /// <summary>
        /// Local position pointed at by vector of motion.
        /// </summary>
        public Vector2 Target;

        public Particle()
            : base()
        {
        }

        public override void Fire(Vector2 target)
        {
            Target = target;
            Motion = Utils.Vector.MotionTo(origin: Origin, target: Target);
            SpriteMirror = (Origin.X < Target.X ? SpriteEffects.FlipHorizontally : 0)
                | (Origin.Y < Target.Y ? SpriteEffects.FlipVertically : 0);
            base.Fire(target);
        }

        public override Actor Spawn(Vector2 position)
        {
            return base.Spawn(position);
        }

        public override void Die()
        {
            base.Die();
        }

        public override State Update(TimeSpan time)
        {
            Position += Motion * SpeedCur;
            return base.Update(time);
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            base.Draw(b, viewport);
        }

        public override void Reset()
        {
            Motion = default;
            Origin = Target = default;
            base.Reset();
        }

        public override ICopyable CopyTo(ICopyable target)
        {
            return base.CopyTo(target);
        }
    }
}
