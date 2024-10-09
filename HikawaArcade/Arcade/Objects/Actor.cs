using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Actor : GameElement
    {
        // Generic
        public string Type = null;
        public Point SpriteOrigin = Point.Zero;
        public Point SpriteSize = Point.Zero;
        public Rectangle SpriteArea = Rectangle.Empty;
        public float SpeedCur = 0;
        public float Speed = 0;
        public int Size = 0;
        public string DieSound = null;
        public string SpawnSound = null;
        public int FireRate = 0;
        public int FireCount = 0;
        public int FireTime = 0;
        public int FireSpread = 0;
        public string FireType = null;
        public string FireName = null;

        // Unique
        public Actor Owner = null;
        public Actor CollidingWith = null;
        public Vector2 Position = Vector2.Zero;
        public SpriteEffects SpriteMirror = SpriteEffects.None;
        public Color Colour = Color.White;
        public int AnimationTimer = 0;
        public int ActionTimer = 0;
        public int FireCountRemaining = 0;
        public virtual bool CanBeDrawn { get => Visible; }


        public Actor()
            : base()
        {
        }

        public virtual void Fire(Vector2 target)
        {
            ActionTimer = FireTime;
        }

        public virtual Actor Spawn(Vector2 position)
        {
            Game.PlaySound(SpawnSound);
            Position = position;
            return this;
        }

        public virtual void Die()
        {
            Game.PlaySound(DieSound);
        }

        public virtual bool IsOnScreen()
        {
            return Position.X > -Size
                && Position.X < Width + Size
                && Position.Y > -Size
                && Position.Y < Height + Size;
        }

        public virtual bool IsCollidingWith(Vector2 position)
        {
            return Vector2.Distance(Position, position) < Size;
        }

        public virtual bool IsCollidingWith(Actor actor)
        {
            return Vector2.Distance(Position, actor.Position) < Size + actor.Size;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            Game.Draw(
                b: b,
                viewport: viewport,
                position: Position,
                sourceRectangle: SpriteArea,
                colour: Colour,
                effects: SpriteMirror);
        }

        public override State Update(TimeSpan time)
        {
            if (ActionTimer > 0)
                ActionTimer -= time.Milliseconds;

            return base.Update(time);
        }

        public override void Reset()
        {
            SpriteArea.X = SpriteOrigin.X;
            SpriteArea.Y = SpriteOrigin.Y;
            SpriteArea.Width = SpriteSize.X;
            SpriteArea.Height = SpriteSize.Y;
            CollidingWith = Owner = null;
            Position = default;
            SpriteMirror = default;
            Colour = default;
            AnimationTimer = ActionTimer = 0;
            Visible = true;
            FireCountRemaining = 0;
            SpeedCur = Speed;
            base.Reset();

            Visible = true;
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is Actor t)
            {
                t.Type = Type;
                t.FireRate = FireRate;
                t.FireCount = FireCount;
                t.FireTime = FireTime;
                t.FireSpread = FireSpread;
                t.FireType = FireType;
                t.FireName = FireName;
                t.SpriteOrigin = SpriteOrigin;
                t.SpriteSize = SpriteSize;
                t.SpriteArea = SpriteArea;
                t.Speed = Speed;
                t.Size = Size;
                t.SpawnSound = SpawnSound;
                t.DieSound = DieSound;
            }
            return base.CopyTo(target);
        }
    }
}
