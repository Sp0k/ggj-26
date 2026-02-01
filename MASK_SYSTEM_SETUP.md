# Mask Collection System Setup Guide

## Overview
This system tracks mask collection on the server and replicates the count to all clients for display in the UI. It uses Unity's NetCode for multiplayer synchronization.

## Files Created/Modified

### New Files:
1. **Assets/Scripts/Masks/MaskCollectible.cs**
   - Component for collectible masks in the world
   - Authoring component to place masks in scenes
   
2. **Assets/Scripts/Networking/Server/MaskCollectionServerSystem.cs**
   - Server-side system that handles mask collection logic
   - Checks distance between players and masks
   - Updates PlayerMaskInventory when masks are collected

3. **Assets/Scripts/Networking/Client/MaskCollectionClientSystem.cs**
   - Client-side system that detects mask collection
   - Plays sound effects (placeholder for integration with your sound system)

### Modified Files:
1. **Assets/Scripts/Networking/Server/ServerGameData.cs**
   - Already contains `PlayerMaskInventory` component (no changes needed)

2. **Assets/Scripts/Networking/Server/ServerGameSystem.cs**
   - Added initialization of `PlayerMaskInventory` when spawning players

3. **Assets/Scripts/Gameplay/UI/InGameHUD.cs**
   - Added `m_MaskCountLabel` field
   - Added `UpdateMaskCounter()` method
   - Integrated mask counter display into UI update loop

## Setup Instructions

### 1. Create Mask Prefabs

1. In Unity, create a new GameObject (e.g., 3D Cube or custom mesh)
2. Add the **MaskCollectibleAuthoring** component
3. In the inspector, set the **Mask Type** (Gold, Blue, Silver, Bronze)
4. Add a Collider component (optional, for visual reference)
5. Save as a prefab in your Prefabs folder

### 2. Place Masks in Your Level

1. Open your game scene
2. Drag mask prefabs into the scene
3. Position them where players should find them
4. Each mask will be converted to an Entity with the `MaskCollectible` component

### 3. Setup UI (UI Toolkit)

You need to add a Label element to your UI Toolkit UXML file:

1. Find your UI Document asset (likely in `Assets/UI Toolkit/`)
2. Open it in the UI Builder
3. Add a new **Label** element
4. Set its **Name** property to: `mask-count-label`
5. Position and style it (recommended: top-right corner)
6. Example styling:
   - Font Size: 24-32
   - Color: White
   - Text Shadow or Outline for visibility
   - Position: Absolute (top-right)

### 4. Configure Collection Radius (Optional)

In `MaskCollectionServerSystem.cs`, you can adjust the collection radius:

```csharp
private const float CollectionRadius = 2f; // Change this value
```

### 5. Add Sound Effects (Optional)

To integrate sound effects:

1. Create a SoundDef asset for the pickup sound
2. In `MaskCollectionClientSystem.cs`, update the `PlayMaskCollectionSound()` method:

```csharp
[BurstDiscard]
private void PlayMaskCollectionSound()
{
    // Replace with your actual sound system call
    var soundSystem = Object.FindObjectOfType<SoundSystem>();
    if (soundSystem != null)
    {
        // Play your pickup sound
        // soundSystem.CreateEmitter(yourPickupSoundDef, position);
    }
}
```

## How It Works

### Server Flow:
1. `MaskCollectionServerSystem` runs every frame on the server
2. Checks distance between each player and uncollected masks
3. When distance ≤ CollectionRadius:
   - Marks mask as collected
   - Increments player's `MaskCount`
   - Destroys the mask entity
   - Logs the event

### Client Flow:
1. NetCode replicates `PlayerMaskInventory` from server to clients (via `[GhostField]`)
2. `InGameHUD.UpdateMaskCounter()` reads the local player's inventory
3. Updates the UI Label with "Masks: X/Y"
4. `MaskCollectionClientSystem` detects count changes and plays sound effects

## Testing

1. **Play in Editor**:
   - Start a server and client
   - Move player near masks
   - Watch the counter update in the UI

2. **Debug Logs**:
   - Server: `[Server] Mask collected! Type: Bronze, Count: 1/5`
   - Client: `[Client] Mask collected! Total: 1/5`

3. **Common Issues**:
   - **UI not showing**: Check that your Label is named exactly `mask-count-label`
   - **Masks not collecting**: Increase `CollectionRadius` or check mask placement
   - **Count not syncing**: Verify `PlayerMaskInventory` has `[GhostField]` attributes

## Customization

### Different Mask Values
Masks use the existing `MaskDatabase` system:
- Gold: 100 points
- Blue: 50 points
- Silver: 30 points
- Bronze: 10 points

You can modify these in `Assets/Scripts/Masks/MaskDatabase.cs`

### Per-Player vs Team Collection
Currently, each player has their own mask count. To make it team-based, you would:
1. Add a team component
2. Update masks on all team members in `MaskCollectionServerSystem`

### Persistent Collection (Masks Don't Respawn)
Currently implemented - masks are destroyed when collected. To make them respawn:
1. Don't destroy the entity
2. Add a respawn timer component
3. Create a respawn system

## Integration Checklist

- [ ] Created mask prefabs with `MaskCollectibleAuthoring`
- [ ] Placed masks in game scene
- [ ] Added `mask-count-label` to UI Toolkit document
- [ ] Styled the mask counter label
- [ ] Tested in multiplayer (server + client)
- [ ] (Optional) Added sound effect integration
- [ ] (Optional) Adjusted collection radius
- [ ] (Optional) Customized mask values

## Architecture Notes

- **Server Authority**: All collection logic runs on server only
- **Automatic Replication**: `PlayerMaskInventory` uses NetCode's ghost system
- **ECS Systems**: Uses Unity DOTS for performance
- **UI Toolkit**: Modern UI system for clean, resolution-independent UI
