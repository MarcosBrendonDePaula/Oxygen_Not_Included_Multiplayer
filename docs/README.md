# ONI Together - Documentation

Welcome to the ONI Together mod documentation. This folder contains all the guides and references you need for development.

## Getting Started

1. **[Development Guide](DEVELOPMENT.md)** - Start here for complete setup instructions
2. **[Visual Studio Setup](VISUAL_STUDIO_SETUP.md)** - Configure your IDE

## Reference

- **[API Reference](API_REFERENCE.md)** - Code documentation and examples
- **[Changelog](CHANGELOG.md)** - Version history and changes

## Quick Setup

For a quick start, run these commands in order:

```bash
# 1. Set up development environment
python setup-dev-environment.py

# 2. Configure Visual Studio (if using)
python configure-vs.py

# 3. Build and install the mod
python build.py --install
```

## Development Workflow

```bash
# Make code changes in ClassLibrary1/

# Build and test
python build.py --install

# Monitor logs while testing
python dev-tools.py watch-logs
```

## Common Issues

### Visual Studio can't find DLLs
```bash
python configure-vs.py
# Then restart Visual Studio
```

### Environment not configured
```bash
python setup-dev-environment.py
```

### Build errors
```bash
python dev-tools.py test-deps
python build.py --clean --install
```

## File Structure

```
docs/
├── README.md              # This file
├── DEVELOPMENT.md         # Complete development guide
├── VISUAL_STUDIO_SETUP.md # Visual Studio configuration
├── API_REFERENCE.md       # Code documentation
└── CHANGELOG.md           # Version history
```

## Need Help?

1. Check the [Development Guide](DEVELOPMENT.md) for detailed instructions
2. Run `python dev-tools.py test-deps` to check your setup
3. Look at the [API Reference](API_REFERENCE.md) for code examples
4. Check the [Changelog](CHANGELOG.md) for recent changes