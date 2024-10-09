using HikawaArcade.Arcade.Interfaces;
using static HikawaArcade.Arcade.ArcadeGame;

namespace HikawaArcade.Arcade.Objects
{
    public class CopySpawner<T> : ICopyable where T : ICopyable
    {
        public delegate T SpawnFunc();
        public SpawnFunc Spawn;
        public T Prototype;


        public CopySpawner() { }

        public CopySpawner(T prototype, SpawnFunc spawnFunction)
        {
            Prototype = prototype;
            Spawn = spawnFunction;
        }

        public virtual void Reset()
        {
            ((ICopyable)this).Reset();
        }

        public virtual ICopyable CopyTo(ICopyable target)
        {
            if (target is CopySpawner<T> t)
            {
                t.Prototype = Prototype;
                t.Spawn = Spawn;
            }
            return ((ICopyable)this).CopyTo(target: target);
        }
    }
}
