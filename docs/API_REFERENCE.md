# ONI Together - API Reference

This document provides an overview of the main classes and systems in the ONI Together mod.

## Core Classes

### MultiplayerMod
**File:** `MultiplayerMod.cs`

Main entry point for the mod. Handles initialization and setup.

```csharp
public class MultiplayerMod : UserMod2
{
    public override void OnLoad(Harmony harmony)
    // Called when the mod is loaded
}
```

**Key Methods:**
- `OnLoad()` - Initializes all mod systems
- `InitializeCloud()` - Sets up Google Drive integration
- `LoadAssetBundles()` - Loads custom assets

### Configuration
**File:** `Configuration.cs`

Manages mod settings and configuration.

```csharp
public class Configuration
{
    public static Configuration Instance { get; }
    public HostSettings Host { get; set; }
    public ClientSettings Client { get; set; }
}
```

**Key Properties:**
- `Host` - Server-side settings
- `Client` - Client-side settings
- `Instance` - Singleton access

## Networking System

### SteamLobby
**File:** `Networking/SteamLobby.cs`

Handles Steam lobby creation and management.

```csharp
public static class SteamLobby
{
    public static void Initialize()
    public static void CreateLobby(ELobbyType lobbyType, int maxMembers)
    public static void JoinLobby(CSteamID lobbyId)
}
```

### GameServer / GameClient
**Files:** `Networking/GameServer.cs`, `Networking/GameClient.cs`

Core networking components for host and client functionality.

### Packet System
**Directory:** `Networking/Packets/`

Handles network message serialization and processing.

**Base Classes:**
- `IPacket` - Interface for all packets
- `PacketSender` - Handles packet transmission

**Packet Categories:**
- `Core/` - Essential packets (cursor, ready status, etc.)
- `Tools/` - Tool synchronization packets
- `Social/` - Chat and social features
- `World/` - World state synchronization

## Debug System

### DebugConsole
**File:** `DebugTools/DebugConsole.cs`

In-game console for debugging and commands.

```csharp
public static class DebugConsole
{
    public static void Log(string message)
    public static void LogError(string message)
    public static void LogWarning(string message)
}
```

### DebugMenu
**File:** `DebugTools/DebugMenu.cs`

Debug menu accessible via Shift+F1.

**Features:**
- Lobby management
- Network testing
- Hierarchy viewer
- Console access

## UI Components

### ChatScreen
**File:** `Menus/ChatScreen.cs`

In-game chat system.

### MultiplayerOverlay
**File:** `Menus/MultiplayerOverlay.cs`

Main multiplayer UI overlay.

## Cloud Integration

### GoogleDrive
**File:** `Cloud/GoogleDrive.cs`

Handles large save file synchronization via Google Drive.

```csharp
public class GoogleDrive
{
    public static GoogleDrive Instance { get; }
    public void Initialize()
    public GoogleDriveUploader Uploader { get; }
    public GoogleDriveDownloader Downloader { get; }
}
```

## Harmony Patches

### Tool Patches
**Directory:** `Patches/ToolPatches/`

Synchronizes various game tools:
- `BuildToolPatch` - Building synchronization
- `DigToolPatch` - Digging synchronization
- `DeconstructToolPatch` - Deconstruction sync
- `MopToolPatch` - Mopping synchronization

### Game Patches
**Directory:** `Patches/GamePatches/`

Core game behavior modifications:
- `SaveLoaderPatch` - Save/load synchronization
- `WorldDamagePatch` - World damage events
- `PlayerControllerPatch` - Player input handling

## Utility Classes

### Utils
**File:** `Misc/Utils.cs`

General utility functions.

### SafeSerializer
**File:** `Misc/SafeSerializer.cs`

Safe serialization for network packets.

### ResourceLoader
**File:** `Misc/ResourceLoader.cs`

Loads embedded resources and asset bundles.

## Events and Callbacks

### Key Events
- `MultiplayerMod.OnPostSceneLoaded` - Fired after scene loads
- `SteamLobby.OnLobbyCreated` - Lobby creation callback
- `SteamLobby.OnLobbyEntered` - Lobby join callback

## Configuration Settings

### HostSettings
```csharp
public class HostSettings
{
    public int MaxLobbySize { get; set; } = 4;
    public int MaxMessagesPerPoll { get; set; } = 128;
    public int SaveFileTransferChunkKB { get; set; } = 256;
    public GoogleDriveSettings GoogleDrive { get; set; }
}
```

### ClientSettings
```csharp
public class ClientSettings
{
    public bool UseCustomMainMenu { get; set; } = true;
    public int MaxMessagesPerPoll { get; set; } = 16;
    public bool UseRandomPlayerColor { get; set; } = true;
    public ColorRGB PlayerColor { get; set; }
}
```

## Adding New Features

### Creating a New Packet

1. **Create packet class:**
```csharp
public class MyCustomPacket : IPacket
{
    public string Data { get; set; }
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Data);
    }
    
    public void Deserialize(BinaryReader reader)
    {
        Data = reader.ReadString();
    }
    
    public void Process()
    {
        // Handle packet logic
    }
}
```

2. **Register packet type** in the packet system

### Creating a Harmony Patch

1. **Create patch class:**
```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
public class MyPatch
{
    [HarmonyPrefix]
    public static bool Prefix(/* method parameters */)
    {
        // Pre-method logic
        return true; // Continue to original method
    }
    
    [HarmonyPostfix]
    public static void Postfix(/* method parameters */)
    {
        // Post-method logic
    }
}
```

2. **Harmony will automatically apply** the patch when the mod loads

## Best Practices

### Networking
- Always validate packet data
- Handle network errors gracefully
- Use appropriate packet types for different data
- Consider bandwidth usage

### Patches
- Minimize patch impact on game performance
- Test patches thoroughly
- Document patch behavior
- Handle edge cases

### Debugging
- Use `DebugConsole.Log()` for debugging output
- Test with debug menu features
- Monitor network traffic
- Check game logs for errors

## Common Patterns

### Singleton Access
```csharp
var config = Configuration.Instance;
var googleDrive = GoogleDrive.Instance;
```

### Packet Sending
```csharp
var packet = new MyPacket { Data = "test" };
PacketSender.SendToAll(packet);
```

### Safe Component Access
```csharp
var component = gameObject.GetComponent<MyComponent>();
if (component != null)
{
    // Use component
}
```