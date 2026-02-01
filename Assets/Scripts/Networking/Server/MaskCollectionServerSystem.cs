using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

namespace Unity.FPSSample_2
{
    /// <summary>
    /// Server-side system that handles mask collection logic
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct MaskCollectionServerSystem : ISystem
    {
        private const float CollectionRadius = 2f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            
            // Only run on the first predicted tick to avoid duplicate collection
            if (!networkTime.IsFirstTimeFullyPredictingTick) 
                return;

            // Count total masks in the level
            int totalMasks = 0;
            foreach (var mask in SystemAPI.Query<RefRO<MaskCollectible>>())
            {
                totalMasks++;
            }

            // Update total mask count for all players
            foreach (var inventory in SystemAPI.Query<RefRW<PlayerMaskInventory>>())
            {
                inventory.ValueRW.TotalMasksInLevel = totalMasks;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Check each player for nearby masks to collect
            foreach (var (transform, inventory, entity) in 
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerMaskInventory>>()
                .WithEntityAccess())
            {
                var playerPos = transform.ValueRO.Position;

                // Check all masks in the world
                foreach (var (maskTransform, mask, maskEntity) in 
                    SystemAPI.Query<RefRO<LocalTransform>, RefRW<MaskCollectible>>()
                    .WithEntityAccess())
                {
                    // Skip already collected masks
                    if (mask.ValueRO.IsCollected) 
                        continue;

                    var maskPos = maskTransform.ValueRO.Position;
                    var distance = math.distance(playerPos, maskPos);

                    // Player is close enough to collect the mask
                    if (distance <= CollectionRadius)
                    {
                        // Mark mask as collected
                        mask.ValueRW.IsCollected = true;
                        
                        // Increment player's mask count
                        inventory.ValueRW.MaskCount++;

                        // Log the collection event
                        LogMaskCollection(mask.ValueRO.MaskType, inventory.ValueRO.MaskCount, inventory.ValueRO.TotalMasksInLevel);

                        // Destroy the mask entity
                        ecb.DestroyEntity(maskEntity);
                        
                        break; // Only collect one mask per frame per player
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstDiscard]
        private void LogMaskCollection(MaskType maskType, int currentCount, int totalCount)
        {
            Debug.Log($"[Server] Mask collected! Type: {maskType}, Count: {currentCount}/{totalCount}");
        }
    }
}
