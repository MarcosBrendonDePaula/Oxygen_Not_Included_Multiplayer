#!/usr/bin/env python3
"""
ONI Together - Visual Studio Configuration
This script configures Visual Studio to find ONI DLLs
"""

import os
import sys
import platform
import subprocess
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

def find_oni_path():
    """Find ONI path"""
    if platform.system() == "Windows":
        possible_paths = [
            r"E:\SteamLibrary\steamapps\common\OxygenNotIncluded",
            r"C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded",
            r"C:\Program Files\Steam\steamapps\common\OxygenNotIncluded",
            r"D:\Steam\steamapps\common\OxygenNotIncluded",
            r"F:\Steam\steamapps\common\OxygenNotIncluded"
        ]
    else:
        home = Path.home()
        possible_paths = [
            home / ".steam/steam/steamapps/common/OxygenNotIncluded",
            home / ".local/share/Steam/steamapps/common/OxygenNotIncluded"
        ]
    
    for path in possible_paths:
        if Path(path).exists():
            return str(path)
    
    return None

def create_user_props():
    """Create user properties file for Visual Studio"""
    Colors.print_colored("Configuring Visual Studio...", Colors.YELLOW)
    
    oni_path = find_oni_path()
    if not oni_path:
        Colors.print_colored("✗ ONI not found", Colors.RED)
        return False
    
    # Create user properties file
    props_content = f"""<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <ONI_GAME_DIR>{oni_path}</ONI_GAME_DIR>
    <GAME_DIR>{oni_path}</GAME_DIR>
  </PropertyGroup>
</Project>"""
    
    # Save to project directory
    props_file = Path("ONI_MP.props")
    with open(props_file, 'w', encoding='utf-8') as f:
        f.write(props_content)
    
    Colors.print_colored(f"✓ Properties file created: {props_file}", Colors.GREEN)
    Colors.print_colored(f"✓ ONI_GAME_DIR configured to: {oni_path}", Colors.GREEN)
    
    return True

def update_csproj():
    """Update .csproj to import properties"""
    csproj_path = Path("ClassLibrary1/ONI_MP.csproj")
    
    if not csproj_path.exists():
        Colors.print_colored("✗ .csproj file not found", Colors.RED)
        return False
    
    # Read current content
    with open(csproj_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Check if import already exists
    if '<Import Project="../ONI_MP.props"' in content:
        Colors.print_colored("✓ .csproj already configured", Colors.GREEN)
        return True
    
    # Add import at the beginning of the project
    import_line = '  <Import Project="../ONI_MP.props" Condition="Exists(\'../ONI_MP.props\')" />\n'
    
    # Insert after the first Project line
    lines = content.split('\n')
    for i, line in enumerate(lines):
        if '<Project Sdk=' in line:
            lines.insert(i + 1, import_line)
            break
    
    # Save updated file
    with open(csproj_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))
    
    Colors.print_colored("✓ .csproj updated to import properties", Colors.GREEN)
    return True

def test_configuration():
    """Test if configuration is working"""
    Colors.print_colored("Testing configuration...", Colors.YELLOW)
    
    try:
        result = subprocess.run(['dotnet', 'build', '--verbosity', 'minimal'], 
                              cwd='ClassLibrary1', capture_output=True, text=True)
        if result.returncode == 0:
            Colors.print_colored("✓ Build working in Visual Studio!", Colors.GREEN)
            return True
        else:
            Colors.print_colored("✗ Build error:", Colors.RED)
            Colors.print_colored(result.stderr, Colors.RED)
            return False
    except Exception as e:
        Colors.print_colored(f"✗ Error testing: {e}", Colors.RED)
        return False

def main():
    Colors.print_colored("=== Visual Studio Configuration for ONI Together ===", Colors.GREEN)
    print()
    
    if not create_user_props():
        sys.exit(1)
    
    if not update_csproj():
        sys.exit(1)
    
    if not test_configuration():
        Colors.print_colored("⚠ Build failed, but properties were configured", Colors.YELLOW)
        Colors.print_colored("Try opening Visual Studio and reloading the project", Colors.YELLOW)
    
    print()
    Colors.print_colored("=== Configuration Complete ===", Colors.GREEN)
    Colors.print_colored("Now you can open Visual Studio and the DLLs should be found!", Colors.WHITE)
    Colors.print_colored("If there are still issues, close and reopen Visual Studio.", Colors.YELLOW)

if __name__ == "__main__":
    main()