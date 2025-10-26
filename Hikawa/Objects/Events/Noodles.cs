using System;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Hikawa.Objects.Events
{
    public class Noodles : ICustomEventScript
    {
        readonly NPC Rei;
        int Stage;
        float Timer;
        float Alpha;
        float AlphaChange;
        bool CanQuit;

        public Noodles()
        {
            Rei = Game1.CurrentEvent.getActorByName(ModEntry.ModData.NpcRei);
        }

        public void draw(SpriteBatch b)
        {
        }

        public void drawAboveAlwaysFront(SpriteBatch b)
        {
            float ms = (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            float interval = MathF.PI * 1000;

            float spriteAlpha = 1f * this.Alpha;
            float overlayAlpha = 0.5f * this.Alpha;
            float scale = Game1.pixelZoom;

            bool[] big = [true, true, true, false, false];
            Rectangle[][] ingredients = [
                [
                    // fishcake
                    new(0, 1184, 32, 32)
                ],
                [
                    // egg
                    new(32, 1184, 32, 32)
                ],
                [
                    // seaweed
                    new(64, 1184, 32, 32)
                ],
                [
                    // spring onions
                    new(96, 1184, 16, 16), new(96, 1200, 16, 16), new(112, 1184, 16, 16)
                ],
                [
                    // frozen veg
                    new(112, 1200, 16, 16), new(128, 1184, 16, 16), new(128, 1200, 16, 16), new(128, 1200, 16, 16), new(144, 1200, 16, 16), new(144, 1200, 16, 16), new(160, 1200, 16, 16), new(160, 1200, 16, 16)
                ]
            ];

            // Draw overlay
            b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.OrangeRed * overlayAlpha);

            if (this.Stage < 0 || this.Stage >= ingredients.Length)
                return;

            int mult = big[this.Stage] ? 6 : 3;
            int w = Game1.viewport.Width;
            int h = Game1.viewport.Height;
            int countX = w / Game1.tileSize / mult;
            int countY = h / Game1.tileSize / mult + 1;

            // Draw ingredient sprites
            for (int x = 0; x < countX; ++x)
            {
                for (int y = 0; y < countY; ++y)
                {
                    float ratio = ((ms + (x + y) * 1000) % interval) / interval;
                    float circular = Utils.SharpCircularFromRatio(ratio);

                    Rectangle[] sources = ingredients[this.Stage];
                    Rectangle source = sources[(x + y) % sources.Length];
                    Vector2 position = new Vector2(w * x / w * Game1.tileSize * mult, h * y / h * Game1.tileSize * mult * 1.25f);

                    b.Draw(
                        texture: ModEntry.EventSprites,
                        position: position
                            + new Vector2(0.5f, -0.5f) * Game1.tileSize * mult
                            + new Vector2(0f, ratio * Game1.tileSize * mult * 0.75f),
                        sourceRectangle: source,
                        color: Color.White
                            * spriteAlpha
                            * circular,
                        rotation: MathF.PI * 2 * ratio,
                        origin: source.Size.ToVector2() / 2,
                        scale: scale,
                        effects: SpriteEffects.None,
                        layerDepth: 1);
                }
            }

            // Redraw actors above overlay and sprites
            foreach (var actor in Game1.CurrentEvent.actors)
                actor.draw(b);
            foreach (var actor in Game1.CurrentEvent.farmerActors)
                actor.draw(b);
        }

        public bool update(GameTime time, Event e)
        {
            float ms = (float)time.ElapsedGameTime.TotalMilliseconds;

            this.Timer = Math.Max(this.Timer - ms, 0);
            this.Alpha = Math.Clamp(this.Alpha + this.AlphaChange * ms, 0, 1);

            if (e.spriteTextToDraw != null)
            {
                e.int_useMeForAnything2 = 1;
                e.float_useMeForAnything += ms;

                if (e.float_useMeForAnything > 80f)
                {
                    if (e.int_useMeForAnything >= e.spriteTextToDraw.Length)
                    {
                        if (e.float_useMeForAnything >= 2500)
                        {
                            e.int_useMeForAnything = 0;
                            e.float_useMeForAnything = 0;
                            e.spriteTextToDraw = "";
                            e.spriteTextToDraw = null;
                        }
                    }
                    else
                    {
                        e.int_useMeForAnything++;
                        e.float_useMeForAnything = 0f;
                    }
                }
            }

            switch (this.Stage)
            {
                case 0:
                    this.AlphaChange = 0.001f;
                    if (this.Alpha >= 1)
                    {
                        e.spriteTextToDraw = "fishcake...";
                        this.Timer = 3000;
                        ++this.Stage;
                    }
                    break;
                case 1:
                    this.AlphaChange = 0;
                    if (this.Timer <= 0)
                    {
                        e.spriteTextToDraw = "egg...";
                        this.Timer = 3000;
                        ++this.Stage;
                    }
                    break;
                case 2:
                    this.AlphaChange = 0;
                    if (this.Timer <= 0)
                    {
                        e.spriteTextToDraw = "seaweed...";
                        this.Timer = 3000;
                        ++this.Stage;
                    }
                    break;
                case 3:
                    this.AlphaChange = 0;
                    if (this.Timer <= 0)
                    {
                        e.spriteTextToDraw = "sprongions...";
                        this.Timer = 3000;
                        ++this.Stage;
                    }
                    break;
                case 4:
                    this.AlphaChange = 0;
                    if (this.Timer <= 0)
                    {
                        e.spriteTextToDraw = "fregetables...";
                        this.Timer = 3000;
                        ++this.Stage;
                    }
                    break;
                case 5:
                    this.AlphaChange = -0.001f;
                    if (this.Alpha == 0)
                    {
                        e.spriteTextToDraw = null;
                        ++this.Stage;
                    }
                    break;
                default:
                    this.CanQuit = true;
                    break;
            }

            return this.CanQuit;
        }
    }
}
