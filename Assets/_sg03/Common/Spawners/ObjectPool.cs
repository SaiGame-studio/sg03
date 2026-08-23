using UnityEngine;

/// <summary>
/// General-purpose spawner for every prefab that derives from <see cref="PoolObj"/>.
/// </summary>
[AddComponentMenu("SG03/Spawner/Object Pool")]
public class ObjectPool : Spawner<PoolObj>
{
    /// <summary>Spawns a pooled object while preserving its concrete component type.</summary>
    public T Spawn<T>(T prefab) where T : PoolObj
    {
        return base.Spawn(prefab) as T;
    }

    /// <summary>Spawns a pooled object at a world position while preserving its concrete component type.</summary>
    public T Spawn<T>(T prefab, Vector3 position) where T : PoolObj
    {
        return base.Spawn(prefab, position) as T;
    }
}
