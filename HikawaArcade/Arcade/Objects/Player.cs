using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using System;
using System.Collections.Generic;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class Player : Character, Interfaces.IHandleInput
    {
        private int _energyCur = 0;
        public int EnergyCur
        {
            get => _energyCur;
            set
            {
                _energyCur = Math.Max(0, Math.Min(Energy, value));
                if (_energyCur == 0)
                    IsEnergyDepleting = false;
            }
        }
        public int Energy = 3;
        public int Lives = 3;
        public bool IsPlayerOne = true;
        public bool HasPlayerQuit = false;
        public bool IsEnergyDepleting = false;
        public List<Move> MovementDirections = new List<Move>();
        public Vector2 LastAimMotion = Vector2.Zero;

        public int RespawnTimer = 0;

        public Player()
            : base()
        {
            IsPlayerOne = true;
            Reset();
        }

        public override void Reset()
        {
            base.Reset();

            Lives = GameLivesDefault;
            EnergyCur = Energy;
            RespawnTimer = 0;
            MovementDirections.Clear();
        }

        public override DamagePacket TakeDamage(int damage)
        {
            return base.TakeDamage(damage);
        }

        public override Actor Spawn(Vector2 position)
        {
            return base.Spawn(position);
        }

        public override void Die()
        {
            base.Die();

            --Lives;
            RespawnTimer = GameDeathDelay;
            if (Lives >= 0)
            {
                Respawn();
            }
            else
            {
                RespawnTimer *= 3;
            }
        }

        internal void Respawn()
        {
            InvincibleTimer = GameInvincibleDelay;
            HealthCur = Health;
        }

        internal void AddMovementDirection(Move direction)
        {
            if (MovementDirections.Contains(direction))
                return;
            MovementDirections.Add(direction);
        }

        internal void PowerStart()
        {
            PowerBeforeActive();
        }

        private void PowerBeforeActive()
        {
            Game.ActivePowerPhase = PowerPhase.BeforeActive1;
        }

        private void PowerActive()
        {
            // Energy levels between 0 and the low-threshold will use a light special power
            Game.ActiveSpecialPower = EnergyCur >= Energy
                ? SpecialPower.Normal
                : SpecialPower.Megaton;

            Game.ActivePowerPhase = PowerPhase.Active1;
        }

        private void PowerAfterActive()
        {
            Game.ActiveSpecialPower = SpecialPower.None;
            Game.ActivePowerPhase = PowerPhase.AfterActive1;
        }

        private void PowerEnd()
        {
            Game.ActivePowerPhase = PowerPhase.None;
        }

        public override void Fire(Vector2 target)
        {
            // Mirror player sprite to face target
            if (MovementDirections.Count == 0)
                SpriteMirror = target.X < Position.X + Size
                    ? SpriteEffects.FlipHorizontally
                    : SpriteEffects.None;

            LastAimMotion = Utils.Vector.MotionTo(origin: Position, target: target);

            base.Fire(target: target);
        }

        public void HandleInput(Keys k)
        {
            bool hasMoved = false;
            if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k)
                && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, Keys.Left))
            {
                // Move left
                AddMovementDirection(Move.Left);
                hasMoved = true;
            }
            if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k)
                && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
            {
                // Move right
                AddMovementDirection(Move.Right);
                hasMoved = true;
            }
            if (!hasMoved)
            {
                switch (k)
                {
                    case Keys.Enter:
                    case Keys.Space:
                    case Keys.X:
                        // Special power trigger
                        if (Game.PowerTimer <= 0
                            && EnergyCur >= GameEnergyThresholdLow)
                        {
                            SpriteMirror = SpriteEffects.None;
                            PowerStart();
                        }
                        break;
                    case Keys.A:
                        // Move left
                        AddMovementDirection(Move.Left);
                        AnimationTimer = 0;
                        break;
                    case Keys.D:
                        // Move right
                        AddMovementDirection(Move.Right);
                        AnimationTimer = 0;
                        break;
                }
            }
        }

        public void HandleInputReleased(Keys k)
        {
            // Accept new input
            if (k != Keys.None)
            {
                const Keys keys = Keys.Down;
                if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k))
                    // Move right
                    k = Keys.D;
                else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k))
                    // Move left
                    k = Keys.A;
                else if (Game1.options.doesInputListContain(Game1.options.actionButton, k))
                    // Dodge
                    k = keys;
            }
            // Otherwise clear old input
            else
            {
                bool hasMoved = false;
                if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, k)
                    && !Game1.options.doesInputListContain(Game1.options.moveLeftButton, Keys.Left))
                {
                    if (MovementDirections.Contains(Move.Right))
                    {
                        MovementDirections.Remove(Move.Right);
                    }
                    hasMoved = true;
                }
                if (Game1.options.doesInputListContain(Game1.options.moveRightButton, k)
                    && !Game1.options.doesInputListContain(Game1.options.moveRightButton, Keys.Right))
                {
                    if (MovementDirections.Contains(Move.Left))
                    {
                        MovementDirections.Remove(Move.Left);
                    }
                    hasMoved = true;
                }
                if (!hasMoved)
                {
                    // Update inputs

                    if (k == Keys.A)
                    {
                        // Move left
                        if (!MovementDirections.Contains(Move.Left))
                            return;
                        MovementDirections.Remove(Move.Left);
                    }
                    else if (k == Keys.D)
                    {
                        // Move right
                        if (!MovementDirections.Contains(Move.Right))
                            return;
                        MovementDirections.Remove(Move.Right);
                    }
                }
            }
        }

        public virtual void HandleClick(int x, int y)
        {
        }

        public void HandleClickReleased(int x, int y)
        {
        }

        public override State Update(TimeSpan time)
        {
            if (HasPlayerQuit)
                return State.IsDead;

            // Per-quarter-second updates
            if (time.TotalMilliseconds / time.Milliseconds % (1000 / time.Milliseconds)
                == 250 / time.Milliseconds)
            {
                if (IsHealthRegenerating)
                    ++HealthCur;
                if (IsEnergyDepleting)
                    --EnergyCur;
            }

            /* Player special powers */

            // Move through the power animations and effects
            if (Game.ActiveSpecialPower != SpecialPower.None)
            {
                // Advance phases
                Game.PowerTimer += time.Milliseconds;
                if (Game.PowerTimer >= PowerPhaseDurations[Game.ActivePowerPhase])
                {
                    ++Game.ActivePowerPhase;
                }

                // Power phases
                if (Game.PowerTimer < PowerPhaseDurations[Game.ActivePowerPhase])
                { }
                else
                {
                    switch (Game.ActivePowerPhase)
                    {
                        case PowerPhase.BeforeActive1:
                            // Start power effects
                            PowerActive();
                            break;
                        case PowerPhase.Active4:
                            // End power effects
                            PowerAfterActive();
                            break;
                        case PowerPhase.AfterActive2:
                            // Return to usual game flow
                            PowerEnd();
                            break;
                    }
                }
            }

            // While the player has agency
            else if (Game.PowerTimer <= 0)
            {
                // Run down the death timer
                if (RespawnTimer > 0.0)
                    RespawnTimer -= time.Milliseconds;

                // Handle player movement
                if (MovementDirections.Count > 0)
                {
                    switch (MovementDirections[0])
                    {
                        case Move.Right:
                            SpriteMirror = SpriteEffects.None;
                            if (Position.X + Size < Width)
                                Position.X += SpeedCur;
                            else
                                Position.X = Width - Size;
                            break;
                        case Move.Left:
                            SpriteMirror = SpriteEffects.FlipHorizontally;
                            if (Position.X > 0)
                                Position.X -= SpeedCur;
                            else
                                Position.X = 0;
                            break;
                    }
                }

                AnimationTimer += time.Milliseconds;
                AnimationTimer %= PlayerRunFrames * PlayerAnimTimescale;
            }

            return State.IsAlive;
        }

        public override void Draw(SpriteBatch b, Rectangle viewport)
        {
            Vector2[] positions = new Vector2[3];
            Rectangle[] sources = new Rectangle[3];
            const int ARMS = 2;
            const int LEGS = 1;
            const int BODY = 0;

            // Draw full body action sprites
            if (Game.ActiveSpecialPower != SpecialPower.None)
            {   // Player used a special power
                // Draw power effects by type
                if (Game.ActiveSpecialPower == SpecialPower.Normal)
                {   // Player used Venus Love Shower / THRESHOLD_LOW === POWER_NORMAL
                    // . . . .
                }
                // Draw full body sprite by phase
                positions[BODY] = Position;
                sources[BODY] = new Rectangle(
                    PlayerPowerX
                        + PlayerW * (int)(PowerAnimationFrameGroup)Game.ActiveSpecialPower
                        + PlayerW * (int)Game.ActivePowerPhase,
                    PlayerFullY,
                    PlayerW,
                    PlayerFullH);
            }
            else if (Game.PowerTimer > 0)
            {   // Activated special power
                positions[BODY] = Position;
                sources[BODY] = new Rectangle(
                    PlayerSpecialX + PlayerW * (int)Game.ActivePowerPhase,
                    PlayerFullY,
                    PlayerW,
                    PlayerFullH);
            }
            else if (RespawnTimer > 0)
            {   // Player dying
                // . .. . .
            }
            // Draw full body idle sprite
            else if (ActionTimer <= 0 && MovementDirections.Count == 0)
            {   // Standing idle
                positions[BODY] = Position;
                sources[BODY] = new Rectangle(
                    PlayerX,
                    PlayerFullY,
                    PlayerW,
                    PlayerFullH);
            }
            // Draw appropriate sprite upper body
            else
            {
                if (ActionTimer > 0)
                {
                    int whichFrame = 0; // authors note: it was quicker to swap the level and below sprites 
                                        // than to fix my stupid broken logic
                    if (LastAimMotion != Vector2.Zero)
                    {
                        // Firing arms
                        if ((int)Math.Ceiling(LastAimMotion.X) == (int)SpriteMirror
                            || MovementDirections.Count > 0 && Math.Abs(LastAimMotion.Y) >= 0.9f)
                            whichFrame = 4; // Aiming backwards, also aiming upwards while running
                        else if (Math.Abs(LastAimMotion.Y) >= 0.9f) // Aiming upwards while standing
                            whichFrame = 5; // invalid index, therefore no visible arms
                        else if (LastAimMotion.Y < -0.6f) // Aiming low
                            whichFrame = 3;
                        else if (LastAimMotion.Y < -0.2f) // Aiming level
                            whichFrame = 2;
                        else if (LastAimMotion.Y > 0.2f) // Aiming below
                            whichFrame = 1;
                        positions[ARMS] = new Vector2(
                            x: Position.X + PlayerSplitWH / 2
                                * (SpriteMirror == SpriteEffects.None ? 1 : -1),
                            y: Position.Y);
                        sources[ARMS] = new Rectangle(
                            PlayerArmsX + PlayerW * whichFrame,
                            PlayerArmsY,
                            PlayerW,
                            PlayerSplitWH);
                    }
                    if (MovementDirections.Count > 0)
                    {   // Firing running torso
                        whichFrame = AnimationTimer / PlayerAnimTimescale;
                        positions[BODY] = Position;
                        sources[BODY] = new Rectangle(
                            PlayerBodyRunFireX + PlayerW * whichFrame,
                            PlayerBodyY,
                            PlayerW,
                            PlayerSplitWH);
                    }
                    else
                    {   // Firing standing torso
                        whichFrame = PlayerBodySideFireX; // Aiming sideways
                        if (Math.Abs(LastAimMotion.Y) >= 0.9f
                            || (int)Math.Ceiling(LastAimMotion.X) == (int)SpriteMirror)
                            whichFrame = PlayerBodyUpFireX; // Aiming upwards
                        positions[BODY] = Position;
                        sources[BODY] = new Rectangle(
                            whichFrame,
                            PlayerBodyY,
                            PlayerW,
                            PlayerSplitWH);
                    }
                }
                else
                {   // Running torso
                    int whichFrame = AnimationTimer / PlayerAnimTimescale;
                    positions[BODY] = Position;
                    sources[BODY] = new Rectangle(
                        PlayerBodyRunX + PlayerW * whichFrame,
                        PlayerBodyY,
                        PlayerW,
                        PlayerSplitWH);
                }
            }

            // Draw appropriate sprite legs
            if (MovementDirections.Count > 0)
            {   // Running
                int whichFrame = AnimationTimer / PlayerAnimTimescale;
                positions[LEGS] = new Vector2(
                    x: Position.X,
                    y: Position.Y + PlayerFullH - PlayerSplitWH);
                sources[LEGS] = new Rectangle(
                    PlayerLegsRunX + PlayerW * whichFrame,
                    PlayerLegsY,
                    PlayerW,
                    PlayerSplitWH);
            }
            else if (ActionTimer > 0)
            {   // Standing and firing
                if (LastAimMotion != Vector2.Zero)
                {   // Firing legs
                    int whichFrame = 0; // Aiming sideways
                    if (Math.Abs(LastAimMotion.Y) > 0.9f)
                        whichFrame = 1; // Aiming upwards
                    positions[LEGS] = new Vector2(
                        x: Position.X,
                        y: Position.Y + PlayerFullH - PlayerSplitWH);
                    sources[LEGS] = new Rectangle(
                        PlayerX + PlayerW * whichFrame,
                        PlayerLegsY,
                        PlayerW,
                        PlayerSplitWH);
                }
            }

            // Draw the player from each component sprite
            for (int i = sources.Length - 1; i >= 0; --i)
            {
                if (sources[i] != Rectangle.Empty)
                {
                    Game.Draw(
                        b: b,
                        viewport: viewport,
                        position: positions[i],
                        sourceRectangle: sources[i],
                        texture: IsPlayerOne
                            ? Game.ArcadeTexture
                            : Game.Player2Texture,
                        colour: Colour,
                        effects: SpriteMirror,
                        layerDepth: Position.Y / 10000f - i / 1000f + 1f / 1000f);
                }
            }

            // Draw the player's shadow
            Game.Draw(
                b: b,
                viewport: viewport,
                position: new Vector2(
                    x: Position.X,
                    y: Position.Y + PlayerFullH - ActorShadowRect.Height),
                sourceRectangle: ActorShadowRect,
                texture: IsPlayerOne
                    ? Game.ArcadeTexture
                    : Game.Player2Texture,
                layerDepth: Position.Y / 10000f - 5f / 1000f + 1f / 1000f);
        }
    }
}
