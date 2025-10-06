#!/usr/bin/env python3
"""
ONI Together - Development Tools
Useful commands for mod development
Works on Windows and Linux
"""

import os
import sys
import subprocess
import platform
import time
from pathlib import Path

class Colors:
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    RED = '\033[91m'
    WHITE = '\033[97m'
    BLUE = '\033[94m'
    GRAY = '\033[90m'
    RESET = '\033[0m'
    
    @staticmethod
    def print_colored(text, color):
        print(f"{color}{text}{Colors.RESET}")

def show_help():
    """Show help for available commands"""
    Colors.print_colored("=== ONI Together - Development Tools ===", Colors.GREEN)
    print()
    Colors.print_colored("Available commands:", Colors.YELLOW)
    Colors.print_colored("  python dev-tools.py build [--clean] [--install]  - Compile project", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py install                      - Install compiled mod", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py clean                        - Clean build files", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py info                         - Show project information", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py open-mods                    - Open ONI mods folder", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py open-game                    - Open game folder", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py watch-logs                   - Monitor game logs", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py test-deps                    - Check dependencies", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py setup                        - Configure environment", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py configure-vs                 - Configure Visual Studio", Colors.WHITE)
    print()
    Colors.print_colored("Examples:", Colors.YELLOW)
    Colors.print_colored("  python dev-tools.py build --install              - Compile and install", Colors.WHITE)
    Colors.print_colored("  python dev-tools.py build --clean --install      - Clean, compile and install", Colors.WHITE)
    print()

def run_build(args):
    """Executa o script de build"""
    cmd = ['python', 'build.py'] + args
    return subprocess.run(cmd).returncode == 0

def install_mod():
    """Instala o mod"""
    return run_build(['--install'])

def clean_build():
    """Limpa build"""
    return run_build(['--clean'])

def show_info():
    """Mostra informações"""
    return run_build(['--info'])

def open_mods_folder():
    """Open mods folder"""
    Colors.print_colored("Opening mods folder...", Colors.YELLOW)
    
    if platform.system() == "Windows":
        documents_path = Path.home() / "Documents"
        mods_path = documents_path / "Klei" / "OxygenNotIncluded" / "mods" / "local"
    else:  # Linux
        home = Path.home()
        possible_paths = [
            home / ".klei" / "OxygenNotIncluded" / "mods" / "local",
            home / "Documents" / "Klei" / "OxygenNotIncluded" / "mods" / "local",
            home / ".local" / "share" / "Klei" / "OxygenNotIncluded" / "mods" / "local"
        ]
        
        mods_path = None
        for path in possible_paths:
            if path.exists():
                mods_path = path
                break
        
        if not mods_path:
            mods_path = possible_paths[0]
    
    if mods_path.exists():
        if platform.system() == "Windows":
            os.startfile(str(mods_path))
        else:  # Linux
            subprocess.run(['xdg-open', str(mods_path)])
        Colors.print_colored(f"✓ Folder opened: {mods_path}", Colors.GREEN)
    else:
        Colors.print_colored(f"✗ Mods folder not found: {mods_path}", Colors.RED)

def open_game_folder():
    """Open game folder"""
    Colors.print_colored("Opening game folder...", Colors.YELLOW)
    
    game_dir = os.environ.get('ONI_GAME_DIR')
    if game_dir and Path(game_dir).exists():
        if platform.system() == "Windows":
            os.startfile(game_dir)
        else:  # Linux
            subprocess.run(['xdg-open', game_dir])
        Colors.print_colored(f"✓ Folder opened: {game_dir}", Colors.GREEN)
    else:
        Colors.print_colored("✗ ONI_GAME_DIR not configured or folder does not exist", Colors.RED)
        Colors.print_colored("Run: python setup-dev-environment.py", Colors.YELLOW)

def watch_logs():
    """Monitor game logs"""
    Colors.print_colored("Monitoring ONI logs...", Colors.YELLOW)
    Colors.print_colored("Press Ctrl+C to stop", Colors.GRAY)
    
    if platform.system() == "Windows":
        documents_path = Path.home() / "Documents"
        log_path = documents_path / "Klei" / "OxygenNotIncluded" / "Player.log"
    else:  # Linux
        home = Path.home()
        possible_paths = [
            home / ".klei" / "OxygenNotIncluded" / "Player.log",
            home / "Documents" / "Klei" / "OxygenNotIncluded" / "Player.log",
            home / ".local" / "share" / "Klei" / "OxygenNotIncluded" / "Player.log"
        ]
        
        log_path = None
        for path in possible_paths:
            if path.exists():
                log_path = path
                break
    
    if not log_path or not log_path.exists():
        Colors.print_colored(f"✗ Log file not found: {log_path}", Colors.RED)
        return
    
    try:
        if platform.system() == "Windows":
            # No Windows, usar PowerShell Get-Content -Wait
            subprocess.run(['powershell', '-Command', 
                          f'Get-Content "{log_path}" -Wait -Tail 10'])
        else:  # Linux
            subprocess.run(['tail', '-f', str(log_path)])
    except KeyboardInterrupt:
        Colors.print_colored("\nMonitoring interrupted", Colors.YELLOW)
    except Exception as e:
        Colors.print_colored(f"✗ Error monitoring logs: {e}", Colors.RED)

def test_dependencies():
    """Check dependencies"""
    Colors.print_colored("=== Checking Dependencies ===", Colors.GREEN)
    
    # .NET SDK
    try:
        result = subprocess.run(['dotnet', '--version'], 
                              capture_output=True, text=True, check=True)
        version = result.stdout.strip()
        Colors.print_colored(f"✓ .NET SDK {version}", Colors.GREEN)
    except (subprocess.CalledProcessError, FileNotFoundError):
        Colors.print_colored("✗ .NET SDK not found", Colors.RED)
    
    # .NET Framework (Windows)
    if platform.system() == "Windows":
        try:
            import winreg
            key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, 
                               r"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full")
            release, _ = winreg.QueryValueEx(key, "Release")
            winreg.CloseKey(key)
            
            if release >= 461808:
                Colors.print_colored("✓ .NET Framework 4.7.2+", Colors.GREEN)
            else:
                Colors.print_colored("✗ .NET Framework 4.7.2+ required", Colors.RED)
        except Exception:
            Colors.print_colored("✗ .NET Framework not found", Colors.RED)
    
    # Game Directory
    game_dir = os.environ.get('ONI_GAME_DIR')
    if game_dir and Path(game_dir).exists():
        Colors.print_colored("✓ ONI_GAME_DIR configured", Colors.GREEN)
        
        # Verificar DLLs
        managed_path = Path(game_dir) / "OxygenNotIncluded_Data" / "Managed"
        if managed_path.exists():
            required_dlls = [
                "Assembly-CSharp.dll",
                "0Harmony.dll",
                "com.rlabrecque.steamworks.net.dll",
                "UnityEngine.dll"
            ]
            
            missing = []
            for dll in required_dlls:
                if not (managed_path / dll).exists():
                    missing.append(dll)
            
            if not missing:
                Colors.print_colored("✓ All required DLLs found", Colors.GREEN)
            else:
                Colors.print_colored("⚠ DLLs not found:", Colors.YELLOW)
                for dll in missing:
                    Colors.print_colored(f"  - {dll}", Colors.RED)
    else:
        Colors.print_colored("✗ ONI_GAME_DIR not configured", Colors.RED)

def setup_environment():
    """Run environment setup"""
    Colors.print_colored("Running environment setup...", Colors.YELLOW)
    return subprocess.run(['python', 'setup-dev-environment.py']).returncode == 0

def configure_visual_studio():
    """Configure Visual Studio"""
    Colors.print_colored("Configuring Visual Studio...", Colors.YELLOW)
    return subprocess.run(['python', 'configure-vs.py']).returncode == 0

def main():
    if len(sys.argv) < 2:
        show_help()
        return
    
    command = sys.argv[1].lower()
    
    if command == 'help' or command == '--help' or command == '-h':
        show_help()
    elif command == 'build':
        run_build(sys.argv[2:])
    elif command == 'install':
        install_mod()
    elif command == 'clean':
        clean_build()
    elif command == 'info':
        show_info()
    elif command == 'open-mods':
        open_mods_folder()
    elif command == 'open-game':
        open_game_folder()
    elif command == 'watch-logs':
        watch_logs()
    elif command == 'test-deps':
        test_dependencies()
    elif command == 'setup':
        setup_environment()
    elif command == 'configure-vs':
        configure_visual_studio()
    else:
        Colors.print_colored(f"Unknown command: {command}", Colors.RED)
        show_help()

if __name__ == "__main__":
    main()