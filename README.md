![Logo](https://i.imgur.com/GCIbhpn.png)
# ONI Together: An Oxygen Not Included Multiplayer Mod (WIP)

> **Note:** This is a work-in-progress project. Not to be confused with [onimp/oni_multiplayer](https://github.com/onimp/oni_multiplayer).

A new mod that introduces multiplayer functionality to *Oxygen Not Included*, featuring a custom networking layer and lobby system.
> **Note:** This mod is in the very early pre-pre alpha stage. Name is subject to change

Join the [Discord](https://discord.gg/jpxveK6mmY)

Steam workshop: Not released yet.

---

## Demo

![Multiplayer Demo](https://i.imgur.com/VfiPUkn.jpeg)

---

## What's Done

- Network infrastructure for sending and receiving packets  
- Creating, joining, and leaving lobbies  
- Host, Client and Session detection  
- Steam overlay support  
  - Ability to join/invite friends
- Debug Tools
  - Bespoke console
  - Hierarchy viewer
  - Debug Menu (Open with Shift+F1)
    
  Debug Menu contains:
    - Open other menus
    - Test steamworks instantiation
    - Create Lobby
    - Leave Lobby

- Chat box
    - Fully synchronized
    - Includes Join / Leave messages of other users
    - Themed to look like other UI elements (95% finished)
    - Expandable
    - Can be dragged and moved around
 
 - Synchronization
    - Building tool
    - Cancel tool
    - Deconstruct tool
    - Priority tool
    - Sweep tool
    - Mop tool
    - Digging
    - World Cycle
      > (Controlled by the host)
    - Rough duplicant position and orientation synchronization
      > They at least move smoothly. They only snap if they fall out of sync
    - Rough Duplicant animation synchronization
      > Some animations don't play properly and get stuck on 1 frame like walking, running, climbing etc. Others seem fine
    - Move To tool synchronization
      > When you click a duplicant and use the "Move To Location" tool
    - "Trigger" synchronization
      > (its rough and not 100% but it lets duplicants pull out their dig tools, suck up tools etc)
    - Hard Sync
      > (At the start of every new cycle the server will perform a hard sync, which basically boots out all the clients and makes them redownload the map before automatically reconnecting them)
    - Player cursors
      > See where other players are pointing, the tool they are using and they are color coded.
    - Save file synchronization via Google Drive
      > Using Google Drive allows synchronization of worlds larger then 10MB. A limit the other multiplayer mod has. Without it, this mod would have the same 10MB limit. This however comes at the cost of a small but simple setup process by hosts that only needs to be completed once. The guide can be found [here](https://github.com/Lyraedan/Oxygen_Not_Included_Multiplayer/wiki/Google-Drive-Setup-Guide)

- Synchronized UI Elements
     - World Cycle

- Configuration file
   > Change lobby size, polling rates and player color (the color other players see you as)

---

## Work in Progress
- Tool synchronization (Building, Wire building, Pipe building, Mopping, Sweeping etc)
  > There is a rough sync attempt with wires but its unfinished any of the smaller tools (like sweep) are not synced (mopping, attacking, ranching etc)
- Storage sync
  > This right now relies on hard sync
- Gas, Temp, Fluid sync
  > I think this will stay to hard sync and let the client interpret how they should flow

## Known issues
- Theres alot of issues right now. But once these are ironed out a release will be put out on the steam workshop which will later be linked here

- Crash when connecting to a host that has alot of things going on.
  > Like duplicants actively digging etc (This happens sometimes)
- The loading screens disappear when connecting to a host / hard syncing etc
- Inviting from the pause menu does not invite the player
  >(Thanks steam x_x)
- Clients seem to get double the resource drops
  > Not sure why considering they get passed the hosts values
- Clients can sometimes trigger their own tasks which causes them to fall out of sync
  > Synchronization is properly regained when a hard sync occures
- Clients can have ceiling collapses when the host doesn't and vise versa
- When sweeping the clients can fall out of sync
  > Synchronization is properly regained when a hard sync occures
- When placing wires they don't look like they are connected properly for other players
  > When other players place them though they suddenly look connected to something and sometimes won't build?
- Sometimes after a hard sync clients can't seem to process incoming packets.
  > To fix this the client should just restart their game and rejoin

## Found an issue?
Raise it on the issues page. Please at least try to include a video if you can. It makes replicating it so much easier

---

## What's Planned

- World state synchronization
- Seamless mid-game hosting (start and stop multiplayer at any point during gameplay)  
- Menu and UI synchronization  
- Additional features to be announced
- More item synchronization (storage, research, skill points etc)

---

## Why not just contribute to the old multiplayer mod?

I like the old multiplayer mod — I do — and kudos to the guys that made it. But its implementation is very limited without a lot of extra effort, if not a full rewrite.  
On top of this, it hasn't seen activity in over 6 months.  
> **NOTE:** as of June 6th 2025, when I started this project.

Initially it was just conceptual, but once I got lobbies and packets set up, I knew I was onto something.

## Setup

To get started with building the mod, follow these steps:

### Prerequisites
- **Python 3.7+** for development scripts
- **.NET Framework 4.7.2+** (Windows) or **.NET SDK 6.0+** (Linux/Mac)
- **Oxygen Not Included** installed via Steam

### Quick Setup

1. **Clone the repository**  
   ```bash
   git clone https://github.com/Lyraedan/Oxygen_Not_Included_Multiplayer.git
   cd Oxygen_Not_Included_Multiplayer
   ```

2. **Run the setup script**  
   ```bash
   python setup-dev-environment.py
   ```
   This will automatically:
   - Detect your ONI installation
   - Configure environment variables
   - Verify dependencies
   - Test the build

3. **Build and install the mod**  
   ```bash
   python build.py --install
   ```

### Quick Commands

```bash
# Build and install mod
python build.py --install

# Configure Visual Studio
python configure-vs.py

# Development tools
python dev-tools.py build --install    # Build and install
python dev-tools.py configure-vs       # Configure Visual Studio
python dev-tools.py watch-logs         # Monitor game logs
python dev-tools.py test-deps          # Check dependencies
```

## Documentation

- **[Development Guide](docs/DEVELOPMENT.md)** - Complete setup and development instructions
- **[Visual Studio Setup](docs/VISUAL_STUDIO_SETUP.md)** - Configure Visual Studio for development
- **[API Reference](docs/API_REFERENCE.md)** - Code documentation and examples
- **[Changelog](docs/CHANGELOG.md)** - Version history and changes

---

## Contributing

Contributions are welcome!  
If you have improvements, fixes, or new features, feel free to open a Pull Request.

Please make sure your changes are clear and well-documented where necessary.

---

## License

This project is licensed under the MIT License.  
Copyright (c) 2023 Zuev Vladimir, Denis Pakhorukov

