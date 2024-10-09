using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Pickup : Actor, Interfaces.ICopyable
    {
        // Generic
        public int Duration;
        public double Chance;
        public double ChanceOverTime;
        public string PickupSound;

        // Unique
        public int DurationCur;
        public Vector2 Target;


        public Pickup()
            : base()
        {
            // Pick a texture for the powerup drop
            switch (Type)
            { // HARD Y
                case "Cake":
                    // Decide which cake to show
                    /*double d = this.Game.Random.NextDouble();
					if (d > 0.05)
					{
						int x = (int)(CakesFrames * (d * Math.Floor(10d / (CakesFrames))));
						this.SpriteArea = new Rectangle(TD * x, TD, TD, TD);
					}
					else
					{
						this.SpriteArea = new Rectangle(0, FillColours.Value.Height, TD, TD - FillColours.Value.Height);
					}*/
                    break;
                case "Life":
                    break;
                case "Energy":
                    break;
                case "Time":
                    break;
                case "Megahealth":
                    break;
            }
        }

        public override State Update(TimeSpan time)
        {
            // Powerup expired
            if (DurationCur <= 0)
            {
                return State.IsDead;
            }

            // Powerup dropping in from spawn
            if (Position.Y < Target.Y)
            {
                Position.Y = Math.Min(Target.Y, Position.Y + SpeedCur);
            }
            else
            {
                DurationCur -= time.Milliseconds; // Run down duration from the time it reaches the ground
            }

            return State.IsAlive;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            if (DurationCur <= 2000 && DurationCur / 250 % 2 != 0)
                return;

            // Pickup
            Game.Draw(
                b: b,
                viewport: viewport,
                position: Position,
                sourceRectangle: SpriteArea,
                colour: Colour,
                effects: SpriteMirror,
                layerDepth: Position.Y / 10000f + 1f / 1000f);

            // Shadow
            Game.Draw(
                b: b,
                viewport: viewport,
                position: Target,
                sourceRectangle: LootShadowRect,
                layerDepth: Position.Y / 10000f);
        }

        public override void Reset()
        {
            DurationCur = Duration;
            base.Reset();
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is Pickup t)
            {
                t.Type = Type;
                t.Duration = Duration;
            }
            return base.CopyTo(target);
        }
    }
}
