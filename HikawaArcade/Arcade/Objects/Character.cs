using Microsoft.Xna.Framework;
using System;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public abstract class Character : Actor
    {
        // Generic
        public int Health;
        public int HurtInvincibleTime;
        public string HurtSound;

        // Unique
        private int _healthCur;
        public int HealthCur
        {
            get => _healthCur;
            set
            {
                _healthCur = Math.Max(0, Math.Min(Health, value));
                if (_healthCur == Health)
                    IsHealthRegenerating = false;
            }
        }
        public bool IsHealthRegenerating;
        public int InvisibleTimer;
        public int InvincibleTimer;
        public Vector2 ActionTarget;
        public DamagePacket LastDamage;
        public override bool CanBeDrawn => base.CanBeDrawn && InvincibleTimer > 0 && InvincibleTimer / 100 % 2 != 0;


        protected Character()
            : base()
        {
            LastDamage = new DamagePacket();
        }

        public virtual DamagePacket TakeDamage(int damage)
        {
            int lastHealth = HealthCur;
            HealthCur = Math.Max(0, HealthCur - damage);
            LastDamage.DamageSent = damage;
            LastDamage.DamageReceived = HealthCur - lastHealth;
            LastDamage.IsAlive = HealthCur > 0;

            if (LastDamage.IsAlive && HurtInvincibleTime > 0)
            {
                InvincibleTimer = HurtInvincibleTime;
            }

            return LastDamage;
        }

        public override State Update(TimeSpan time)
        {
            if (ActionTimer > 0)
                ActionTimer -= time.Milliseconds;
            if (InvincibleTimer > 0)
                InvincibleTimer -= time.Milliseconds;
            if (InvisibleTimer > 0)
                InvisibleTimer -= time.Milliseconds;

            return base.Update(time);
        }

        public override void Reset()
        {
            HealthCur = Health;
            ActionTimer = InvisibleTimer = InvincibleTimer = 0;
            IsHealthRegenerating = false;
            ActionTarget = default;
            base.Reset();
        }

        public override Interfaces.ICopyable CopyTo(Interfaces.ICopyable target)
        {
            if (target is Character t)
            {
                t.Health = Health;
                t.HurtInvincibleTime = HurtInvincibleTime;
                t.HurtSound = HurtSound;
            }
            return base.CopyTo(target);
        }
    }
}
