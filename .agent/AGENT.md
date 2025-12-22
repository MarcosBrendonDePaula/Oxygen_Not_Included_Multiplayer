# ONI Multiplayer Mod - Agent Guidelines

This document contains important context and lessons learned for AI agents working on this codebase.

## Project Overview

This is a **Multiplayer mod for Oxygen Not Included** using:
- **Harmony** for patching game code
- **Steamworks.NET** for networking
- **Unity** as the game engine

## Critical Rules

### 1. Never Serialize Unity Objects Over Network

**NEVER** attempt to serialize `UnityEngine.Object` types (or their subclasses) for network transmission. This includes:
- `MonoBehaviour`
- `Component`
- `GameObject`
- `ScriptableObject`
- Game types like `Pickupable`, `Building`, `MinionIdentity`, etc.

**Why:** Unity objects have circular references and native pointers that cause the serializer to hang indefinitely, freezing the game.

**Solution:** Always check if data is a Unity object before serializing:
```csharp
if (data != null && typeof(UnityEngine.Object).IsAssignableFrom(data.GetType()))
{
    data = null; // Skip Unity objects
}
```

### 2. Host-Only vs Client-Only Code

- **Host-side code** runs on the game host and sends packets to clients
- **Client-side code** receives packets and applies state
- Always check `MultiplayerSession.IsHost` or `MultiplayerSession.IsClient` before running sync logic
- Packet `OnDispatched()` methods should typically check `if (MultiplayerSession.IsHost) return;` to prevent running on host

### 3. Patch Timing Issues

When patching game methods that run during initialization (like `OnSpawn`), components may not be fully initialized:
- Always add null checks before accessing components
- Wrap critical sync code in try-catch blocks
- Consider delaying sync operations by a frame if components aren't ready

### 4. Debug Logging Pattern

When debugging freezes, add entry/exit logging to narrow down the exact freeze location:
```csharp
DebugConsole.Log("[ClassName] MethodName START");
// ... code ...
DebugConsole.Log("[ClassName] MethodName END");
```

If "START" appears without "END", the freeze is in that method.

### 5. Common Freeze Causes

1. **Serialization of Unity objects** - Causes infinite loops in the serializer
2. **Grid.PosToCell on destroyed objects** - Check for null and valid cells
3. **Accessing `chore.gameObject` when chore is canceling** - Always null check
4. **Animation context access on newly spawned buildings** - Wrap in try-catch

## File Structure

- `Networking/` - Core networking code (packets, senders, registry)
- `Patches/` - Harmony patches organized by game system
- `Networking/Components/` - MonoBehaviours that handle sync (DuplicantStateSender, WorldStateSyncer, etc.)
- `Misc/` - Utilities (SafeSerializer, Batchers)

## Testing Checklist

Before submitting changes that affect networking:
1. Test as **Host** - create session, perform actions
2. Test as **Client** - join session, verify sync works
3. Test **edge cases** - save/load, building fresh structures, rapid actions
