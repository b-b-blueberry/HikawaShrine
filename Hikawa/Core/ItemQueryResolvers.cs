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
    }
}
