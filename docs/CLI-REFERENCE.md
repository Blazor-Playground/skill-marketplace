# CLI Reference — `skill-marketplace.cs`

> **Plugin marketplace asset manager** — Installs, syncs, and manages skills, agents, prompts, instructions, MCP servers, and VS Code settings across Copilot CLI, Claude Code, and VS Code.

## Running the Script

```bash
dotnet run --project scripts/skill-marketplace.cs [command] [subcommand] [options]
```

---

## Global Options

These options apply to all commands:

| Option | Description | Values | Default |
|---|---|---|---|
| `--edition` | Target VS Code edition | `insiders`, `stable`, `both` | `both` |
| `--target` | Install target tool(s) | `auto`, `copilot`, `claude`, `vscode`, `all` | `auto` |
| `--exact` | Full sync: remove target files/entries not in repo (after backup) | flag | `false` |
| `--force` | Overwrite existing files without prompting | flag | `false` |
| `--dry-run` | Show what would be done without making changes | flag | `false` |
| `--verbose` | Show detailed output | flag | `false` |
| `--backup-path` | Custom backup directory | path | `<repo-root>/backup` |

### Target Detection (`--target`)

- **`auto`** (default) — Detects which tools are installed by checking for `~/.copilot/` and `~/.claude/` directories.
- **`copilot`** — Only install to Copilot CLI (`~/.copilot/`).
- **`claude`** — Only install to Claude Code (`~/.claude/`).
- **`vscode`** — Install to VS Code via Copilot CLI directory.
- **`all`** — Install to both Copilot CLI and Claude Code.

---

## Commands

### `skills` — Manage Agent Skills

Skills are directory-based assets containing a `SKILL.md` sentinel file.

**Additional options:**

| Option | Description |
|---|---|
| `--plugin` | Filter to a specific plugin group (e.g., `dotnet-runtime`) |
| `--skill` | Filter to a specific skill by name (e.g., `ci-analysis`) |
| `--scope` | Install target: `personal` (`~/.copilot/skills/`) or `project` (`.github/skills/`) — default: `personal` |

#### `skills list`

List skills available in the repo and currently installed.

```bash
dotnet run --project scripts/skill-marketplace.cs skills list
dotnet run --project scripts/skill-marketplace.cs skills list --plugin sample --verbose
dotnet run --project scripts/skill-marketplace.cs skills list --scope project
```

#### `skills install`

Install skills from the repo to the target directory.

```bash
dotnet run --project scripts/skill-marketplace.cs skills install
dotnet run --project scripts/skill-marketplace.cs skills install --force
dotnet run --project scripts/skill-marketplace.cs skills install --plugin sample --skill hello-world
dotnet run --project scripts/skill-marketplace.cs skills install --exact --dry-run
dotnet run --project scripts/skill-marketplace.cs skills install --target claude --scope personal
```

#### `skills uninstall`

Remove repo-managed skills from the target directory.

```bash
dotnet run --project scripts/skill-marketplace.cs skills uninstall
dotnet run --project scripts/skill-marketplace.cs skills uninstall --plugin sample --dry-run
```

#### `skills diff`

Compare repo skills vs installed skills, showing items that are added (`+`), removed (`-`), or modified (`~`).

```bash
dotnet run --project scripts/skill-marketplace.cs skills diff
dotnet run --project scripts/skill-marketplace.cs skills diff --plugin sample
```

---

### `agents` — Manage Custom Agents

Agents are file-based assets matching `*.agent.md`.

**Additional options:**

| Option | Description |
|---|---|
| `--plugin` | Filter to a specific plugin group |
| `--scope` | `personal` (`~/.copilot/agents/`) or `project` (`.github/agents/`) — default: `personal` |

> **Note:** Only Copilot CLI supports personal agents. VS Code and Claude Code use project-level agents only.

#### `agents list`

```bash
dotnet run --project scripts/skill-marketplace.cs agents list
dotnet run --project scripts/skill-marketplace.cs agents list --plugin my-plugin
```

#### `agents install`

