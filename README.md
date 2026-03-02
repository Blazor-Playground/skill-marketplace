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

## Installing from Your Marketplace

Once you've published plugins, others can install them. Replace `<owner>/<repo>` with your marketplace's GitHub path.

### GitHub Copilot CLI

```
/plugin marketplace add <owner>/<repo>
/plugin marketplace browse <marketplace-name>
/plugin install <plugin-name>@<marketplace-name>
/plugin list
```

Or install directly from GitHub:
```
/plugin install <owner>/<repo>:plugins/<plugin-name>
```

List and manage installed skills:
```
/skills list
/skills        # toggle on/off with arrow keys + spacebar
/skills reload # pick up newly added skills
```

### Claude Code

```
/plugin marketplace add <owner>/<repo>
/plugin   # → go to Discover tab
/plugin install <plugin-name>@<marketplace-name>
```

### VS Code / VS Code Insiders (Preview)

```jsonc
// settings.json
{
  "chat.plugins.enabled": true,
  "chat.plugins.marketplaces": ["<owner>/<repo>"]
}
```

Once configured, type `/plugins` in Copilot Chat to browse and install plugins from the marketplace.

### CLI Tool

```powershell
# Requires .NET 10 SDK
dotnet run scripts/skill-marketplace.cs -- all list      # see what's in the repo
dotnet run scripts/skill-marketplace.cs -- all install    # install everything locally
dotnet run scripts/skill-marketplace.cs -- skills diff    # compare repo vs installed
dotnet run scripts/skill-marketplace.cs -- all install --exact  # full sync
```

## Repository Structure

```
.
├── .github/
│   ├── agents/                    # Copilot agents for multi-step workflows
│   │   └── marketplace-manager.agent.md
│   ├── copilot-instructions.md    # Tells Copilot how to work in this repo
│   ├── plugin/
│   │   └── marketplace.json       # Marketplace manifest — lists all plugins
│   ├── skills/                    # Project-level skills
│   │   └── skill-authoring/
│   │       └── SKILL.md
│   └── workflows/                 # CI/CD workflows
├── docs/
│   ├── CLI-REFERENCE.md           # Full CLI command reference
│   └── CUSTOMIZATION.md           # Guide to customizing your marketplace
├── instructions/                  # Instruction files (installed via CLI)
│   └── skill-conventions.md
├── plugins/
│   └── sample/                    # Example plugin
│       ├── plugin.json            # Plugin metadata and skill list
│       ├── skills/
│       │   ├── hello-world/
│       │   │   └── SKILL.md
│       │   └── greeting-customizer/
│       │       ├── SKILL.md
│       │       └── references/
│       │           └── locale-patterns.md
│       └── agents/
│           └── greeter/
│               └── greeter.agent.md
├── schemas/                       # JSON validation schemas
├── scripts/
│   ├── skill-marketplace.cs       # Main CLI tool
│   └── sync-mirror.cs             # Mirror sync utility
└── template.mcp.json              # MCP template configuration
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
| `skills <list\|install\|uninstall\|diff>` | Manage agent skills (SKILL.md folders) |
| `agents <list\|install\|uninstall\|diff>` | Manage custom agents (.agent.md) |
| `plugin <install\|list\|diff\|uninstall> [name]` | Manage plugin groups (skills + agents + MCP + LSP) |
| `prompts <list\|install\|uninstall\|diff>` | Manage prompt files (.prompt.md) |
| `instructions <list\|install\|uninstall\|diff>` | Manage instruction files |
| `mcp <list\|install\|uninstall\|diff>` | Manage MCP server configuration |
| `settings <list\|update\|diff>` | Manage VS Code settings |
| `all <list\|install\|uninstall\|diff>` | Bulk operations across all categories |
| `bootstrap` | Clone repo, install all assets, and clean up |
| `marketplace init` | Scaffold a new plugin group with a first skill |
| `marketplace readme` | Auto-generate the README plugin/skill table |

Run any command with:

```bash
dotnet run scripts/skill-marketplace.cs -- <command> [subcommand] [options]
```

## Documentation

- **[CUSTOMIZATION.md](docs/CUSTOMIZATION.md)** — Customize the marketplace for your team
- **[CLI-REFERENCE.md](docs/CLI-REFERENCE.md)** — Full CLI command reference

## Uninstall

```
# Copilot CLI / Claude Code
/plugin uninstall <plugin-name>@<marketplace-name>

# VS Code — remove the entry from chat.plugins.marketplaces in settings.json
```

## License

[MIT](LICENSE)
