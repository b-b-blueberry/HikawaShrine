using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Bullet : Particle
    {
        // Generic
        public int BulletDamage;
        public string Animation;

        // Unique


        public Bullet()
            : base()
        {
            int xOffset = 0;
            switch (Type)
            {
                case "Player":
                    break;
                case "Gun":
                    break;
                case "Junk":
                    //xOffset = TD * 2 + (int)(this.SpriteArea.Width / this.Game.Random.NextDouble());
                    //this.SpriteArea = new Rectangle(xOffset, BulletSpriteY, Defs.BulletSize[type], Defs.BulletSize[type]);
                    break;
            }
        }

        public override void Fire(Vector2 target)
        {
            base.Fire(target);
        }

        /// <summary>
        /// Update on-screen bullets per tick, removing as they travel offscreen or hit a target.
        /// </summary>
        public override State Update(TimeSpan time)
        {
            // Update bullet positions
            base.Update(time: time);
            Position += Motion * SpeedCur;

            return State.IsAlive;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            int whichFrame = //int.Parse(this.Animation.Split('/')[1].Split(' ')[this.AnimationTimer / BulletAnimTimescale]);
                0;

            Game.Draw(
                b: b,
                viewport: viewport,
                position: Position,
                sourceRectangle: new Rectangle(
                    SpriteArea.X + SpriteArea.Width * whichFrame,
                    SpriteArea.Y,
                    SpriteArea.Width,
                    SpriteArea.Height),
                colour: Colour,
                effects: SpriteMirror,
                layerDepth: Position.Y / 10000f);
        }

        public override void Reset()
        {
            Motion = default;
            Origin = Target = default;
            base.Reset();
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is Bullet t)
            {
                t.Animation = Animation;
                t.BulletDamage = BulletDamage;
            }
            return base.CopyTo(target);
        }
    }
}
