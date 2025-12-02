using StardewValley.Delegates;
using StardewValley.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using static StardewValley.Internal.ItemQueryResolver;

namespace Hikawa
{
    internal static class ItemQueryResolvers
    {
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        private sealed class ItemQueryAttribute(string id) : Attribute
        {
            public readonly string Id = id;
        }

        public static void RegisterAll(string prefix)
        {
            foreach (var method in typeof(ItemQueryResolvers).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.GetCustomAttribute<ItemQueryAttribute>() is ItemQueryAttribute itemQueryResolver)
                    ItemQueryResolver.Register($"{prefix}_{itemQueryResolver.Id}", method.CreateDelegate<ResolveItemQueryDelegate>());
            }
        }

        [ItemQueryAttribute("Geode")]
        private static IEnumerable<ItemQueryResult> Geode(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return Helpers.ErrorResult(key, arguments, logError, "must specify an item ID");
            }
            if (Utility.getTreasureFromGeode(ItemRegistry.Create(arguments)) is Item output)
            {
                return [new ItemQueryResult(output)];
            }
            return [];
        }
    }
}
