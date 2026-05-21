using SG03;
using UnityEngine;

public class Card3DDespawn : Despawn<Card3DCtrl>
{
    protected override void Reset()
    {
s        base.Reset();
        this.SetDefaultDespawnByTime();
    }

    private void SetDefaultDespawnByTime()
    {
        this.isDespawnByTime = false;
    }
}
