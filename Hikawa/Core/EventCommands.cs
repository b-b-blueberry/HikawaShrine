using Hikawa.Objects.Events;
using StardewModdingAPI.Events;
using StardewValley.Delegates;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Reflection;

namespace Hikawa;

public static class EventCommands
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    private sealed class EventCommandAttribute(string id) : Attribute
    {
        public readonly string Id = id;
    }

    public static void RegisterAll(string prefix)
    {
        foreach (var method in typeof(EventCommands).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (method.GetCustomAttribute<EventCommandAttribute>() is EventCommandAttribute eventCommand)
            {
                Event.RegisterCommand(name: $"{prefix}_{eventCommand.Id}", action: method.CreateDelegate<EventCommandDelegate>());
            }
        }
    }

    [EventCommandAttribute("TimedQuestion")]
    private static void TimedQuestion(Event e, string[] args, EventContext context)
    {
        // youtu.be/gNIwlRClHsQ

        float ms = (float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
        float previous = e.float_useMeForAnything;

        e.float_useMeForAnything -= ms;

        if (e.float_useMeForAnything <= 0)
        {
            if (previous <= 0)
            {
                // initial setup
                if (!ArgUtility.TryGetInt(args, 1, out int duration, out string error, name: "duration"))
                {
                    e.LogCommandErrorAndSkip(args, error);
                    return;
                }

                e.float_useMeForAnything = (float)(duration);

                // make qq with args except timer duration
                var qq = e.GetCurrentCommand();
                var qqargs = qq.Split(' ').ToList();
                qqargs.RemoveAt(1);
                e.ReplaceCurrentCommand(string.Join(' ', qqargs));
                Event.DefaultCommands.QuickQuestion(e, args, context);
                // it's called qq for more reasons than one :demetriums:
            }
            else
            {
                // final cleanup
                if (Game1.activeClickableMenu is DialogueBox db)
                {
                    e.float_useMeForAnything = 0;

                    // you have failed to respond in time. so perish
                    db.closeDialogue();
                }
            }
        }
    }

    [EventCommandAttribute("ShakeTerrainFeatures")]
    private static void ShakeTerrainFeatures(Event e, string[] args, EventContext context)
    {
        // parse
        if (!ArgUtility.TryGetPoint(args, 1, out Point tile, out string error, name: "tile"))
        {
            e.LogCommandErrorAndSkip(args, $"Failed to parse shake tile: {error}");
            return;
        }
        if (!ArgUtility.TryGetOptionalFloat(args, 3, out float maxShake, out error, name: "maxShake"))
        {
            e.LogCommandErrorAndSkip(args, $"Failed to parse shake value: {error}");
            return;
        }
        if (!ArgUtility.TryGetOptional(args, 4, out string cueId, out error))
        {
            e.LogCommandErrorAndSkip(args, $"Failed to parse shake cue ID: {error}");
            return;
        }

        // apply
        if (Game1.soundBank.Exists(cueId))
        {
            context.Location.localSound(cueId, tile.ToVector2());
        }
        if (!Utils.ShakeTerrainFeature(context.Location, tile, maxShake))
        {
            e.LogCommandError(args, $"No supported terrain features found on tile {tile}");
        }

        // continue
        e.CurrentCommand++;
    }

    [EventCommandAttribute("CrystalBall")]
    private static void CrystalBall(Event e, string[] args, EventContext context)
    {
        if (e.currentCustomEventScript is not null)
        {
            if (e.currentCustomEventScript.update(context.Time, e))
            {
                e.currentCustomEventScript = null;
                e.CurrentCommand++;
            }
        }
        else
        {
            e.currentCustomEventScript = new CrystalBall();
            Game1.globalFadeToClear(afterFade: null, fadeSpeed: 0.01f);
        }
    }

    [EventCommandAttribute("Noodles")]
    private static void Noodles(Event e, string[] args, EventContext context)
    {
        float ms = (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        e.float_useMeForAnything = ms;

        e.CurrentCommand++;
        switch (e.int_useMeForAnything++)
        {
            case 0:
                e.int_useMeForAnything2 = (int)ms;
                ModEntry.Instance.Helper.Events.Display.RenderedWorld += draw;
                break;
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
                break;
            case 6:
                e.int_useMeForAnything2 = (int)ms;
                break;
            default:
                ModEntry.Instance.Helper.Events.Display.RenderedWorld -= draw;
                break;
        }

        void draw(object sender, RenderedWorldEventArgs args)
        {
            SpriteBatch b = args.SpriteBatch;

            float ms = (float)Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            float interval = MathF.PI * 1000;

            bool[] big = [false, true, true, false, false, true];
            Rectangle[][] ingredients = [
                [],
                [
                    // fishcake
                    new(0, 1184, 32, 32)
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
                ],
                [
                    // egg
                    new(32, 1184, 32, 32)
                ]
            ];

            float overlayStartMs = e.int_useMeForAnything2;
            float overlayFadeTime = 1000;
            float overlayFadeMs = Math.Min(ms - overlayStartMs, overlayFadeTime);
            float overlayFadeRatio = overlayFadeMs / overlayFadeTime;

            float spriteStartMs = e.float_useMeForAnything;
            float spriteFadeTime = 1000;
            float spriteFadeMs = Math.Min(ms - spriteStartMs, spriteFadeTime);
            float spriteFadeRatio = spriteFadeMs / spriteFadeTime;

            int stage = e.int_useMeForAnything;
            stage = Math.Clamp(stage + (spriteFadeRatio < 0.5f ? -1 : 0), 0, ingredients.Length);

            bool outro = stage >= ingredients.Length; // specifically checks for greater than length, since length fades-out the last ingredient
            float spriteAlpha = 1f - Utils.CircularFromRatio(spriteFadeRatio);
            float overlayAlphaMax = 0.5f;
            float overlayAlpha = (outro
                ? overlayAlphaMax - overlayAlphaMax * Utils.CircularFromRatio(overlayFadeRatio)
                : overlayAlphaMax * Utils.CircularFromRatio(0.5f * overlayFadeRatio));
            float scale = Game1.pixelZoom;

            // Draw overlay
            b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.PaleVioletRed * overlayAlpha);

            // Redraw actors above overlay and sprites
            Game1.player.draw(b);
            foreach (var actor in e.actors)
                actor.draw(b);

            if (stage <= 0 || stage >= ingredients.Length)
                return;

            int mult = big[stage] ? 6 : 3;
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

                    Rectangle[] sources = ingredients[stage];
                    Rectangle source = sources[(x + y) % sources.Length];
                    Vector2 position = new Vector2(w * x / w * Game1.tileSize * mult, h * y / h * Game1.tileSize * mult * 1.25f);

                    b.Draw(
                        texture: ModEntry.EventSprites,
                        position: position
                            + new Vector2(0.5f, -0.25f) * Game1.tileSize * mult
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
        }
    }

    [EventCommandAttribute("Meditate")]
    private static void Meditate(Event e, string[] args, EventContext context)
    {
        // possibly my most devilish enterprise yet
        // not as thrilling as desert bus but therein lies the charm

        float ms = (float)context.Time.ElapsedGameTime.TotalMilliseconds;
        float previous = e.float_useMeForAnything;

        // meditation requires focus
        if (Game1.game1.IsActive)
        {
            e.float_useMeForAnything -= ms;
        }

        if (e.float_useMeForAnything <= 0)
        {
            if (previous <= 0)
            {
                // initial setup

                // if you're lucky you won't have to sit there for half an hour
                double luck = Game1.player.DailyLuck / 20 + Game1.player.LuckLevel / 200;
                double minutes = 25 + 10 * (Game1.random.NextDouble() - luck);
                e.int_useMeForAnything = (int)(60000 * minutes); // max
                e.float_useMeForAnything = e.int_useMeForAnything; // current

                // may as well add the sprite here since we're doing the smoke per tick
                Vector2 tile = new Vector2(10, 5);
                var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
                    textureName: ModEntry.EventSprites.Name,
                    sourceRect: new Rectangle(176, 1184, 16, 32),
                    position: tile * Game1.tileSize,
                    flipped: false,
                    alphaFade: 0,
                    color: Color.White);
                sprite.scale = Game1.pixelZoom;
                sprite.holdLastFrame = true;
                context.Location.TemporarySprites.Add(sprite);
            }
            else
            {
                // final cleanup
                e.int_useMeForAnything = 0;
                e.float_useMeForAnything = 0;
                e.CurrentCommand++;
            }
        }
        else
        {
            // embers
            var ratio = 1 - (e.float_useMeForAnything / e.int_useMeForAnything);
            if ((context.Time.TotalGameTime.Ticks % (int)(30 + e.int_useMeForAnything / 20000 * ratio)) == 0)
            {
                Vector2 tile = new Vector2(10, 5) + new Vector2(0.5f);
                var sprite = TemporaryAnimatedSprite.GetTemporaryAnimatedSprite(
                    textureName: null,
                    sourceRect: new Rectangle(0, 0, 1, 1),
                    position: tile * Game1.tileSize,
                    flipped: false,
                    alphaFade: 0.0015f,
                    color: Color.LightGray);
                sprite.alpha = 0.4f + (float)Game1.random.NextDouble() * 0.4f;
                sprite.motion = new Vector2(0f, -0.2f);
                sprite.acceleration = new Vector2(0.0015f, 0f);
                sprite.interval = 99999f;
                sprite.xPeriodic = true;
                sprite.xPeriodicLoopTime = 2000f;
                sprite.xPeriodicRange = Game1.tileSize / 8 + (float)(Game1.random.NextDouble() * Game1.tileSize / 24);
                sprite.layerDepth = 1f;
                sprite.scale = Game1.pixelZoom;
                sprite.scaleChange = -0.015f;
                sprite.texture = Game1.staminaRect;
                context.Location.TemporarySprites.Add(sprite);
            }
        }
    }
}
