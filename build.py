#!/usr/bin/env python3
"""
ONI Together - Build Script
Compiles the project and optionally installs the mod
Works on Windows and Linux
"""

import os
import sys
import argparse
import subprocess
import shutil
import platform
from pathlib import Path

class Colors:
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    RED = '\033[91m'
    WHITE = '\033[97m'
    RESET = '\033[0m'
    
    @staticmethod
    def print_colored(text, color):
        print(f"{color}{text}{Colors.RESET}")

def clean_build():
    """Clean previous build files"""
    Colors.print_colored("Cleaning previous build...", Colors.YELLOW)
    
    project_path = Path("ClassLibrary1")
    if not project_path.exists():
        Colors.print_colored("✗ ClassLibrary1 folder not found", Colors.RED)
        return False
    
    try:
        result = subprocess.run(['dotnet', 'clean'], cwd=project_path, 
                              capture_output=True, text=True)
        if result.returncode == 0:
            Colors.print_colored("✓ Build cleaned successfully", Colors.GREEN)
            return True
        else:
            Colors.print_colored(f"✗ Error cleaning build: {result.stderr}", Colors.RED)
            return False
    except Exception as e:
        Colors.print_colored(f"✗ Error executing dotnet clean: {e}", Colors.RED)
        return False

def build_project(configuration="Debug"):
    """Compile the project"""
    Colors.print_colored("Compiling project...", Colors.YELLOW)
    
    project_path = Path("ClassLibrary1")
    if not project_path.exists():
        Colors.print_colored("✗ ClassLibrary1 folder not found", Colors.RED)
        return False, None
    
    try:
        result = subprocess.run(['dotnet', 'build', '--configuration', configuration], 
                              cwd=project_path, capture_output=True, text=True)
        if result.returncode == 0:
            Colors.print_colored("✓ Build successful!", Colors.GREEN)
            
            # Find generated DLL file
            dll_path = project_path / "bin" / configuration / "net472" / "ONI_MP.dll"
            if dll_path.exists():
                return True, str(dll_path)
            else:
                Colors.print_colored(f"⚠ DLL not found at: {dll_path}", Colors.YELLOW)
                return True, None
        else:
            Colors.print_colored("✗ Build error:", Colors.RED)
            Colors.print_colored(result.stdout, Colors.RED)
            Colors.print_colored(result.stderr, Colors.RED)
            return False, None
    except Exception as e:
        Colors.print_colored(f"✗ Error executing build: {e}", Colors.RED)
        return False, None

def find_mods_path():
    """Find the mods folder path"""
    if platform.system() == "Windows":
        documents_path = Path.home() / "Documents"
        mods_path = documents_path / "Klei" / "OxygenNotIncluded" / "mods" / "local" / "oni_mp"
    else:  # Linux
        home = Path.home()
        possible_paths = [
            home / ".klei" / "OxygenNotIncluded" / "mods" / "local" / "oni_mp",
            home / "Documents" / "Klei" / "OxygenNotIncluded" / "mods" / "local" / "oni_mp",
            home / ".local" / "share" / "Klei" / "OxygenNotIncluded" / "mods" / "local" / "oni_mp"
        ]
        
        # Search for which one exists
        for path in possible_paths:
            if path.parent.exists() or path.exists():
                mods_path = path
                break
        else:
            mods_path = possible_paths[0]  # Usar o padrão
    
    return mods_path

def install_mod(dll_path):
    """Install mod to ONI mods folder"""
    Colors.print_colored("Installing mod...", Colors.YELLOW)
    
    if not dll_path or not Path(dll_path).exists():
        Colors.print_colored("✗ DLL not found for installation", Colors.RED)
        return False
    
    mods_path = find_mods_path()
    
    try:
        # Create folder if it doesn't exist
        mods_path.mkdir(parents=True, exist_ok=True)
        
        # Copy DLL
        destination = mods_path / "ONI_MP.dll"
        shutil.copy2(dll_path, destination)
        
        Colors.print_colored(f"✓ Mod installed at: {destination}", Colors.GREEN)
        return True
    except Exception as e:
        Colors.print_colored(f"✗ Error installing mod: {e}", Colors.RED)
        return False

def show_info():
    """Show project information"""
    Colors.print_colored("=== Project Information ===", Colors.GREEN)
    
    # Check ONI_GAME_DIR
    game_dir = os.environ.get('ONI_GAME_DIR')
    if game_dir:
        Colors.print_colored(f"ONI_GAME_DIR: {game_dir}", Colors.WHITE)
        if Path(game_dir).exists():
            Colors.print_colored("✓ Game folder exists", Colors.GREEN)
        else:
            Colors.print_colored("✗ Game folder does not exist", Colors.RED)
    else:
        Colors.print_colored("✗ ONI_GAME_DIR not configured", Colors.RED)
        Colors.print_colored("Run: python setup-dev-environment.py", Colors.YELLOW)
    
    # Check mods folder
    mods_path = find_mods_path()
    Colors.print_colored(f"Mods folder: {mods_path}", Colors.WHITE)
    
    if mods_path.exists():
        mod_dll = mods_path / "ONI_MP.dll"
        if mod_dll.exists():
            mod_time = mod_dll.stat().st_mtime
            import datetime
            mod_date = datetime.datetime.fromtimestamp(mod_time)
            Colors.print_colored(f"✓ Mod installed: {mod_date}", Colors.GREEN)
        else:
            Colors.print_colored("⚠ Mod not installed", Colors.YELLOW)
    else:
        Colors.print_colored("⚠ Mods folder does not exist", Colors.YELLOW)
    
    # Check project
    project_path = Path("ClassLibrary1")
    if project_path.exists():
        Colors.print_colored("✓ Project found", Colors.GREEN)
        
        dll_path = project_path / "bin" / "Debug" / "net472" / "ONI_MP.dll"
        if dll_path.exists():
            dll_time = dll_path.stat().st_mtime
            import datetime
            dll_date = datetime.datetime.fromtimestamp(dll_time)
            Colors.print_colored(f"✓ Last build: {dll_date}", Colors.GREEN)
        else:
            Colors.print_colored("⚠ Project not compiled", Colors.YELLOW)
    else:
        Colors.print_colored("✗ Project not found", Colors.RED)

def main():
    parser = argparse.ArgumentParser(description='ONI Together - Script de Build')
    parser.add_argument('--clean', action='store_true', 
                       help='Limpar build anterior')
    parser.add_argument('--install', action='store_true', 
                       help='Instalar mod após compilar')
    parser.add_argument('--configuration', default='Debug', 
                       choices=['Debug', 'Release'],
                       help='Build configuration (Debug/Release)')
    parser.add_argument('--info', action='store_true',
                       help='Show project information')
    
    args = parser.parse_args()
    
    if args.info:
        show_info()
        return
    
    Colors.print_colored("=== ONI Together - Build Script ===", Colors.GREEN)
    
    success = True
    dll_path = None
    
    # Limpar se solicitado
    if args.clean:
        success = clean_build()
        if not success:
            sys.exit(1)
    
    # Build project
    success, dll_path = build_project(args.configuration)
    if not success:
        sys.exit(1)
    
    # Instalar se solicitado
    if args.install:
        success = install_mod(dll_path)
        if not success:
            sys.exit(1)
    
    print()
    Colors.print_colored("Build complete!", Colors.GREEN)
    if not args.install and dll_path:
        Colors.print_colored("To install the mod, run: python build.py --install", Colors.YELLOW)

if __name__ == "__main__":
    main()