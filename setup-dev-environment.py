#!/usr/bin/env python3
"""
ONI Together - Script de Configuração do Ambiente de Desenvolvimento
Este script configura o ambiente para desenvolver o mod ONI Together
Funciona no Windows e Linux
"""

import os
import sys
import platform
import subprocess
import shutil
from pathlib import Path

class Colors:
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    RED = '\033[91m'
    WHITE = '\033[97m'
    GRAY = '\033[90m'
    RESET = '\033[0m'
    
    @staticmethod
    def print_colored(text, color):
        print(f"{color}{text}{Colors.RESET}")

def print_header():
    Colors.print_colored("=== ONI Together - Development Environment Setup ===", Colors.GREEN)
    print()

def check_dotnet_framework():
    """Check .NET Framework on Windows"""
    Colors.print_colored("1. Checking .NET Framework...", Colors.YELLOW)
    
    if platform.system() != "Windows":
        Colors.print_colored("   ✓ Linux detected - .NET Framework not required", Colors.GREEN)
        return True
    
    try:
        import winreg
        key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, 
                           r"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full")
        release, _ = winreg.QueryValueEx(key, "Release")
        winreg.CloseKey(key)
        
        if release >= 461808:  # .NET Framework 4.7.2+
            Colors.print_colored("   ✓ .NET Framework 4.7.2+ detected", Colors.GREEN)
            return True
        else:
            Colors.print_colored("   ✗ .NET Framework 4.7.2+ required", Colors.RED)
            Colors.print_colored("   Download at: https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472", Colors.YELLOW)
            return False
    except Exception as e:
        Colors.print_colored(f"   ✗ Error checking .NET Framework: {e}", Colors.RED)
        return False

def check_dotnet_sdk():
    """Check .NET SDK"""
    Colors.print_colored("2. Checking .NET SDK...", Colors.YELLOW)
    
    try:
        result = subprocess.run(['dotnet', '--version'], 
                              capture_output=True, text=True, check=True)
        version = result.stdout.strip()
        Colors.print_colored(f"   ✓ .NET SDK {version} detected", Colors.GREEN)
        return True
    except (subprocess.CalledProcessError, FileNotFoundError):
        Colors.print_colored("   ✗ .NET SDK not found", Colors.RED)
        Colors.print_colored("   Download at: https://dotnet.microsoft.com/download", Colors.YELLOW)
        return False

def find_oni_path():
    """Locate Oxygen Not Included"""
    Colors.print_colored("3. Locating Oxygen Not Included...", Colors.YELLOW)
    
    if platform.system() == "Windows":
        possible_paths = [
            r"C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded",
            r"C:\Program Files\Steam\steamapps\common\OxygenNotIncluded",
            r"D:\Steam\steamapps\common\OxygenNotIncluded",
            r"E:\Steam\steamapps\common\OxygenNotIncluded",
            r"F:\Steam\steamapps\common\OxygenNotIncluded",
            r"F:\SteamLibrary\steamapps\common\OxygenNotIncluded"
        ]
    else:  # Linux
        home = Path.home()
        possible_paths = [
            home / ".steam/steam/steamapps/common/OxygenNotIncluded",
            home / ".local/share/Steam/steamapps/common/OxygenNotIncluded",
            "/usr/games/steam/steamapps/common/OxygenNotIncluded"
        ]
    
    # Search in default paths
    for path in possible_paths:
        if Path(path).exists():
            Colors.print_colored(f"   ✓ ONI found at: {path}", Colors.GREEN)
            return str(path)
    
    # If not found, ask user
    Colors.print_colored("   ⚠ Oxygen Not Included not found in default locations", Colors.YELLOW)
    Colors.print_colored("   Please enter the full path to the game folder:", Colors.YELLOW)
    
    if platform.system() == "Windows":
        Colors.print_colored("   Example: C:\\Program Files (x86)\\Steam\\steamapps\\common\\OxygenNotIncluded", Colors.GRAY)
    else:
        Colors.print_colored("   Example: ~/.steam/steam/steamapps/common/OxygenNotIncluded", Colors.GRAY)
    
    while True:
        user_path = input("   ONI Path: ").strip()
        if Path(user_path).exists():
            managed_path = Path(user_path) / "OxygenNotIncluded_Data" / "Managed"
            if managed_path.exists():
                Colors.print_colored(f"   ✓ ONI found at: {user_path}", Colors.GREEN)
                return user_path
            else:
                Colors.print_colored(f"   ✗ Managed folder not found at: {managed_path}", Colors.RED)
        else:
            Colors.print_colored(f"   ✗ Path does not exist: {user_path}", Colors.RED)

