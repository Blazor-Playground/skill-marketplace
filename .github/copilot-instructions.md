# Copilot Instructions — Skill Marketplace

This repository is a **skill-marketplace template**. It provides a structured way for teams to organize, share, and install Copilot CLI skills, agents, prompts, and MCP server configurations as plugin groups.

## Directory Structure

```
plugins/<name>/             # Each plugin group lives here
  plugin.json               # Plugin metadata: name, description, version, skills[], agents[]
  skills/<skill-name>/
    SKILL.md                 # Skill definition (frontmatter + markdown instructions)
  agents/<agent-name>/
    <agent-name>.agent.md    # Agent definition
.github/plugin/
  marketplace.json           # Marketplace manifest — lists all available plugins
scripts/
  skill-marketplace.cs       # Main CLI tool (C# script, run with `dotnet run`)
  sync-mirror.cs             # Mirror sync utility
template.mcp.json            # MCP server template configuration
```

## File Format Conventions

### SKILL.md

Skills use YAML frontmatter followed by markdown instructions:

```markdown
---
name: my-skill
description: A brief description of what this skill does.
---

# My Skill

Instructions for the skill go here.
```

### plugin.json

Each plugin group has a `plugin.json` describing its contents:

```json
{
  "name": "my-plugin",
  "description": "What this plugin group does",
  "version": "0.1.0",
  "skills": [
    { "name": "my-skill", "path": "skills/my-skill", "description": "Brief description" }
  ],
  "agents": []
}
```

## CLI Tool

The CLI is located at `scripts/skill-marketplace.cs`. Run it with:

```bash
dotnet run scripts/skill-marketplace.cs -- <command> [subcommand] [options]
```

Key commands: `skills`, `agents`, `plugin`, `prompts`, `instructions`, `mcp`, `settings`, `all`, `bootstrap`, `marketplace`.

Global options include `--edition`, `--target`, `--exact`, `--force`, `--dry-run`, `--verbose`, and `--backup-path`.

## Adding a New Plugin

Use the `marketplace init` subcommand to scaffold a new plugin group:

```bash
dotnet run scripts/skill-marketplace.cs -- marketplace init
```

This creates a new directory under `plugins/` with a `plugin.json` and a starter `SKILL.md`. Edit those files to define your skills.
