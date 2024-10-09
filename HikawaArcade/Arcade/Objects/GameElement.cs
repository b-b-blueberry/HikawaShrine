using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HikawaArcade.Arcade.Objects
{
    public abstract class GameElement : Interfaces.ICopyable
    {
        public enum State
        {
            IsAlive = 0,
            IsDead = 1
        }

        // Common
        protected ArcadeGame Game;

        // Unique
        public bool Visible;


        protected GameElement()
        {
            Game = StardewValley.Game1.currentMinigame as ArcadeGame;
        }

        public virtual State Update(TimeSpan time) { return State.IsAlive; }
        public virtual void Draw(SpriteBatch b, Rectangle viewport) { }

        public virtual void Reset()
        {
            Visible = false;
        }

        public virtual Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            target.Reset();
            if (target is GameElement t)
            {
                t.Game = Game;
            }
            return target;
        }
    }
}
