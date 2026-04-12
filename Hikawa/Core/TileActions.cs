using Hikawa.Objects.Locations;
using Hikawa.Objects.Menus;
using System;
using System.Reflection;

namespace Hikawa;

public static class TileActions
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    private sealed class TileActionAttribute(string id) : Attribute
    {
        public readonly string Id = id;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    private sealed class TouchActionAttribute(string id) : Attribute
    {
        public readonly string Id = id;
    }

    public static void RegisterAll(string prefix)
    {
        foreach (var method in typeof(TileActions).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (method.GetCustomAttribute<TileActionAttribute>() is TileActionAttribute tileAction)
                GameLocation.RegisterTileAction(key: $"{prefix}_{tileAction.Id}", action: method.CreateDelegate<Func<GameLocation, string[], Farmer, Point, bool>>());
            else if (method.GetCustomAttribute<TouchActionAttribute>() is TouchActionAttribute touchAction)
                GameLocation.RegisterTouchAction(key: $"{prefix}_{touchAction.Id}", action: method.CreateDelegate<Action<GameLocation, string[], Farmer, Vector2>>());
        }
    }

    [TileActionAttribute("Shrine_Shop")]
    private static bool Action_Shrine_Shop(GameLocation where, string[] args, Farmer who, Point tile)
    {
        // Using the Shrine souvenir shop
        if (where is Shrine shrine && shrine.GetShopPerson() is NPC npc)
        {
            var dialogue = npc.TryGetDialogue("shop_main");
            npc.setNewDialogue(dialogue, add: true, clearOnMovement: true);
            Game1.drawDialogue(npc);
            return true;
        }

        return false;
    }

    [TileActionAttribute("Shrine_Offering")]
    private static bool Action_Shrine_Offering(GameLocation where, string[] args, Farmer who, Point tile)
    {
        // Using the Shrine offertory box
        Utils.CreateInspectThenQuestionDialogue(
            [
                ModEntry.I18n.Get("world.shrine.offer.inspect"),
                ModEntry.I18n.Get($"world.shrine.offer.prompt")
            ],
            [
                new Response("offer_yes", ModEntry.I18n.Get("ui.menu.yes")),
                new Response("offer_no", ModEntry.I18n.Get("ui.menu.no"))
            ]);

        return true;
    }

    [TileActionAttribute("Shrine_CrowTrade")]
    private static bool Action_CrowTrade(GameLocation where, string[] args, Farmer who, Point tile)
    {
        // Interactions with the crow trade tile at the Shrine
        return where is Shrine shrine && shrine.HandleCrowTradeAction(who);
    }

    [TileActionAttribute("Shrine_Ema")]
    private static bool Action_Shrine_Ema(GameLocation where, string[] args, Farmer who, Point tile)
    {
        // Interactions with the Ema stand at the Shrine
        Game1.activeClickableMenu = new EmaMenu();
        return true;
    }

    [TileActionAttribute("Shrine_Hall")]
    private static bool Action_Shrine_Hall(GameLocation where, string[] args, Farmer who, Point tile)
    {
        // Trying to enter the Shrine Hall front doors
        return true;
    }

    [TileActionAttribute("House_Lockbox")]
    private static bool Action_House_Lockbox(GameLocation where, string[] args, Farmer who, Point tile)
    {
        // Lockbox
        return true;
    }

    [TileActionAttribute("House_Wardrobe")]
    private static bool Action_House_Wardrobe(GameLocation where, string[] args, Farmer who, Point tile)
    {
        // Wardrobe
        // Offer to toggle seasonal outfits on Hikawa characters
        Game1.playSound("doorCreak");
        Game1.freezeControls = true;
        Game1.delayedActions.Add(new DelayedAction(300, () =>
        {
            Game1.freezeControls = false;
            Utils.CreateInspectThenQuestionDialogue(
                [
                    ModEntry.I18n.Get("world.house.wardrobe", new {season = Game1.CurrentSeasonDisplayName}),
                    ModEntry.I18n.Get($"world.house.wardrobe.{(false ? "disable" : "enable")}")
                ],
                [
                    new Response("wardrobe_yes", ModEntry.I18n.Get("ui.menu.yes")),
                    new Response("wardrobe_no", ModEntry.I18n.Get("ui.menu.no"))
                ]);
        }));
        return true;
    }

    [TileActionAttribute("Vortex")]
    private static bool Action_Vortex(GameLocation where, string[] args, Farmer who, Point tile)
    {
        // Vortex warps
        if (where is Vortex && args.Length > 2 && int.TryParse(args[1], out int toX) && int.TryParse(args[2], out int toY))
        {
            Point toTile = new Point(toX, toY);
            string toLocation = args.Length > 3 ? args[3] : null;
            Vortex.TouchVortexWarp(tile, toTile, toLocation);
            return true;
        }
        return false;
    }


    [TouchActionAttribute("Sound")]
    private static void TouchAction_Sound(GameLocation where, string[] args, Farmer who, Vector2 tile)
    {
        if (args.Length > 1 && args[1] is string cueId && Game1.soundBank.Exists(cueId))
            Game1.playSound(cueId);
    }

    [TouchActionAttribute("ShakeTerrainFeatures")]
    private static void TouchAction_ShakeTerrainFeatures(GameLocation where, string[] args, Farmer who, Vector2 tile)
    {
        if (!ArgUtility.TryGetFloat(args, 1, out float maxShake, out string error))
        {
            throw new Exception($"Failed to parse shake value: {error}");
        }
        if (!ArgUtility.TryGetOptional(args, 2, out string cueId, out error))
        {
            throw new Exception($"Failed to parse shake cue ID: {error}");
        }

        if (Game1.soundBank.Exists(cueId))
        {
            where.localSound(cueId, tile);
        }
        foreach (var other in Utility.getAdjacentTileLocations(tile))
        {
            Utils.ShakeTerrainFeature(where, other.ToPoint(), maxShake);
        }
    }

    [TouchActionAttribute("Hop")]
    private static void TouchAction_Hop(GameLocation where, string[] args, Farmer who, Vector2 tile)
    {
        // Don't allow for triggering other Hop tiles while already hopping
        if (Game1.player.freezePause > 0)
            return;

        const int argsToSkip = 1; // First element is the action name, unused
        const int argsLength = 4; // Each hop parses 4 elements in args before continuing
        void hop(int argsIndex, Vector2 fromPosition)
        {
            Vector2 toTile = Vector2.Zero;
            if (float.TryParse(args[argsIndex + 0], out toTile.X)
                && float.TryParse(args[argsIndex + 1], out toTile.Y)
                && int.TryParse(args[argsIndex + 2], out int facingDirection))
            {
                // Behaviour on hop started:

                const int duration = 350;
                Vector2 toPosition = toTile * Game1.tileSize;

                // Play starting sound cue
                Utils.TryPlaySound(cueName: args[argsIndex + 3]);

                // Play dust-puff effect
                TemporaryAnimatedSprite puff = new(
                    textureName: "TileSheets/animations",
                    sourceRect: new Rectangle(0, 320, 64, 64),
                    animationInterval: 50f,
                    animationLength: 8,
                    numberOfLoops: 0,
                    position: new Vector2(
                        x: fromPosition.X - fromPosition.X % Game1.tileSize + 16,
                        y: fromPosition.Y - fromPosition.Y % Game1.tileSize + 16),
                    flicker: false,
                    flipped: false)
                {
                    scale = 0.5f,
                    alpha = 0.95f,
                    alphaFade = 0.01f
                };
                Game1.player.currentLocation.TemporarySprites.Add(puff);

                // StardewValley.Farmer.cs:BeginSitting
                // Stop player animations and hop to the target position
                Game1.player.Halt();
                Game1.player.synchronizedJump(4f);
                Game1.player.FarmerSprite.StopAnimation();
                Game1.player.LerpPosition(
                    start_position: Game1.player.Position,
                    end_position: toPosition,
                    duration: duration / 1000f);

                Game1.player.FarmerSprite.setCurrentAnimation(animation:
                [
                    new FarmerSprite.AnimationFrame(
                        frame: new[]{ FarmerSprite.walkUp, FarmerSprite.walkRight, FarmerSprite.walkDown, FarmerSprite.walkRight }[facingDirection],
                        milliseconds: duration,
                        secondaryArm: false,
                        flip: facingDirection == Game1.left,
                        frameBehavior: (Farmer who) =>
                        {
						    // Behaviour on hop completed:

						    // Required for ending hop-animation
						    Game1.player.Halt();
                            Game1.player.FarmerSprite.StopAnimation();
                            Game1.player.completelyStopAnimatingOrDoingAction();

							// Check progress in hop-chain given in args
							int nextIndex = argsIndex + argsLength;
                            if (args.Length >= nextIndex + argsLength)
                            {
								// Continue to next point in hop-chain
								hop(argsIndex: nextIndex, fromPosition: Game1.player.Position);
                            }
                            else
                            {
								// At end of hop-chain:

								// Match player facing-direction to last hop's direction
								Game1.player.FacingDirection = facingDirection;

								// Play landing sound cue
								Game1.player.checkForFootstep();
                            }
                        },
                        behaviorAtEndOfFrame: true)
                ]);
                // Required for holding hop-animation until complete
                Game1.player.FarmerSprite.PauseForSingleAnimation = true;
            }
        }
        if (args.Length - argsToSkip >= argsLength)
        {
            hop(argsIndex: argsToSkip, fromPosition: Game1.player.Position);
        }
    }
}
