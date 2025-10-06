# Visual Studio Setup Guide

This guide explains how to configure Visual Studio to work with the ONI Together mod development environment.

## Quick Setup

Run the automatic configuration script:

```bash
python configure-vs.py
```

This script will:
- Detect your ONI installation
- Create `ONI_MP.props` with the correct paths
- Update the `.csproj` file to import the properties
- Test the configuration

## Manual Setup (if needed)

If the automatic script doesn't work, you can configure manually:

### 1. Create ONI_MP.props

Create a file named `ONI_MP.props` in the root directory with:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <ONI_GAME_DIR>YOUR_ONI_PATH_HERE</ONI_GAME_DIR>
    <GAME_DIR>YOUR_ONI_PATH_HERE</GAME_DIR>
  </PropertyGroup>
</Project>
```

Replace `YOUR_ONI_PATH_HERE` with your actual ONI installation path, for example:
- `E:\SteamLibrary\steamapps\common\OxygenNotIncluded`
- `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded`

### 2. Verify .csproj Import

Make sure the `ClassLibrary1/ONI_MP.csproj` file has this import line near the top:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="../ONI_MP.props" Condition="Exists('../ONI_MP.props')" />
  <!-- rest of the project file -->
</Project>
```

## Common Issues

### DLLs Still Not Found

1. **Close Visual Studio completely**
2. **Run the configuration script:**
   ```bash
   python configure-vs.py
   ```
3. **Reopen Visual Studio**
4. **Clean and rebuild the solution**

### Environment Variables Not Working

Visual Studio sometimes doesn't pick up environment variables. The `ONI_MP.props` file solves this by providing the paths directly to MSBuild.

### Multiple ONI Installations

If you have ONI installed in multiple locations, edit the `ONI_MP.props` file to point to the correct installation.

## Verification

After setup, you should be able to:

1. **Open the solution** in Visual Studio without errors
2. **See all references** resolved (no yellow warning triangles)
3. **Build successfully** (Ctrl+Shift+B)
4. **IntelliSense works** for ONI classes like `GameObject`, `Component`, etc.

## Files Created

The configuration creates these files:

- `ONI_MP.props` - Contains the ONI path for Visual Studio
- `Directory.Build.props` - Auto-detection for different ONI locations
- Updated `ClassLibrary1/ONI_MP.csproj` - Imports the properties

## Alternative: Environment Variables

You can also set system environment variables:

**Windows:**
```cmd
setx ONI_GAME_DIR "E:\SteamLibrary\steamapps\common\OxygenNotIncluded"
```

**PowerShell:**
```powershell
[Environment]::SetEnvironmentVariable("ONI_GAME_DIR", "E:\SteamLibrary\steamapps\common\OxygenNotIncluded", "User")
```

After setting environment variables, restart Visual Studio.

## Troubleshooting

### Script Fails to Find ONI

If the automatic script can't find ONI:

1. **Check common locations:**
   - `C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded`
   - `E:\SteamLibrary\steamapps\common\OxygenNotIncluded`
   - Your custom Steam library folders

2. **Manually create ONI_MP.props** with the correct path

3. **Verify the path** contains `OxygenNotIncluded_Data\Managed` folder

### Build Still Fails

1. **Check the Output window** in Visual Studio for detailed errors
2. **Verify all required DLLs exist** in the Managed folder:
   - `Assembly-CSharp.dll`
   - `0Harmony.dll`
   - `com.rlabrecque.steamworks.net.dll`
   - `UnityEngine.dll`
   - And others listed in the `.csproj`

3. **Update ONI** if DLLs are missing or outdated

### IntelliSense Not Working

1. **Clean and rebuild** the solution
2. **Close and reopen** Visual Studio
3. **Delete** `bin` and `obj` folders, then rebuild
4. **Check** that references show the correct paths in Solution Explorer