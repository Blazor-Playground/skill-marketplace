# Skill Marketplace

A GitHub template repository for managing and distributing Copilot CLI skills as structured plugin marketplaces.

## What Is This?

Skill Marketplace provides a ready-made structure for teams to organize, share, and install Copilot CLI skills. Click **"Use this template"** to create your own marketplace, then add plugins containing the skills your team needs.

Each marketplace ships with:

- A **manifest** (`.github/plugin/marketplace.json`) describing available plugins
- A **plugin system** where each plugin groups related skills under `plugins/<name>/`
- A **CLI tool** (`scripts/skill-marketplace.cs`) for installing, syncing, and scaffolding plugins

## Quick Start

1. Click **Use this template** on GitHub to create your own marketplace repo.
2. Clone your new repository:
   ```bash
   git clone https://github.com/<your-org>/<your-marketplace>.git
   cd <your-marketplace>
   ```
3. Install the marketplace skills into your environment:
   ```bash
   dotnet run scripts/skill-marketplace.cs -- install
   ```

## Repository Structure

```
.
├── .github/
│   ├── plugin/
│   │   └── marketplace.json      # Marketplace manifest — lists all plugins
│   └── workflows/                # CI/CD workflows
├── docs/
│   ├── CLI-REFERENCE.md          # Full CLI command reference
│   └── CUSTOMIZATION.md          # Guide to customizing your marketplace
├── plugins/
│   └── sample/                   # Example plugin
│       ├── plugin.json           # Plugin metadata and skill list
│       └── skills/
│           └── hello-world/
│               └── SKILL.md      # Skill definition
├── scripts/
│   ├── skill-marketplace.cs      # Main CLI tool
│   └── sync-mirror.cs            # Mirror sync utility
└── template.mcp.json             # MCP template configuration
```

## Creating Your First Plugin

Scaffold a new plugin with the CLI:

```bash
dotnet run scripts/skill-marketplace.cs -- marketplace init
```

This creates a plugin directory under `plugins/` with a `plugin.json` and a starter `SKILL.md`. Edit the generated files to define your skill's name, description, and instructions.

A `plugin.json` looks like this:

```json
{
  "name": "my-plugin",
  "description": "What this plugin group does",
  "version": "0.1.0",
  "skills": [
    {
      "name": "my-skill",
      "path": "skills/my-skill",
      "description": "A brief description"
    }
  ],
  "agents": []
}
```

## Available Commands

| Command | Description |
|---------|-------------|
| `install` | Install marketplace skills into your environment |
| `marketplace init` | Scaffold a new plugin with starter files |
| `sync` | Sync skills from a remote marketplace mirror |
| `list` | List all available plugins and skills |

Run any command with:

```bash
dotnet run scripts/skill-marketplace.cs -- <command>
```

## Documentation

- **[CUSTOMIZATION.md](docs/CUSTOMIZATION.md)** — Customize the marketplace for your team
- **[CLI-REFERENCE.md](docs/CLI-REFERENCE.md)** — Full CLI command reference

## License

[MIT](LICENSE)