def set_game_dir(oni_path):
    """Configure ONI_GAME_DIR environment variable"""
    Colors.print_colored("4. Configuring ONI_GAME_DIR environment variable...", Colors.YELLOW)
    
    # Set for current session
    os.environ['ONI_GAME_DIR'] = oni_path
    
    # Try to set permanently
    if platform.system() == "Windows":
        try:
            subprocess.run(['setx', 'ONI_GAME_DIR', oni_path], check=True, capture_output=True)
            Colors.print_colored(f"   ✓ ONI_GAME_DIR set permanently: {oni_path}", Colors.GREEN)
        except subprocess.CalledProcessError:
            Colors.print_colored(f"   ⚠ ONI_GAME_DIR set for this session only: {oni_path}", Colors.YELLOW)
            Colors.print_colored("   To set permanently, run: setx ONI_GAME_DIR \"" + oni_path + "\"", Colors.GRAY)
    else:  # Linux
        bashrc_path = Path.home() / ".bashrc"
        game_dir_line = f'export ONI_GAME_DIR="{oni_path}"\n'
        
        try:
            with open(bashrc_path, 'r') as f:
                content = f.read()
            
            if 'ONI_GAME_DIR=' not in content:
                with open(bashrc_path, 'a') as f:
                    f.write(f'\n# ONI Together mod development\n{game_dir_line}')
                Colors.print_colored(f"   ✓ ONI_GAME_DIR added to ~/.bashrc: {oni_path}", Colors.GREEN)
            else:
                Colors.print_colored(f"   ✓ ONI_GAME_DIR already exists in ~/.bashrc: {oni_path}", Colors.GREEN)
        except Exception as e:
            Colors.print_colored(f"   ⚠ Error configuring ~/.bashrc: {e}", Colors.YELLOW)
            Colors.print_colored(f"   ONI_GAME_DIR set for this session only: {oni_path}", Colors.YELLOW)

def check_dependencies(oni_path):
    """Check project dependencies"""
    Colors.print_colored("5. Checking project dependencies...", Colors.YELLOW)
    
    managed_path = Path(oni_path) / "OxygenNotIncluded_Data" / "Managed"
    required_dlls = [
        "Assembly-CSharp.dll",
        "0Harmony.dll",
        "com.rlabrecque.steamworks.net.dll", 
        "UnityEngine.dll",
        "Unity.TextMeshPro.dll"
    ]
    
    missing_dlls = []
    for dll in required_dlls:
        dll_path = managed_path / dll
        if not dll_path.exists():
            missing_dlls.append(dll)
    
    if not missing_dlls:
        Colors.print_colored("   ✓ All dependencies found", Colors.GREEN)
    else:
        Colors.print_colored("   ⚠ DLLs not found:", Colors.YELLOW)
        for dll in missing_dlls:
            Colors.print_colored(f"     - {dll}", Colors.RED)
        Colors.print_colored("   Make sure ONI is updated", Colors.YELLOW)
    
    return len(missing_dlls) == 0

