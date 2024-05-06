using UnityEngine;

namespace DCFApixels.ObjectPools
{
    public interface IObjectPool
    {
        public bool IsCanTake { get; }

        public T TakeAggressive<T>() where T : Component;
        public ObjectPoolUnit TakeAggressive();

        public bool TryTake<T>(out T instance) where T : Component;
        public bool TryTake(out ObjectPoolUnit instance);

        public T Take<T>() where T : Component;
        public ObjectPoolUnit Take();

        public void Return(ObjectPoolUnit unit);

        public void Destroy();
    }
}


