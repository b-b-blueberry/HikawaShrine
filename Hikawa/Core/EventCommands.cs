using Hikawa.Objects.Events;
using StardewValley.Delegates;
using System;
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
}
