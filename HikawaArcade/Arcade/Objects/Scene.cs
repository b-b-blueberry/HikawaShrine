using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Scene : GameElement, Interfaces.ICopyable, Interfaces.IHandleInput
    {
        public int Timer;
        public int Phase
        {
            get => _phase;
            set
            {
                _phase = value;
                Timer = 0;
            }
        }
        private int _phase;

        public delegate void OnEndBehaviour();
        public OnEndBehaviour EndBehaviour;


        public Scene()
            : base()
        {
        }

        public override State Update(TimeSpan time)
        {
            return State.IsAlive;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
        }

        public override void Reset()
        {
            Phase = 0;
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is Scene t)
            {
                t.EndBehaviour = EndBehaviour;
            }
            return base.CopyTo(target);
        }

        public virtual void HandleInput(Keys k)
        {
        }

        public virtual void HandleInputReleased(Keys k)
        {
        }

        public virtual void HandleClick(int x, int y)
        {
        }

        public virtual void HandleClickReleased(int x, int y)
        {
        }
    }
}
