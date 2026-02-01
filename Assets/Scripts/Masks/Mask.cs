using UnityEngine;
using System.Collections.Generic;

// namespace Mask {
public class Mask
{
    public MaskType Type { get; private set; }
    
    public string Name => MaskDatabase.Get(Type).Name;
    public int Points => MaskDatabase.Get(Type).Points;
    public float SpawnRate => MaskDatabase.Get(Type).SpawnRate;

    public Mask(MaskType type)
    {
        Type = type;
    }

    // Factory method for common creation pattern
    public static Mask Create(MaskType type) => new Mask(type);
}
// }