```bash
dotnet run --project scripts/skill-marketplace.cs agents install
dotnet run --project scripts/skill-marketplace.cs agents install --force --verbose
dotnet run --project scripts/skill-marketplace.cs agents install --scope project
```

#### `agents uninstall`

```bash
dotnet run --project scripts/skill-marketplace.cs agents uninstall
```

#### `agents diff`

```bash
dotnet run --project scripts/skill-marketplace.cs agents diff
```

---

### `plugin` — Manage Plugin Groups

A plugin group bundles skills, agents, MCP servers, and LSP servers into a single installable unit. Each plugin lives under `plugins/<name>/`.

#### `plugin list [name]`

Show plugins and their contents (skills, agents, MCP/LSP servers). Omit `name` to list all plugins.

```bash
dotnet run --project scripts/skill-marketplace.cs plugin list
dotnet run --project scripts/skill-marketplace.cs plugin list sample
```

#### `plugin install [name]`

Install all assets from a plugin group (skills, agents, MCP servers). Omit `name` to install all plugins.

```bash
dotnet run --project scripts/skill-marketplace.cs plugin install sample
dotnet run --project scripts/skill-marketplace.cs plugin install --force --dry-run
dotnet run --project scripts/skill-marketplace.cs plugin install sample --scope project
```

#### `plugin diff [name]`

Compare plugin assets: repo vs installed.

```bash
dotnet run --project scripts/skill-marketplace.cs plugin diff
dotnet run --project scripts/skill-marketplace.cs plugin diff sample --verbose
```

#### `plugin uninstall [name]`

Remove a plugin's installed assets (skills, agents, MCP servers).

```bash
dotnet run --project scripts/skill-marketplace.cs plugin uninstall sample
dotnet run --project scripts/skill-marketplace.cs plugin uninstall --dry-run
```

---

### `prompts` — Manage Prompt Files

Prompts are `*.prompt.md` files stored in the `prompts/` repo directory and synced to the VS Code user prompts directory.

#### `prompts list`

```bash
dotnet run --project scripts/skill-marketplace.cs prompts list
dotnet run --project scripts/skill-marketplace.cs prompts list --edition insiders
```

#### `prompts install`

```bash
dotnet run --project scripts/skill-marketplace.cs prompts install
dotnet run --project scripts/skill-marketplace.cs prompts install --exact --force
```

#### `prompts uninstall`

```bash
dotnet run --project scripts/skill-marketplace.cs prompts uninstall
```

#### `prompts diff`

```bash
dotnet run --project scripts/skill-marketplace.cs prompts diff
```

---

### `instructions` — Manage Instruction Files

Instruction files are `*.md` files from the `instructions/` repo directory. They are installed to `~/.copilot-instructions/instructions/` and concatenated into `~/.copilot/copilot-instructions.md` for the Copilot CLI.

#### `instructions list`

```bash
dotnet run --project scripts/skill-marketplace.cs instructions list
```

#### `instructions install`

Installs instructions and ensures the `chat.instructionFilesLocations` VS Code setting points to the target directory.

```bash
dotnet run --project scripts/skill-marketplace.cs instructions install
dotnet run --project scripts/skill-marketplace.cs instructions install --exact --force
```

#### `instructions uninstall`

```bash
dotnet run --project scripts/skill-marketplace.cs instructions uninstall
```

#### `instructions diff`

```bash
dotnet run --project scripts/skill-marketplace.cs instructions diff
```

---

### `mcp` — Manage MCP Server Configuration

Manages MCP server entries defined in `template.mcp.json`. Servers are merged into the appropriate config file for each detected target tool.

**Target config file locations:**

| Target | Config Path | JSON Key |
|---|---|---|
| Copilot CLI | `~/.copilot/mcp-config.json` | `mcpServers` |
| Claude Code | `~/.claude/settings.local.json` | `mcpServers` |
| VS Code | `<user-dir>/mcp.json` | `servers` |

#### `mcp list`

```bash
dotnet run --project scripts/skill-marketplace.cs mcp list
dotnet run --project scripts/skill-marketplace.cs mcp list --target copilot
```

#### `mcp install`

