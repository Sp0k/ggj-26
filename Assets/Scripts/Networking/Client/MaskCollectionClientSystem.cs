using Unity.Entities;
using Unity.NetCode;
using Unity.Burst;
using UnityEngine;

namespace Unity.FPSSample_2
{
    /// <summary>
    /// Client-side system that detects when masks are collected and plays sound effects
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct MaskCollectionClientSystem : ISystem
    {
        private int lastKnownMaskCount;
        private bool isInitialized;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            lastKnownMaskCount = 0;
            isInitialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            // Get local player's network ID
            if (!SystemAPI.TryGetSingleton<NetworkId>(out var localNetworkId))
                return;

            // Find the local player's mask inventory
            foreach (var (inventory, ghostOwner) in 
                SystemAPI.Query<RefRO<PlayerMaskInventory>, RefRO<GhostOwner>>())
            {
                if (ghostOwner.ValueRO.NetworkId == localNetworkId.Value)
                {
                    var currentCount = inventory.ValueRO.MaskCount;

                    // Initialize on first frame
                    if (!isInitialized)
                    {
                        lastKnownMaskCount = currentCount;
                        isInitialized = true;
                        return;
                    }

                    // Check if a mask was collected
                    if (currentCount > lastKnownMaskCount)
                    {
                        // Play collection sound effect
                        PlayMaskCollectionSound();
                        
                        // Log the collection
                        LogMaskCollected(currentCount, inventory.ValueRO.TotalMasksInLevel);
                        
                        lastKnownMaskCount = currentCount;
                    }
                    
                    return;
                }
            }
        }

        [BurstDiscard]
        private void PlayMaskCollectionSound()
        {
            // TODO: Hook up to your sound system
            // Example: SoundSystem.Instance?.PlaySound(pickupSoundDef);
            // For now, just use Unity's basic audio
            // You can create a SoundDef asset and play it through the SoundSystem
        }

        [BurstDiscard]
        private void LogMaskCollected(int currentCount, int totalCount)
        {
            Debug.Log($"[Client] Mask collected! Total: {currentCount}/{totalCount}");
        }
    }
}
