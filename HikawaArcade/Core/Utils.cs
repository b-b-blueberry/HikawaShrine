using Microsoft.Xna.Framework;
using System;

namespace HikawaArcade
{
    internal static class Utils
    {
        #region Vector operations

        internal static class Vector
        {
            public static Vector2 PointAt(Vector2 origin, Vector2 target)
            {
                return target - origin;
            }

            public static float RadiansBetween(Vector2 va, Vector2 vb)
            {
                return (float)Math.Atan2(vb.Y - va.Y, vb.X - va.X);
            }

            public static Vector2 MotionTo(Vector2 origin, Vector2 target)
            {
                Vector2 motion = Utils.Vector.PointAt(origin: origin, target: target);
                motion.Normalize();
                return motion;
            }
        }

        #endregion

    }
}