def test_build():
    """Test project build"""
    Colors.print_colored("6. Testing project build...", Colors.YELLOW)
    
    project_path = Path("ClassLibrary1")
    if not project_path.exists():
        Colors.print_colored("   ✗ ClassLibrary1 folder not found", Colors.RED)
        return False
    
    try:
        result = subprocess.run(['dotnet', 'build', '--verbosity', 'quiet'], 
                              cwd=project_path, capture_output=True, text=True)
        if result.returncode == 0:
            Colors.print_colored("   ✓ Build successful!", Colors.GREEN)
            return True
        else:
            Colors.print_colored("   ✗ Build error:", Colors.RED)
            Colors.print_colored(f"   {result.stderr}", Colors.RED)
            return False
    except Exception as e:
        Colors.print_colored(f"   ✗ Error executing build: {e}", Colors.RED)
        return False

def setup_mods_folder():
    """Configure mods folder"""
    Colors.print_colored("7. Configuring mods folder...", Colors.YELLOW)
    
    if platform.system() == "Windows":
        documents_path = Path.home() / "Documents"
        mods_path = documents_path / "Klei" / "OxygenNotIncluded" / "mods" / "local" / "oni_mp"
    else:  # Linux
        home = Path.home()
        # Search for possible locations on Linux
        possible_paths = [
            home / ".klei" / "OxygenNotIncluded" / "mods" / "local" / "oni_mp",
            home / "Documents" / "Klei" / "OxygenNotIncluded" / "mods" / "local" / "oni_mp",
            home / ".local" / "share" / "Klei" / "OxygenNotIncluded" / "mods" / "local" / "oni_mp"
        ]
        
        # Use the first one that exists or create the default
        mods_path = None
        for path in possible_paths:
            if path.parent.parent.exists():
                mods_path = path
                break
        
        if not mods_path:
            mods_path = possible_paths[0]  # Use default
    
    try:
        mods_path.mkdir(parents=True, exist_ok=True)
        Colors.print_colored(f"   ✓ Mods folder configured: {mods_path}", Colors.GREEN)
        return str(mods_path)
    except Exception as e:
        Colors.print_colored(f"   ✗ Error creating mods folder: {e}", Colors.RED)
        return None

def print_summary(oni_path, mods_path):
    """Print configuration summary"""
    print()
    Colors.print_colored("=== Setup Complete ===", Colors.GREEN)
    print()
    Colors.print_colored("Next steps:", Colors.YELLOW)
    Colors.print_colored("1. To compile: cd ClassLibrary1 && dotnet build", Colors.WHITE)
    if mods_path:
        Colors.print_colored(f"2. To install mod: copy .dll to {mods_path}", Colors.WHITE)
    Colors.print_colored("3. For debug: use Shift+F1 in game", Colors.WHITE)
    print()
    Colors.print_colored("Environment variables:", Colors.YELLOW)
    Colors.print_colored(f"ONI_GAME_DIR = {oni_path}", Colors.WHITE)
    print()
    Colors.print_colored("Useful commands:", Colors.YELLOW)
    if platform.system() == "Windows":
        Colors.print_colored("python build.py                    - Compile project", Colors.WHITE)
        Colors.print_colored("python build.py --install           - Compile and install mod", Colors.WHITE)
        Colors.print_colored("python build.py --clean --install   - Clean, compile and install", Colors.WHITE)
    else:
        Colors.print_colored("./build.py                          - Compile project", Colors.WHITE)
        Colors.print_colored("./build.py --install                - Compile and install mod", Colors.WHITE)
        Colors.print_colored("./build.py --clean --install        - Clean, compile and install", Colors.WHITE)

def main():
    """Main function"""
    print_header()
    
    # Checks
    if not check_dotnet_sdk():
        sys.exit(1)
    
    if platform.system() == "Windows" and not check_dotnet_framework():
        sys.exit(1)
    
    # Locate ONI
    oni_path = find_oni_path()
    if not oni_path:
        Colors.print_colored("Could not locate Oxygen Not Included", Colors.RED)
        sys.exit(1)
    
    # Configure environment
    set_game_dir(oni_path)
    check_dependencies(oni_path)
    test_build()
    mods_path = setup_mods_folder()
    
    # Summary
    print_summary(oni_path, mods_path)

if __name__ == "__main__":
    main()