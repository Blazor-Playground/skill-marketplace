# Copilot Instructions — Skill Marketplace

This repository is a **skill marketplace** — a structured catalog of Copilot CLI skills, agents, prompts, and MCP server configurations organized as plugin groups. Skills live in `plugins/<name>/skills/<skill>/SKILL.md`, agents in `plugins/<name>/agents/`, and the marketplace manifest at `.github/plugin/marketplace.json` ties it all together.

## Marketplace Commands (Use These First)

For most tasks, use the `marketplace` CLI subcommands. These are the primary tools:

| Command | What it does |
|---------|-------------|
| `dotnet run scripts/skill-marketplace.cs -- marketplace init` | Scaffold a new plugin + skill interactively |
| `dotnet run scripts/skill-marketplace.cs -- marketplace readme` | Auto-generate the README plugin/skill table from marketplace.json |

### When the user asks...

**"I want to add a skill" / "Create a new skill for X"**
1. Run `marketplace init` — it will prompt for plugin name, skill name, and descriptions
2. Edit the generated `SKILL.md` at `plugins/<plugin>/skills/<skill>/SKILL.md`
3. Run `marketplace readme` to update the catalog table
4. Commit the changes

**"Add a skill to an existing plugin"**
1. Create the skill directory: `plugins/<plugin>/skills/<new-skill>/`
2. Write `SKILL.md` with YAML frontmatter (name, description) and markdown instructions
3. Update `plugins/<plugin>/plugin.json` — add the skill to the `skills` array
4. Run `marketplace readme` to refresh the README table

**"What skills are available?" / "What's in this repo?"**
1. Run `dotnet run scripts/skill-marketplace.cs -- skills list` to see all discovered skills
2. Or check `README.md` for the generated table
3. Or read `.github/plugin/marketplace.json` for the full manifest

**"Help me write a good SKILL.md"**
1. Use the `@skill-authoring` skill if available (`.github/skills/skill-authoring/SKILL.md`)
2. Key rules: YAML frontmatter with `name` and `description`, markdown body with USE FOR / DO NOT USE FOR sections, keep under 4K tokens, include trigger keywords in the description

**"Update the README" / "Refresh the catalog"**
1. Run `marketplace readme` to regenerate the plugin/skill table from marketplace.json
2. Review the output and commit

**"Add an agent"**
1. Create `plugins/<plugin>/agents/<agent-name>/`
2. Write `<agent-name>.agent.md` with agent instructions
3. Update `plugins/<plugin>/plugin.json` — add to the `agents` array
4. Run `marketplace readme` to refresh the README

**"Validate the marketplace structure"**
1. Run `dotnet run scripts/skill-marketplace.cs -- skills list` — errors indicate structural issues
2. Check that every plugin in marketplace.json has a matching `plugins/<name>/plugin.json`
3. Check that every skill in plugin.json has a matching `SKILL.md`

## Advanced: Full CLI

The script at `scripts/skill-marketplace.cs` has many more commands for power-user scenarios:

| Category | Commands | Use for |
|----------|----------|---------|
| `skills` | `list`, `install`, `uninstall`, `diff` | Cross-repo skill installs, comparing versions |
| `agents` | `list`, `install`, `uninstall`, `diff` | Cross-repo agent management |
| `plugin` | `list`, `install`, `uninstall` | Install/remove entire plugin groups |
| `instructions` | `list`, `install`, `uninstall` | Manage repo instruction files |
| `prompts` | `list`, `install`, `uninstall`, `diff` | Custom prompt management |
| `mcp` | `list`, `install`, `uninstall` | MCP server configurations |
| `settings` | `list`, `install`, `uninstall`, `diff` | VS Code settings management |
| `all` | `list`, `install`, `uninstall` | Bulk operations across all categories |
| `bootstrap` | *(no subcommand)* | Full first-time setup from a remote marketplace |

**Use these when:** importing skills from another marketplace repo, syncing mirrors, doing bulk installs, debugging configuration issues, or managing personal vs project-level installs.

See `docs/CLI-REFERENCE.md` for the full command reference.

## Conventions

- **SKILL.md format:** YAML frontmatter (`name`, `description`) + markdown body
- **Plugin naming:** lowercase kebab-case (e.g., `code-quality`, `devops-tools`)
- **Skill descriptions:** include trigger keywords so Copilot knows when to invoke them. Include "USE FOR:" and "DO NOT USE FOR:" sections.
- **Context budget:** keep skill instructions under 4,000 tokens. Use a `references/` subdirectory for supplementary material.
- **Agents vs Skills:** agents orchestrate multi-step workflows with tools; skills provide knowledge and domain expertise.
- **JSON schemas:** `schemas/plugin.schema.json` and `schemas/marketplace.schema.json` validate structure.

## Consumer Installation

When the user asks "how do people install from my marketplace?" or "how do I share this?":

- **Copilot CLI / Claude Code:** `/plugin marketplace add <owner>/<repo>` then `/plugin install <plugin>@<marketplace>`
- **VS Code:** Add to `settings.json`: `"chat.plugins.marketplaces": ["<owner>/<repo>"]`
- **CLI Tool:** `dotnet run scripts/skill-marketplace.cs -- bootstrap --edition <owner>/<repo>`

See the README "Installing from Your Marketplace" section for full details.
