using UnityEngine;

/// <summary>Returns a pooled world-space stat UI through the shared <see cref="ObjectPool"/>.</summary>
public class WorldSpaceStatUiDespawn : Despawn<PoolObj>
{
    protected override void Reset()
    {
        base.Reset();
        this.SetDefaultDespawnByTime();
    }

    private void SetDefaultDespawnByTime()
    {
        this.isDespawnByTime = false;
    }
}
