using System;
using System.Collections.Generic;
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
    }
}
