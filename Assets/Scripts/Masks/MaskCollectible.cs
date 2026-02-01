using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

namespace Unity.FPSSample_2
{
    /// <summary>
    /// Component for mask collectibles in the world
    /// </summary>
    public struct MaskCollectible : IComponentData
    {
        public MaskType MaskType;
        public bool IsCollected;
        public int Points;
    }

    /// <summary>
    /// Authoring component for mask collectibles
    /// </summary>
    public class MaskCollectibleAuthoring : MonoBehaviour
    {
        [Header("Mask Configuration")]
        public MaskType maskType = MaskType.Bronze;

        class Baker : Baker<MaskCollectibleAuthoring>
        {
            public override void Bake(MaskCollectibleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                var maskData = MaskDatabase.Get(authoring.maskType);
                
                AddComponent(entity, new MaskCollectible
                {
                    MaskType = authoring.maskType,
                    IsCollected = false,
                    Points = maskData.Points
                });
            }
        }
    }
}
