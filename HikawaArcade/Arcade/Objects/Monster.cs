using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Monster : Character
    {
        protected const int EnemyRunFrames = 3;
        protected const int EnemyAnimTimescale = 200;
        protected const int InvincibleHurtTime = 20;

        // Generic
        public bool Flying;
        public int ImpactDamage;
        public int Score;
        public int IdleTime;
        public double LootRate;

        // Unique


        public Monster()
            : base()
        {
        }

        public override DamagePacket TakeDamage(int damage)
        {
            return base.TakeDamage(damage);
        }

        public override void Fire(Vector2 target)
        {
            base.Fire(target: target);
        }

        public virtual void GetNewAimTarget()
        {
            Vector2 target = new Vector2(0f, Game.Player.Position.Y);

            // Fire as close to the player as possible within the aim bounds for this monster
            if (Game.Player.Position.X < Position.X - FireSpread)
                target.X = Game.Player.Position.X;
            else if (Game.Player.Position.X > Position.X + FireSpread)
                target.X = Position.X + FireSpread;
            else
                target.X = Game.Player.Position.X;

            ActionTarget = target;
        }

        public virtual void GetNewMovementTarget()
        {
            int xpos = 0;
            if (Flying)
            {
                // TODO: SYSTEM: Flying enemem
            }
            else
            {
                xpos = Game.Random.Next(Width / 8, Width / 2);

                // Initial target moves a short distance into the screen
                if (Position.X <= 0)
                {
                    // Spawn from left side
                    xpos = 0 + xpos;
                }
                else if (Position.X + Size >= Width)
                {
                    // Spawn from right side
                    xpos = Width - xpos;
                }
                // Repeat targets swap sides
                else if (Position.X < Width / 2)
                {
                    xpos = Width - xpos;
                }
                else
                {
                    xpos = 0 + xpos;
                }

                // Avoid stutter-stepping
                if (Math.Abs(Position.X - xpos) < Size * 2)
                {
                    xpos += (int)(Position.X - xpos);
                }
            }

            SpriteMirror = Position.X < Width / 2
                ? SpriteEffects.None
                : SpriteEffects.FlipHorizontally;
            ActionTarget.X = xpos;
            ActionTarget.Y = Position.Y;
        }

        public virtual void OnReachedMovementTarget()
        {
            AnimationTimer = 0;
            SpriteMirror = SpriteEffects.None;
            ActionTarget = default;
        }

        /// <summary>
        /// Rolls for a chance to create a new Powerup object at some position.
        /// </summary>
        /// <param name="collisionBox">Spawn position and touch bounds.</param>
        public virtual Actor GetLootDrop(Vector2 target, int timeElapsed)
        {
            Actor loot = null;
            Pickup[] lootDrops = null;
            double roll = Game.Random.NextDouble();
            for (int i = 0; i < lootDrops.Length && loot == null; i++)
            {
                double chance = LootRate * (lootDrops[i].Chance + lootDrops[i].ChanceOverTime * timeElapsed);
                if (roll < chance)
                {
                    loot = lootDrops[i];
                }
            }
            if (loot != null)
            {
                loot = loot.Spawn(position: target);
            }
            return loot;
        }

        public override Actor Spawn(Vector2 position)
        {
            return base.Spawn(position: position);
        }

        public override void Die()
        {
            base.Die();
        }

        public override State Update(TimeSpan time)
        {
            if (HealthCur <= 0)
            {
                return State.IsDead;
            }

            // Prepare to fire
            if (ActionTimer == FireTime)
            {
                GetNewAimTarget();
            }
            else if (ActionTimer > 0 && InvincibleTimer <= 0)
            {
                if (ActionTimer < FireTime / 2)
                {
                    int numToFire = ActionTimer / FireCount % time.Milliseconds;
                    for (int i = 0; i < numToFire; ++i)
                    {
                        int x = FireSpread / FireCountRemaining;
                        x -= x / 2;
                        //if ( is not null and Bullet bullet)
                        {
                            Fire(target: ActionTarget);
                            //_enemyBullets.Add(bullet);
                        }
                    }
                    FireCountRemaining -= numToFire;
                    if (FireCountRemaining <= 0)
                    {
                        ActionTarget = default;
                    }
                }
            }
            else if (InvincibleTimer <= 0)
            {
                // While moving
                if (ActionTarget != default)
                {
                    if (IsCollidingWith(position: ActionTarget))
                    {
                        // Act on reaching target position
                        OnReachedMovementTarget();
                    }
                    else
                    {
                        // Update position
                        if (Math.Abs(Position.X - ActionTarget.X) > SpeedCur)
                            Position.X += SpeedCur * (Position.X > ActionTarget.X ? -1 : 1);
                        if (Math.Abs(Position.Y - ActionTarget.Y) > SpeedCur)
                            Position.Y += SpeedCur * (Position.Y > ActionTarget.Y ? -1 : 1);

                        // Tick up the animation timer while moving
                        AnimationTimer += time.Milliseconds;
                        AnimationTimer %= EnemyRunFrames * EnemyAnimTimescale;
                    }
                }
                // While not moving and neither firing or pausing after firing
                else if (ActionTimer <= 0)
                {
                    // Fetch a new movement target after firing and idling
                    GetNewMovementTarget();
                }
            }
            return State.IsAlive;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            int whichFrame = 0;

            // Run frames
            if (ActionTarget != default)
                whichFrame = 1 + AnimationTimer / EnemyAnimTimescale;

            // Draw the monster
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
                layerDepth: Position.Y / 10000f + 1f / 1000f);

            // Draw the monster's shadow
            float yOffsetFromFlying = Flying ? TD : 0f;
            Game.Draw(
                b: b,
                viewport: viewport,
                position: new Vector2(
                    Position.X,
                    Position.Y + SpriteArea.Height - ActorShadowRect.Height + yOffsetFromFlying),
                sourceRectangle: ActorShadowRect,
                effects: SpriteEffects.None,
                layerDepth: (Position.Y - yOffsetFromFlying) / 10000f);
        }

        public override void Reset()
        {
            base.Reset();
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is Monster t)
            {
                t.Type = Type;
                t.Flying = Flying;
                t.ImpactDamage = ImpactDamage;
                t.Score = Score;
                t.IdleTime = IdleTime;
                t.LootRate = LootRate;
            }
            return base.CopyTo(target);
        }
    }
}
