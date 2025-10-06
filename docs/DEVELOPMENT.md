# ONI Together - Development Guide

This guide explains how to set up and use the development environment for the ONI Together mod.

## Initial Setup

### 1. Environment Configuration

Run the setup script to prepare your development environment:

```bash
python setup-dev-environment.py
```

This script will:
- Check .NET Framework 4.7.2+ (Windows) and .NET SDK
- Automatically locate Oxygen Not Included
- Configure the `ONI_GAME_DIR` environment variable
- Verify project dependencies
- Test compilation
- Create the mods folder

### 2. Manual ONI_GAME_DIR Setup (if needed)

If the script cannot automatically locate the game, you can set it manually:

**Windows (PowerShell):**
```powershell
$env:ONI_GAME_DIR = "E:\SteamLibrary\steamapps\common\OxygenNotIncluded"
```

**Linux/Mac (Bash):**
```bash
export ONI_GAME_DIR="~/.steam/steam/steamapps/common/OxygenNotIncluded"
```

### 3. Visual Studio Configuration

If you're using Visual Studio, run the VS configuration script:

```bash
python configure-vs.py
```

This will create the necessary property files for Visual Studio to find the game DLLs.

## Development Commands

### Build Script (build.py)

```bash
# Compile project
python build.py

# Compile and install mod
python build.py --install

# Clean, compile and install
python build.py --clean --install

# Compile in Release mode
python build.py --configuration Release

# Show project information
python build.py --info
```

### Development Tools (dev-tools.py)

```bash
# Show help
python dev-tools.py

# Compile and install
python dev-tools.py build --install

# Check dependencies
python dev-tools.py test-deps

# Show project information
python dev-tools.py info

# Open mods folder
python dev-tools.py open-mods

# Open game folder
python dev-tools.py open-game

# Monitor game logs
python dev-tools.py watch-logs

# Configure environment
python dev-tools.py setup

# Configure Visual Studio
python dev-tools.py configure-vs
```

## Project Structure

```
ONI_Together/
├── ClassLibrary1/              # Main source code
│   ├── ONI_MP.csproj          # Project file
│   ├── MultiplayerMod.cs      # Mod entry point
│   ├── Configuration.cs       # Configuration system
│   ├── Networking/            # Networking system
│   ├── Patches/               # Harmony patches
│   ├── DebugTools/            # Debug tools
│   ├── Menus/                 # Custom UI
│   ├── Cloud/                 # Google Drive integration
│   └── Misc/                  # Utilities
├── docs/                      # Documentation
├── setup-dev-environment.py   # Environment setup
├── build.py                   # Build script
├── dev-tools.py              # Development tools
├── configure-vs.py           # Visual Studio configuration
└── Directory.Build.props     # MSBuild properties
```

## Development Workflow

1. **Initial environment setup:**
   ```bash
   python setup-dev-environment.py
   ```

2. **Configure Visual Studio (if using):**
   ```bash
   python configure-vs.py
   ```

3. **Make code changes** (in `ClassLibrary1/` folder)

4. **Compile and test:**
   ```bash
   python build.py --install
   ```

5. **Test in game:**
   - Open Oxygen Not Included
   - Use `Shift+F1` to open debug menu
   - Test mod functionality

6. **Monitor logs (optional):**
   ```bash
   python dev-tools.py watch-logs
   ```

## Dependencies

### Windows
- .NET Framework 4.7.2+
- .NET SDK 6.0+
- Oxygen Not Included (Steam)
- Visual Studio (optional)

### Linux
- .NET SDK 6.0+
- Oxygen Not Included (Steam)

## File Locations

### Game Directory (ONI_GAME_DIR)
- **Windows:** `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded`
- **Linux:** `~/.steam/steam/steamapps/common/OxygenNotIncluded`

### Mods Folder
- **Windows:** `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\local\oni_mp\`
- **Linux:** `~/.klei/OxygenNotIncluded/mods/local/oni_mp/`

### Game Logs
- **Windows:** `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\Player.log`
- **Linux:** `~/.klei/OxygenNotIncluded/Player.log`

## Troubleshooting

### Error: "ONI_GAME_DIR not configured"
```bash
# Check if it's defined
python dev-tools.py test-deps

# Configure manually (Windows)
$env:ONI_GAME_DIR = "PATH_TO_ONI"

# Configure manually (Linux)
export ONI_GAME_DIR="PATH_TO_ONI"
```

### Error: "DLLs not found"
- Make sure ONI is installed and updated
- Verify the `ONI_GAME_DIR` path is correct
- Run `python setup-dev-environment.py` again
- For Visual Studio: run `python configure-vs.py`

### Compilation Error
```bash
# Clean and rebuild
python build.py --clean --install

# Check dependencies
python dev-tools.py test-deps
```

### Mod doesn't appear in game
- Check if `ONI_MP.dll` is in the mods folder
- Run `python build.py --info` to check status
- Make sure mods are enabled in the game

### Visual Studio can't find DLLs
```bash
# Configure Visual Studio
python configure-vs.py

# Then restart Visual Studio
```

## In-Game Debugging

1. **Debug Menu:** Press `Shift+F1` in game
2. **Console:** Available in debug menu
3. **Hierarchy Viewer:** To inspect game objects
4. **Logs:** Use `python dev-tools.py watch-logs` to monitor

## Contributing

1. Make your code changes
2. Test locally with `python build.py --install`
3. Verify no compilation errors
4. Test functionality in game
5. Commit and push changes

## Useful Scripts

### Quick Build
```bash
# Alias for quick compile and install
alias oni-build="python build.py --install"
```

### Continuous Monitoring
```bash
# Monitor logs in real time
python dev-tools.py watch-logs
```

### Health Check
```bash
# Check if everything is working
python dev-tools.py test-deps && python build.py --info
```