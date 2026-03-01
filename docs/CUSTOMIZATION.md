# Customization Guide

This guide explains how to extend and customize the skill marketplace: adding plugin groups, skills, agents, MCP server configurations, mirrors, and more.

---

## Directory Structure Overview

```
skill-marketplace/
├── .github/
│   └── plugin/
│       └── marketplace.json      # Marketplace manifest
├── plugins/                       # Plugin groups
│   ├── my-team/
│   │   ├── plugin.json            # Plugin metadata, MCP & LSP servers
│   │   ├── skills/
│   │   │   ├── code-review/
│   │   │   │   └── SKILL.md
│   │   │   └── ci-analysis/
│   │   │       └── SKILL.md
│   │   └── agents/
│   │       └── reviewer.agent.md
│   └── another-team/
│       ├── plugin.json
│       └── skills/
│           └── deploy-helper/
│               └── SKILL.md
├── prompts/                       # Shared prompt files (*.prompt.md)
├── instructions/                  # Shared instruction files (*.md)
├── template.mcp.json              # Global MCP server template
├── public-mirrors.json            # Mirror configuration
└── scripts/
    └── skill-marketplace.cs       # CLI tool
```

---

## Adding a New Plugin Group

A **plugin group** is a directory under `plugins/` that bundles related skills, agents, and server configurations.

### 1. Create the directory structure

```
plugins/my-plugin/
├── plugin.json
├── skills/
│   └── (skill directories go here)
└── agents/
    └── (agent files go here)
```

### 2. Create `plugin.json`

The `plugin.json` file describes the plugin and optionally declares MCP and LSP servers.

```json
{
  "name": "my-plugin",
  "description": "Skills and agents for my domain",
  "version": "1.0.0",
  "skills": [
    {
      "name": "my-skill",
      "path": "skills/my-skill",
      "description": "A short description of what the skill does"
    }
  ],
  "agents": [],
  "mcpServers": {
    "my-mcp-server": {
      "type": "stdio",
      "command": "node",
      "args": ["path/to/server.js"]
    }
  },
  "lspServers": {
    "my-lsp": {
      "command": "my-lsp-binary",
      "args": ["--stdio"]
    }
  }
}
```

**Fields:**

| Field | Required | Description |
|---|---|---|
| `name` | Yes | Plugin group name (matches directory name) |
| `description` | Yes | Human-readable description |
| `version` | No | Semantic version string |
| `skills` | No | Array of skill references (for documentation; discovery is automatic) |
| `agents` | No | Array of agent references |
| `mcpServers` | No | MCP server configurations (installed to tool config files) |
| `lspServers` | No | LSP server declarations (informational; managed externally) |

### 3. Verify

```bash
dotnet run --project scripts/skill-marketplace.cs plugin list my-plugin
```

---

## Adding Skills to a Plugin Group

Each skill is a **directory** under `plugins/<plugin>/skills/` containing a `SKILL.md` file.

### 1. Create the skill directory

```
plugins/my-plugin/skills/my-new-skill/
└── SKILL.md
```

The directory name becomes the skill's identifier (e.g., `my-new-skill`).

### 2. Write the `SKILL.md` file

The `SKILL.md` uses YAML frontmatter followed by Markdown instructions:

```markdown
---
name: my-new-skill
description: A concise description of what this skill does and when to use it.
---

# My New Skill

A longer explanation of the skill's purpose.

## When to Use
- Describe the scenarios where this skill is helpful
- Be specific about triggers and use cases

## Instructions

Step-by-step instructions for the AI agent:
1. First, do this...
2. Then, check that...
3. Finally, produce output like...

## When NOT to Use
- Scenarios where this skill should not be invoked
- Alternative skills to use instead
```

**Frontmatter fields:**

| Field | Required | Description |
|---|---|---|
| `name` | Yes | Skill identifier (should match directory name) |
| `description` | Yes | Short description shown in skill listings |

### 3. Additional files

A skill directory can contain any additional files the skill needs (scripts, templates, data). Only the `SKILL.md` sentinel file is required for the CLI to recognize it.

### 4. Install and test

```bash
# See the skill in the repo listing
dotnet run --project scripts/skill-marketplace.cs skills list --plugin my-plugin

# Install it
dotnet run --project scripts/skill-marketplace.cs skills install --plugin my-plugin --skill my-new-skill

# Check it's installed
dotnet run --project scripts/skill-marketplace.cs skills diff --plugin my-plugin
```

---

## Adding Agents

Agents are single `*.agent.md` files placed under `plugins/<plugin>/agents/`.

### 1. Create the agent file

```
plugins/my-plugin/agents/my-agent.agent.md
```

The file name (without `.agent.md`) becomes the agent's identifier.

### 2. Write the agent file

Agent files are Markdown documents that define the agent's behavior:

