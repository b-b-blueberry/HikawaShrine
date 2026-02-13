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

        [GameStateQueryAttribute("BugsCaught")]
        private static bool BugsCaught(string[] args, GameStateQueryContext context)
        {
            int min = ArgUtility.GetInt(args, 0, 0);
            int max = ArgUtility.GetInt(args, 1, int.MaxValue);
            int count = ModEntry.SaveData.BugCollection.Select(bug => bug.Value.Count).Sum();
            return count >= min && count <= max;
        }

        [GameStateQueryAttribute("BugTypesCaught")]
        private static bool BugTypesCaught(string[] args, GameStateQueryContext context)
        {
            int min = ArgUtility.GetInt(args, 0, 0);
            int max = ArgUtility.GetInt(args, 1, int.MaxValue);
            int count = ModEntry.SaveData.BugCollection.Select(bug => bug.Value.Count > 0).Count();
            return count >= min && count <= max;
        }

        [GameStateQueryAttribute("BugCollectionProgress")]
        private static bool BugCollectionProgress(string[] args, GameStateQueryContext context)
        {
            float min = ArgUtility.GetFloat(args, 0, 0);
            float max = ArgUtility.GetFloat(args, 1, 1);
            float progress = ModEntry.SaveData.BugCollection.Select(bug => bug.Value.Count > 0).Count() / ModEntry.BugsData.Value.BugData.Count;
            return progress >= min && progress <= max;
        }

        [GameStateQueryAttribute("Mirage")]
        private static bool Mirage(string[] args, GameStateQueryContext context)
        {
            return Utils.IsMirageDay();
        }
    }
}
