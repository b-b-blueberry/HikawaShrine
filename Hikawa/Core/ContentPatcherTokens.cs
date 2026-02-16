using StardewValley.GameData.Characters;
using StardewValley.GameData.Locations;
using StardewValley.TokenizableStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hikawa
{
    internal static class ContentPatcherTokens
    {
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        private sealed class ContentPatcherTokenAttribute(string id) : Attribute
        {
            public readonly string Id = id;
        }

        public static void RegisterAll()
        {
            foreach (var method in typeof(ContentPatcherTokens).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.GetCustomAttribute<ContentPatcherTokenAttribute>() is ContentPatcherTokenAttribute token)
                    Interfaces.Interfaces.ContentPatcherAPI.RegisterToken(ModEntry.Instance.ModManifest, token.Id, method.CreateDelegate<Func<IEnumerable<string>>>());
            }
        }

        [ContentPatcherTokenAttribute("SeasonalOutfits")]
        private static IEnumerable<string> SeasonalOutfits()
        {
            return [ModEntry.Config.SeasonalOutfits.ToString()];
        }

        [ContentPatcherTokenAttribute("WeddingLocations")]
        private static IEnumerable<string> WeddingLocations()
        {
            if (ModEntry.ModData is null)
                yield break;

            yield return "Town";
            yield return ModEntry.ModData.MapShrine;
            if (Game1.player.locationsVisited.Contains(ModEntry.ModData.MapIsland))
                yield return ModEntry.ModData.MapIsland;
        }

        [ContentPatcherTokenAttribute("WeddingLocationsResponses")]
        private static IEnumerable<string> WeddingLocationsResponses()
        {
            if (ModEntry.ModData is null)
                yield break;

            foreach (var id in WeddingLocations())
                yield return $"#$r {id} 0 wedding_who#{(Game1.locationData.TryGetValue(id, out LocationData data) ? TokenParser.ParseText(data.DisplayName) : id)}"; // TODO: add flag/ct
        }

        [ContentPatcherTokenAttribute("WeddingOfficiantsResponses")]
        private static IEnumerable<string> WeddingOfficiantsResponses()
        {
            if (ModEntry.ModData is null)
                yield break;

            foreach (var id in new[] { "Lewis", ModEntry.ModData.NpcRei, ModEntry.ModData.NpcGramps })
                yield return $"#$r {id} 0 wedding_how#{(Game1.characterData.TryGetValue(id, out CharacterData data) ? TokenParser.ParseText(data.DisplayName) : id)}"; // TODO: add flag/ct
        }

        [ContentPatcherTokenAttribute("WeddingStyleResponses")]
        private static IEnumerable<string> WeddingStyleResponses()
        {
            if (ModEntry.ModData is null)
                yield break;

            foreach (var id in new[] { "default", "shinto" })
                yield return $"#$r {id} 0 wedding_confirmed#{ModEntry.I18n.Get($"ui.shop.wedding.options.how.{id}")}"; // TODO: add flag/ct
        }
    }
}