```markdown
---
name: my-agent
description: An agent that handles specific tasks for my domain.
---

# My Agent

Instructions for this custom agent.

## Capabilities
- What this agent can do
- Tools it has access to

## Behavior
- How it should respond to requests
- Constraints and guidelines
```

### 3. Install

```bash
dotnet run --project scripts/skill-marketplace.cs agents install --plugin my-plugin
```

> **Note:** Personal agents (`--scope personal`) are only supported by Copilot CLI (`~/.copilot/agents/`). For VS Code or Claude Code, use `--scope project` to install to `.github/agents/` or `.claude/agents/`.

---

## Configuring MCP Servers

MCP servers can be configured in two places:

### 1. Per-plugin MCP servers (in `plugin.json`)

Add an `mcpServers` object to a plugin's `plugin.json`:

```json
{
  "name": "my-plugin",
  "mcpServers": {
    "database-server": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@my-org/db-mcp-server"]
    },
    "api-server": {
      "type": "http",
      "url": "https://mcp.example.com/api"
    }
  }
}
```

These are installed when running `plugin install`.

### 2. Global MCP template (`template.mcp.json`)

The `template.mcp.json` at the repo root defines MCP servers that apply globally (not tied to a plugin):

```json
{
  "servers": {
    "shared-server": {
      "type": "stdio",
      "command": "node",
      "args": ["path/to/server.js"]
    }
  }
}
```

These are installed via the `mcp install` command.

### Target config locations

The CLI writes MCP server entries to the correct config file for each detected tool:

| Tool | Config File | JSON Key |
|---|---|---|
| Copilot CLI | `~/.copilot/mcp-config.json` | `mcpServers` |
| Claude Code | `~/.claude/settings.local.json` | `mcpServers` |
| VS Code (stable) | `<vscode-user-dir>/mcp.json` | `servers` |
| VS Code (insiders) | `<vscode-insiders-user-dir>/mcp.json` | `servers` |

---

## Configuring Mirrors

Mirrors allow you to replicate the marketplace into other repositories. Configure them in `public-mirrors.json` at the repo root:

```json
{
  "mirrors": [
    {
      "repo": "https://github.com/my-org/public-skills",
      "branch": "main",
      "plugins": ["shared-tools", "common-skills"],
      "description": "Public subset of our marketplace"
    }
  ]
}
```

Each mirror entry specifies a target repository and which plugin groups to include.

---

## Customizing the Marketplace Manifest

The marketplace manifest lives at `.github/plugin/marketplace.json` and registers the marketplace with VS Code's plugin system.

```json
{
  "name": "my-skill-marketplace",
  "metadata": {
    "description": "Our team's curated skill marketplace",
    "version": "1.0.0"
  },
  "owner": {
    "name": "my-org"
  },
  "plugins": [
    {
      "name": "devops",
      "source": "plugins/devops",
      "description": "DevOps and CI/CD skills",
      "version": "1.0.0"
    },
    {
      "name": "security",
      "source": "plugins/security",
      "description": "Security scanning and review skills",
      "version": "0.5.0"
    }
  ]
}
```

The `settings update` command automatically registers this marketplace in VS Code's `chat.plugins.marketplaces` setting (derived from the repo's git remote URL).

---

## Tips for Organizing Skills

### By Domain

Group skills that serve the same technical domain into a single plugin:

```
plugins/
├── frontend/        # React, CSS, accessibility skills
├── backend/         # API, database, caching skills
├── devops/          # CI/CD, deployment, monitoring skills
└── security/        # Vulnerability scanning, auth review
```

### By Team

Use plugin names that match team names or GitHub usernames:

```
plugins/
├── platform-team/   # Shared infrastructure skills
├── alice/           # Alice's personal skills
└── bob/             # Bob's personal skills
```

When multiple plugins provide a skill with the same name, the CLI resolves duplicates by preferring the plugin that matches the current GitHub user (detected via `gh api user`). This lets team members override shared skills with personal versions.

### Naming Conventions

- **Plugin names:** Use lowercase kebab-case (e.g., `dotnet-runtime`, `my-team`).
- **Skill names:** Use lowercase kebab-case matching the directory name (e.g., `code-review`, `ci-analysis`).
- **Agent names:** Use lowercase kebab-case with `.agent.md` suffix (e.g., `reviewer.agent.md`).

### Keeping Skills Focused

A good skill should:

- Have a clear, specific `description` in the frontmatter.
- Document **when to use** and **when NOT to use** it.
- Provide step-by-step instructions for the AI agent.
- Be self-contained — include any helper scripts or data it needs in the skill directory.

### Using `--dry-run` for Safety

Always preview changes before committing:

```bash
# See what would happen without changing anything
dotnet run --project scripts/skill-marketplace.cs all install --dry-run --verbose
```

### Using `--exact` for Full Sync

The `--exact` flag ensures the installed state mirrors the repo exactly — any installed items not present in the repo are removed (after backup):

```bash
dotnet run --project scripts/skill-marketplace.cs skills install --exact
```
