using StardewValley.TokenizableStrings;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Hikawa
{
    internal static class ContentPatcherTokens
    {
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        private sealed class ContentPatcherTokenAttribute(string id) : Attribute
        {
            public readonly string Id = id;
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        private sealed class ContentPatcherAdvancedTokenAttribute(string id) : Attribute
        {
            public readonly string Id = id;
        }

        public static void RegisterAll()
        {
            foreach (var method in typeof(ContentPatcherTokens).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.GetCustomAttribute<ContentPatcherTokenAttribute>() is ContentPatcherTokenAttribute token)
                    Interfaces.Interfaces.ContentPatcherAPI.RegisterToken(ModEntry.Instance.ModManifest, token.Id, method.CreateDelegate<Func<IEnumerable<string>>>());
                if (method.GetCustomAttribute<ContentPatcherAdvancedTokenAttribute>() is ContentPatcherAdvancedTokenAttribute advancedToken)
                    Interfaces.Interfaces.ContentPatcherAPI.RegisterToken(ModEntry.Instance.ModManifest, advancedToken.Id, method.CreateDelegate<Func<object>>());
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

            foreach (var entry in ModEntry.WeddingData.Value.Locations)
                if (GameStateQuery.CheckConditions(entry.Value.Condition))
                    yield return entry.Key;
        }

        [ContentPatcherTokenAttribute("WeddingOfficiants")]
        private static IEnumerable<string> WeddingOfficiants()
        {
            if (ModEntry.ModData is null)
                yield break;

            foreach (var entry in ModEntry.WeddingData.Value.Officiants)
                if (GameStateQuery.CheckConditions(entry.Value.Condition))
                    yield return entry.Key;
        }

        [ContentPatcherTokenAttribute("WeddingDecorations")]
        private static IEnumerable<string> WeddingDecorations()
        {
            if (ModEntry.ModData is null)
                yield break;

            foreach (var entry in ModEntry.WeddingData.Value.Decorations)
                if (GameStateQuery.CheckConditions(entry.Value.Condition))
                    yield return entry.Key;
        }

        [ContentPatcherTokenAttribute("WeddingAttendeeGroups")]
        private static IEnumerable<string> WeddingAttendeeGroups()
        {
            if (ModEntry.ModData is null)
                yield break;

            foreach (var entry in ModEntry.WeddingData.Value.AttendeeGroups)
                if (GameStateQuery.CheckConditions(entry.Value.Condition))
                    yield return entry.Key;
        }

        [ContentPatcherTokenAttribute("WeddingLocationsResponses")]
        private static IEnumerable<string> WeddingLocationsResponses()
        {
            if (ModEntry.ModData is null)
                yield break;

            string next = "wedding_officiant";
            StringBuilder s = new();
            foreach (var id in WeddingLocations())
                s.Append($"#$r {ModEntry.ModData.ContentPrefix}_Wedding_Location_{id} 0 {next}#{TokenParser.ParseText(ModEntry.WeddingData.Value.Locations[id].DisplayName)}");
            yield return s.ToString();
        }

        [ContentPatcherTokenAttribute("WeddingOfficiantResponses")]
        private static IEnumerable<string> WeddingOfficiantResponses()
        {
            if (ModEntry.ModData is null)
                yield break;

            string next = "wedding_attendees";
            StringBuilder s = new();
            foreach (var id in WeddingOfficiants())
                s.Append($"#$r {ModEntry.ModData.ContentPrefix}_Wedding_Officiant_{id} 0 {next}#{TokenParser.ParseText(ModEntry.WeddingData.Value.Officiants[id].DisplayName)}");
            yield return s.ToString();
        }

        [ContentPatcherTokenAttribute("WeddingAttendeesResponses")]
        private static IEnumerable<string> WeddingAttendeesResponses()
        {
            if (ModEntry.ModData is null)
                yield break;

            string next = "wedding_decorations";
            StringBuilder s = new();
            foreach (var id in WeddingAttendeeGroups())
                s.Append($"#$r {ModEntry.ModData.ContentPrefix}_Wedding_Attendees_{id} 0 {next}#{TokenParser.ParseText(ModEntry.WeddingData.Value.AttendeeGroups[id].DisplayName)}");
            yield return s.ToString();
        }

        [ContentPatcherTokenAttribute("WeddingDecorationsResponses")]
        private static IEnumerable<string> WeddingDecorationsResponses()
        {
            if (ModEntry.ModData is null)
                yield break;

            string next = "wedding_confirmed";
            StringBuilder s = new();
            foreach (var id in WeddingDecorations())
                s.Append($"#$r {ModEntry.ModData.ContentPrefix}_Wedding_Decorations_{id} 0 {next}#{TokenParser.ParseText(ModEntry.WeddingData.Value.Decorations[id].DisplayName)}");
            yield return s.ToString();
        }

        [ContentPatcherTokenAttribute("WeddingLocation")]
        private static IEnumerable<string> WeddingLocation()
        {
            if (ModEntry.ModData is not null && Utils.GetWeddingResponse(ModEntry.ModData.ContentPrefix + "_Wedding_Location_") is string value)
                yield return value;
            else
                yield return "Town";
        }

        [ContentPatcherTokenAttribute("WeddingOfficiant")]
        private static IEnumerable<string> WeddingOfficiant()
        {
            if (ModEntry.ModData is not null && Utils.GetWeddingResponse(ModEntry.ModData.ContentPrefix + "_Wedding_Officiant_") is string value)
                yield return value;
            else
                yield return "Lewis";
        }

        [ContentPatcherTokenAttribute("WeddingAttendees")]
        private static IEnumerable<string> WeddingAttendees()
        {
            if (ModEntry.ModData is not null && Utils.GetWeddingResponse(ModEntry.ModData.ContentPrefix + "_Wedding_Attendees_") is string value)
                yield return value;
            else
                yield return "default";
        }

        [ContentPatcherTokenAttribute("WeddingStyle")]
        private static IEnumerable<string> WeddingStyle()
        {
            if (ModEntry.ModData is not null && Utils.GetWeddingResponse(ModEntry.ModData.ContentPrefix + "_Wedding_Style_") is string value)
                yield return value;
            else
                yield return "default";
        }
    }
}