Merge template MCP servers into user config files.

```bash
dotnet run --project scripts/skill-marketplace.cs mcp install
dotnet run --project scripts/skill-marketplace.cs mcp install --exact
dotnet run --project scripts/skill-marketplace.cs mcp install --target vscode --edition insiders
```

#### `mcp uninstall`

Remove template MCP servers from user config files.

```bash
dotnet run --project scripts/skill-marketplace.cs mcp uninstall
```

#### `mcp diff`

```bash
dotnet run --project scripts/skill-marketplace.cs mcp diff
```

---

### `settings` — Manage VS Code Settings

Ensures required VS Code settings are present for Copilot chat functionality. Also registers the marketplace repo in `chat.plugins.marketplaces`.

**Required settings managed:**

| Setting | Value |
|---|---|
| `chat.plugins.enabled` | `true` |
| `chat.useAgentSkills` | `true` |
| `chat.useNestedAgentsMdFiles` | `true` |
| `chat.customAgentInSubagent.enabled` | `true` |
| `chat.instructionFilesLocations` | `{ "$HOME/.copilot-instructions/instructions": true }` |

#### `settings list`

List current `chat.*` and `github.copilot.*` settings.

```bash
dotnet run --project scripts/skill-marketplace.cs settings list
dotnet run --project scripts/skill-marketplace.cs settings list --edition stable
```

#### `settings update`

Ensure required settings are present (adds missing ones, does not remove extras).

```bash
dotnet run --project scripts/skill-marketplace.cs settings update
dotnet run --project scripts/skill-marketplace.cs settings update --dry-run
```

#### `settings diff`

Show missing or different settings compared to the required set.

```bash
dotnet run --project scripts/skill-marketplace.cs settings diff
```

---

### `all` — Bulk Operations

Run operations across all asset categories at once: plugins (skills + agents + MCP), prompts, instructions, MCP (from template), and settings.

#### `all list`

```bash
dotnet run --project scripts/skill-marketplace.cs all list
```

#### `all install`

```bash
dotnet run --project scripts/skill-marketplace.cs all install
dotnet run --project scripts/skill-marketplace.cs all install --force --dry-run
```

#### `all uninstall`

```bash
dotnet run --project scripts/skill-marketplace.cs all uninstall
```

#### `all diff`

```bash
dotnet run --project scripts/skill-marketplace.cs all diff
```

---

### `bootstrap` — One-Shot Setup

Clones the repo to a temporary directory, installs all assets (skills, agents, prompts, instructions, MCP, settings), then cleans up the clone. Useful for first-time setup without keeping the repo checked out.

```bash
dotnet run --project scripts/skill-marketplace.cs bootstrap
dotnet run --project scripts/skill-marketplace.cs bootstrap --dry-run
dotnet run --project scripts/skill-marketplace.cs bootstrap --edition insiders
```

- Uses `--force` implicitly for all installs.
- Backups default to `<cwd>/backup` (since the temp clone is deleted).
- Override with `--backup-path`.

---

## Scope & Target Behavior

### Scope (`--scope`)

| Scope | Skills Directory | Agents Directory |
|---|---|---|
| `personal` | `~/.copilot/skills/` (or `~/.claude/skills/`) | `~/.copilot/agents/` |
| `project` | `.github/skills/` (or `.claude/skills/`) | `.github/agents/` (or `.claude/agents/`) |

### Duplicate Resolution

When multiple plugins provide a skill/agent with the same name, the CLI resolves duplicates by:

1. Checking the current GitHub user (via `gh api user`).
2. If a plugin matches the user's GitHub username, that plugin wins.
3. Otherwise, the duplicate is skipped with a warning — use `--plugin` to select one.

---

## Backups

All destructive operations (overwrite, remove, `--exact` sync) create timestamped backups:

```
<backup-path>/<timestamp>/<category>/
```

- Default backup path: `<repo-root>/backup/`
- Override with `--backup-path <dir>`
- Timestamp format: `yyyy-MM-dd_HHmmss`
- Categories: `skills`, `agents`, `prompts`, `instructions`, `mcp`, `settings`
