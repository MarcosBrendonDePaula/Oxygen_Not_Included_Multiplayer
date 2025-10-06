# Changelog - ONI Together Development Environment

## [1.3.0] - 2025-10-06

### Changed
- **Internationalization**: All Python scripts translated to English
  - `setup-dev-environment.py` - All messages in English
  - `build.py` - Interface and messages in English  
  - `dev-tools.py` - Commands and outputs in English
  - `configure-vs.py` - VS configuration in English

### Improved
- **User Experience**: Consistent English interface across all tools
- **International Collaboration**: Ready for global contributors
- **Documentation**: All guides and references in English

## [1.2.0] - 2025-10-06

### Added
- **Visual Studio Configuration**: Added `configure-vs.py` script for automatic VS setup
- **Documentation Structure**: Organized documentation in `docs/` folder
- **API Reference**: Added comprehensive API documentation
- **Visual Studio Setup Guide**: Detailed VS configuration instructions
- **MSBuild Properties**: Added `Directory.Build.props` and `ONI_MP.props` for better IDE support

### Changed
- **Documentation Language**: Converted documentation to English
- **Project Structure**: Moved documentation to `docs/` folder
- **README**: Updated with new documentation structure

### Fixed
- **Visual Studio DLL Resolution**: Fixed issue where VS couldn't find game DLLs
- **Cross-Platform Compatibility**: Improved path handling for different platforms

## [1.1.0] - 2025-10-06

### Changed
- **BREAKING CHANGE**: Renamed environment variable from `GAME_DIR` to `ONI_GAME_DIR`
  - More specific and avoids conflicts with other projects
  - Updated all scripts and documentation
  - Updated .csproj file to use new variable

### Removed
- Removed problematic PowerShell scripts
- Cleaned up temporary files and build artifacts

### Added
- Added `.gitignore` file for better project hygiene
- Added `CHANGELOG.md` to track changes

### Fixed
- Fixed environment variable conflicts
- Improved cross-platform compatibility

## [1.0.0] - 2025-10-06

### Added
- Initial Python-based development environment
- `setup-dev-environment.py` - Automatic environment configuration
- `build.py` - Build and installation script
- `dev-tools.py` - Development utilities
- `DESENVOLVIMENTO.md` - Complete development guide
- Cross-platform support (Windows/Linux)
- Automatic ONI detection and configuration
- Build automation and mod installation
- Log monitoring and dependency checking

### Features
- ✅ Automatic ONI game detection
- ✅ Environment variable configuration
- ✅ Dependency verification
- ✅ One-command build and install
- ✅ Development tools and utilities
- ✅ Cross-platform compatibility
- ✅ Visual Studio integration
- ✅ Comprehensive documentation