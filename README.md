# Oxygen Not Included – Multiplayer Mod (WIP)

> **Note:** This is a work-in-progress project. Not to be confused with [onimp/oni_multiplayer](https://github.com/onimp/oni_multiplayer).

A new mod that introduces multiplayer functionality to *Oxygen Not Included*, featuring a custom networking layer and lobby system.
> **Note:** This mod is in the very early pre-pre alpha stage

Join the [Discord](https://discord.gg/jpxveK6mmY)

---

## Demo

![Multiplayer Demo](https://i.imgur.com/BDSK0cu.png)

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

---

## Work in Progress

- Move chore synchronization (implemented, currently undergoing testing)
- Entity Position synchronization
  >(Duplicates, implemented, being sent a roughly updated on clients)
- Chore synchronization
  >(Implemented, Chore packet is being sent to clients when a duplicant starts a new chore, when a client recieves this packet they force begin the chore)
- Preventing Duplicants, critters, etc. from moving on clients
  >(their positions will be synchronized from the host)
- Main menu UI  
  - Host/Join/Cancel (Hooked up but not plugged in)
- World / Save synchronization

---

## What's Planned

- World state synchronization  
- Seamless mid-game hosting (start and stop multiplayer at any point during gameplay)  
- Menu and UI synchronization  
- Additional features to be announced  

---

## Why not just contribute to the old multiplayer mod?

I like the old multiplayer mod — I do — and kudos to the guys that made it. But its implementation is very limited without a lot of extra effort, if not a full rewrite.  
On top of this, it hasn't seen activity in over 6 months.  
> **NOTE:** as of June 6th 2025, when I started this project.

Initially it was just conceptual, but once I got lobbies and packets set up, I knew I was onto something.

## Setup

To get started with building the mod, follow these steps:

1. **Install .NET Framework 4.7.2**  
   Make sure you have [.NET Framework 4.7.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472) installed on your system.

2. **Clone the repository**  
   ```
   git clone https://github.com/Lyraedan/Oxygen_Not_Included_Multiplayer.git
   ```

3. **Open the solution**  
   Open the `.sln` file in Visual Studio (or your preferred C# IDE).

4. **Update the `ManagedPath` in the `.csproj` file**  
   Open the `.csproj` file and find the `ManagedPath` property.  
   Change its value to point to your local `OxygenNotIncluded_Data/Managed` folder, for example:
   ```xml
   <ManagedPath>C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed</ManagedPath>
   ```

5. **Build the project**  
   Once the `ManagedPath` is correctly set, build the project.  
   If everything is configured correctly, the build should succeed.

---

## Contributing

Contributions are welcome!  
If you have improvements, fixes, or new features, feel free to open a Pull Request.

Please make sure your changes are clear and well-documented where necessary.

---

## License

This project is licensed under the MIT License.  
Copyright (c) 2023 Zuev Vladimir, Denis Pakhorukov

