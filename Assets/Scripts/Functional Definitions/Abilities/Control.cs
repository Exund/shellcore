using System.Collections.Generic;
using UnityEngine;

public class Control : PassiveAbility
{
    const float healthAddition = 200;
    public const float damageAddition = 50;
    public const float baseControlFractionBoost = 0.2F;
    List<Entity> boosted = new List<Entity>();

    protected override void Awake()
    {
        base.Awake();
        ID = AbilityID.Control;
    }

    public override void Deactivate()
    {
        foreach (var entity in boosted)
        {
            if (!entity)
            {
                continue;
            }

            entity.ControlStacks -= abilityTier;
        }

        base.Deactivate();
        Entity.OnEntitySpawn -= EntitySpawn;
    }

    protected override void Execute()
    {
        foreach (var entity in AIData.entities)
        {
            if (!entity.GetIsDead())
            {
                Enhance(entity);
            }
        }

        Entity.OnEntitySpawn += EntitySpawn;
    }

    void EntitySpawn(Entity entity)
    {
        if (!entity.GetIsDead())
        {
            Enhance(entity);
        }
    }

    void Enhance(Entity entity)
    {
        if (entity.faction == Core.faction && entity != Core && !boosted.Contains(entity))
        {
            entity.ControlStacks += abilityTier;
            boosted.Add(entity);
        }
    }
}
