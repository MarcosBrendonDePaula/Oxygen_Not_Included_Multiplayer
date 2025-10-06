# ONI Together Workflow Documentation

This document describes the complete workflow of the ONI Together mod, from initialization to action synchronization between players.

## Overview

ONI Together is a multiplayer mod for Oxygen Not Included that enables multiple players to collaborate in real-time on the same world. The mod uses Steam API for networking, Google Drive for large save file sharing, and Harmony patches to intercept and synchronize game actions.

## System Architecture

### Core Components

1. **MultiplayerMod** - Main mod entry point
2. **SteamLobby** - Steam lobby management
3. **GameServer/GameClient** - Network communication
4. **PacketSystem** - Message system between players
5. **GoogleDrive** - Save file synchronization
6. **Harmony Patches** - Game action interception

## Initialization Flow

### 1. Mod Loading (`MultiplayerMod.OnLoad`)

```
Mod Initialization
├── DebugMenu Initialization
├── SteamLobby Initialization
├── GoogleDrive Initialization
├── "Multiplayer_Modules" GameObject Creation
│   ├── SteamNetworkingComponent
│   ├── UIVisibilityController
│   ├── MainThreadExecutor
│   └── CursorManager
├── Listeners Setup
├── AssetBundles Loading
└── Harmony Patches Registration
```

### 2. Component Configuration

- **SteamNetworkingComponent**: Manages Steam connections
- **CursorManager**: Controls other players' cursors
- **MainThreadExecutor**: Executes operations on main thread
- **UIVisibilityController**: Controls UI element visibility

### 3. Google Drive Initialization

- Loads credentials from `credentials.json` file
- Authenticates with OAuth2
- Sets up uploader and downloader for large saves

## Lobby Creation Flow

### Host (Lobby Creator)

```
Lobby Creation
├── Steam initialization verification
├── Google Drive initialization verification
├── SteamMatchmaking.CreateLobby() call
├── Lobby metadata configuration
├── GameServer initialization
├── Rich Presence setup
└── Cursor color update
```

### Client (Participant)

```
Lobby Entry
├── Invite reception or lobby search
├── SteamMatchmaking.JoinLobby() call
├── MultiplayerSession host configuration
├── Automatic host connection via GameClient
└── Rich Presence update
```

## Networking System

### Client-Server Architecture

The mod uses a client-server architecture where:
- **Host**: Acts as authoritative server
- **Clients**: Connect to host via Steam P2P

### Packet Types

```
PacketTypes
├── Core
│   ├── ChatMessage
│   ├── PlayerCursor
│   └── ClientReadyStatus
├── Tools
│   ├── Build/BuildComplete
│   ├── Dig/DigComplete
│   ├── Cancel
│   ├── Deconstruct
│   └── Prioritize
├── World
│   ├── WorldData
│   ├── WorldUpdate
│   └── WorldCycle
└── Cloud
    └── GoogleDriveFileShare
```

### Packet Sending Flow

```
Player Action
├── Interception via Harmony Patch
├── Corresponding packet creation
├── Packet serialization
├── Sending via Steam Networking
│   ├── Host → All Clients
│   └── Client → Host
└── Deserialization and execution at destination
```

## Action Synchronization

### Building Tools

1. **BuildTool** (`BuildToolPatch`)
   - Intercepts construction attempts
   - Creates `BuildPacket` with construction data
   - Sends to other players
   - Executes construction locally

2. **DigTool** (`DigToolPatch`)
   - Intercepts dig markings
   - Creates `DiggablePacket`
   - Synchronizes cells marked for digging

3. **SelectTool** (`SelectToolPatch`)
   - Updates cursor color based on player
   - Applies custom tint to cursor

### Synchronization Flow

```
Local Action
├── Original action execution
├── Multiplayer session verification
├── Synchronization packet creation
├── Packet sending
│   ├── If Host: Send to all clients
│   └── If Client: Send to host
└── Reception and execution on other players
```

## Cursor System

### Other Players' Cursor Visualization

```
Cursor System
├── CursorManager
│   ├── Mouse position tracking
│   ├── Active tool state detection
│   └── PlayerCursorPacket sending
├── PlayerCursor (GameObject)
│   ├── Visual cursor rendering
│   ├── Player name display
│   └── State animations
└── MultiplayerSession
    ├── Cursor creation for connected players
    └── Cursor removal for disconnected players
```

## Save File Sharing

### Small Saves (< 10MB)
- Sent directly via Steam Networking
- Fragmented into chunks for transmission

### Large Saves (> 10MB)
```
Google Drive Sharing
├── Host uploads save to Google Drive
├── Public download link generation
├── Link sending via GoogleDriveFileSharePacket
├── Clients download the file
└── Local save loading
```

## Connection States

### Client States
- **Disconnected**: Not connected
- **Connecting**: Attempting to connect
- **Connected**: Connected to lobby
- **InGame**: Actively playing
- **Error**: Connection error

### Server States
- **Stopped**: Server stopped
- **Preparing**: Preparing initialization
- **Starting**: Starting server
- **Started**: Server active
- **Error**: Server error

## Ready Check System

```
Readiness Verification
├── Client sends ClientReadyStatusPacket
├── Host collects status from all clients
├── When all are ready:
│   ├── Host sends AllClientsReadyPacket
│   ├── Starts world synchronization
│   └── Enables packet processing
```

## Disconnection Handling

### Client Disconnection
```
Client Disconnects
├── Connection loss detection
├── Player removal from session
├── Player cursor cleanup
├── Chat notification
└── Member list update
```

### Host Disconnection
```
Host Disconnects
├── Connection loss detection
├── Error message display
├── Forced return to main menu
├── Network state cleanup
└── Steam lobby exit
```

## Debug and Development

### Debug Tools
- **DebugConsole**: In-game command console
- **DebugMenu**: Debug menu with network options
- **Hierarchy Viewer**: Object hierarchy visualization
- **Network Statistics**: Real-time network statistics

### Logging
- Detailed logs of all network operations
- Sent/received packet tracking
- Connection state monitoring

## Configuration

### Configuration File (`multiplayer_settings.json`)
```json
{
  "Host": {
    "MaxLobbySize": 4,
    "MaxMessagesPerPoll": 128,
    "SaveFileTransferChunkKB": 256,
    "GoogleDrive": {
      "ApplicationName": "ONI Multiplayer Mod"
    }
  },
  "Client": {
    "UseCustomMainMenu": true,
    "MaxMessagesPerPoll": 16,
    "UseRandomPlayerColor": true,
    "PlayerColor": { "R": 255, "G": 255, "B": 255 }
  }
}
```

## Known Limitations

1. **Maximum of 4 players** per lobby
2. **Steam dependency** for networking
3. **Google Drive requirement** for large saves
4. **Limited synchronization** - not all game actions are synchronized
5. **Pre-alpha state** - bugs and instabilities expected

## Next Steps

1. Expand synchronization to more game actions
2. Network performance optimization
3. Multiplayer UI improvements
4. Permission system implementation
5. Additional mod support

---

This document provides a comprehensive view of ONI Together's internal workings. For development information, see [DEVELOPMENT.md](DEVELOPMENT.md).