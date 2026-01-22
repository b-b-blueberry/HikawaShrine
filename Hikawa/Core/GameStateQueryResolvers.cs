using StardewValley.Delegates;
using System;
using System.Linq;
using System.Reflection;

namespace Hikawa
{
    internal static class GameStateQueryResolvers
    {
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        private sealed class GameStateQueryAttribute(string id) : Attribute
        {
            public readonly string Id = id;
        }

        public static void RegisterAll(string prefix)
        {
            foreach (var method in typeof(ItemQueryResolvers).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.GetCustomAttribute<GameStateQueryAttribute>() is GameStateQueryAttribute gameStateQuery)
                    GameStateQuery.Register($"{prefix}_{gameStateQuery.Id}", method.CreateDelegate<GameStateQueryDelegate>());
            }
        }
    }
}
