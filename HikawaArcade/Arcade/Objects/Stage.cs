using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using System;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Stage : Scene
    {
        public string Id;
        public int Score;
        public int ScoreCur;
        public int Time;
        public int PlayerHeightFromBottom;

        public Stats Stats;

        public Pool<Monster> MonsterPool;
        public Pool<Bullet> BulletPool;
        public Pool<Actor> PickupPool;
        public Pool<Particle> ParticlePool;
        public Pool<CopySpawner<Monster>> MonsterSpawnerPool;

        internal const int StageTimeInitial = 60000;
        internal const int StageTimeExtra = 15000;
        internal const int StageTimeCritical = 5000;

        public bool IsTimeCritical => Timer <= StageTimeCritical;


        public Stage(string id)
            : base()
        {
            Stats = new Stats();
            Set(id: id);
        }

        public virtual void Set(string id)
        {
            Id = id;
            switch (id)
            {
                default:
                    {
                        MonsterPool = new Pool<Monster>(capacity: 8);
                        BulletPool = new Pool<Bullet>(capacity: 64);
                        PickupPool = new Pool<Actor>(capacity: 8);
                        ParticlePool = new Pool<Particle>(capacity: 64);
                        MonsterSpawnerPool = new Pool<CopySpawner<Monster>>(capacity: 8);

                        ScoreCur = Score = 30;
                        Timer = StageTimeInitial;

                        break;
                    }
            }
        }

        public virtual void AddTime()
        {
            Timer = Math.Min(Time, Timer + StageTimeExtra);
        }

        public virtual void UpdateMonsters(TimeSpan time)
        {
            MonsterPool.ForEach((monster) =>
            {
                bool isDead = monster.Update(time: time) == State.IsDead;
                if (!isDead && monster.ImpactDamage > 0 && monster.IsCollidingWith(actor: Game.Player))
                {
                    Game.Player.TakeDamage(damage: monster.ImpactDamage);
                    SpriteFrame[] frames =
                        new SpriteFrame[] { new SpriteFrame() { SpriteSource = new Rectangle(64, 96, 32, 32), Duration = 1250 }
                    };
                    Game.UI.PortraitAnimator.Animate(frames: frames, endStrategy: GenericSpriteAnimator.Strategy.Reset);
                    isDead = true;
                }
                if (isDead)
                {
                    ++Stats.MonstersDead;
                    Stats.Score += monster.Health;

                    Actor loot = monster.GetLootDrop(target: new Vector2(monster.Position.X, PlayerHeightFromBottom), timeElapsed: Time - Timer);
                    if (loot != null)
                    {
                        PickupPool.Add(loot);
                    }
                    monster.Die();
                }
                return isDead;
            });
        }

        public virtual void UpdateBullets(TimeSpan time)
        {
            BulletPool.ForEach((bullet) =>
            {
                bool isDead = bullet.Update(time: time) == State.IsDead;

                if (!bullet.IsOnScreen())
                {
                    isDead = true;
                }

                if (bullet.Owner is Player owner && bullet.CollidingWith is Monster collider)
                {
                    if (collider.InvincibleTimer <= 0)
                    {
                        ++Stats.ShotsSuccessful;
                        DamagePacket damage = collider.TakeDamage(bullet.BulletDamage);
                        if (damage.IsAlive)
                        {
                            Game.PlaySound(id: collider.HurtSound);
                        }
                        else
                        {
                            Game.PlaySound(id: collider.DieSound);
                        }

                        isDead = true;
                    }
                }
                else if (bullet.Owner is Monster monster && bullet.CollidingWith is Player player)
                {
                    // Damage the player
                    if (player.InvincibleTimer <= 0)
                    {
                        ++Stats.HitsTaken;

                        DamagePacket damage = player.TakeDamage(bullet.BulletDamage);
                        if (damage.DamageReceived > 0)
                        {
                            Game.PlaySound(id: player.HurtSound);
                            Game.UI.ScreenFlash(colour: new Color(new Vector4(255, 0, 0, 0.25f)), milliseconds: 250);
                            SpriteFrame[] frames = new SpriteFrame[] { new SpriteFrame() { SpriteSource = new Rectangle(64, 96, 32, 32), Duration = 1250 } };
                            Game.UI.PortraitAnimator.Animate(frames: frames, endStrategy: GenericSpriteAnimator.Strategy.Reset);
                            if (damage.IsAlive)
                            {
                                Game.PlaySound(id: player.HurtSound);
                            }
                            else
                            {
                                Game.PlaySound(id: player.DieSound);
                            }
                        }

                        isDead = true;
                    }
                }

                if (isDead)
                {
                    bullet.Die();
                }
                return isDead;
            });
        }

        public virtual void UpdatePickups(TimeSpan time)
        {
            PickupPool.ForEach((actor) =>
            {
                bool isDead = actor.Update(time: time) == State.IsDead;
                if (isDead)
                {
                    if (actor is Pickup pickup && pickup.CollidingWith is Player player)
                    {
                        switch (pickup.Type)
                        {
                            case "Cake":
                                Stats.Score += ScoreCake + pickup.SpriteArea.X / pickup.SpriteArea.Width * ScoreCakeExtra;
                                break;
                            case "Life":
                                ++player.HealthCur;
                                break;
                            case "Energy":
                                ++player.EnergyCur;
                                break;
                            case "Time":
                                AddTime();
                                break;
                            case "Bread":
                                Stats.Score += ScoreBread;
                                break;
                            case "Megahealth":
                                player.IsHealthRegenerating = true;
                                break;
                        }
                        Game.PlaySound(pickup.PickupSound);
                    }
                    actor.Die();
                }
                return isDead;
            });
        }

        public virtual void UpdateParticles(TimeSpan time)
        {
            ParticlePool.ForEach((particle) =>
            {
                bool isDead = particle.Update(time: time) == State.IsDead;
                if (isDead)
                {
                }
                return isDead;
            });
        }

        public virtual void DrawBackground(SpriteBatch b, Rectangle viewport)
        {
            switch (Game.ActiveSpecialPower)
            {
                case SpecialPower.Normal:
                case SpecialPower.Megaton:
                    if (Game.ActivePowerPhase is > PowerPhase.BeforeActive2 and < PowerPhase.AfterActive2)
                    {
                        Game.DrawColour(b: b, viewport: viewport, colour: PaletteColour.Black, layerDepth: 1 / 10000f);
                    }
                    else
                    {
                        goto case SpecialPower.None;
                    }
                    break;
                case SpecialPower.Sulphur:
                case SpecialPower.Incense:
                case SpecialPower.None:
                    // Draw the game map
                    Game.DrawColour(b: b, viewport: viewport, colour: PaletteColour.Brown, layerDepth: 1 / 10000f);
                    break;
            }
        }

        public virtual void DrawElements(SpriteBatch b, Rectangle viewport)
        {
            MonsterPool.ForEach((monster) =>
            {
                if (monster.CanBeDrawn)
                {
                    monster.Draw(b: b, viewport: viewport);
                }
            });
            BulletPool.ForEach((bullet) =>
            {
                if (bullet.CanBeDrawn)
                {
                    bullet.Draw(b: b, viewport: viewport);
                }
            });
            ParticlePool.ForEach((particle) =>
            {
                if (particle.CanBeDrawn)
                {
                    particle.Draw(b: b, viewport: viewport);
                }
            });
            PickupPool.ForEach((pickup) =>
            {
                if (pickup.CanBeDrawn)
                {
                    pickup.Draw(b: b, viewport: viewport);
                }
            });
        }

        public override State Update(TimeSpan time)
        {
            Timer -= time.Milliseconds;
            if (Timer <= 0)
            {
                return State.IsDead;
            }

            UpdateMonsters(time: time);
            UpdateBullets(time: time);
            UpdatePickups(time: time);
            UpdateParticles(time: time);

            return State.IsAlive;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            DrawBackground(b: b, viewport: viewport);
            Game.Player.Draw(b: b, viewport: viewport);
            DrawElements(b: b, viewport: viewport);
        }

        public override void HandleInput(Keys k)
        {
            if (k == Keys.F)
            {
                // debug
                MonsterSpawnerPool.ForEach((spawner) =>
                {

                });
            }
        }

        public override void HandleInputReleased(Keys k)
        {
        }

        public override void HandleClick(int x, int y)
        {
            base.HandleClick(x, y);

            if (Game.Player.RespawnTimer <= 0 && Game.Player.ActionTimer <= 1)
            {
                // todo dont do this
                if (Game.BulletTemplate["Gun"] is not null and Bullet bullet)
                {
                    BulletPool.Add(bullet, strategy: Pool<Bullet>.Strategy.Replace);
                    bullet.Fire(target: Game.GetViewportCursorPosition());
                    Game.Player.Fire(target: bullet.Target);
                    ++Stats.ShotsFired;
                }
            }
        }

        public override void Reset()
        {
            Set(id: Id);
            Stats.Reset();
            BulletPool.Reset();
            MonsterPool.Reset();
            PickupPool.Reset();
            base.Reset();
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is Stage t)
            {
                t.Id = Id;
                t.Time = Time;
                t.PlayerHeightFromBottom = PlayerHeightFromBottom;
            }
            return base.CopyTo(target);
        }
    }
}
