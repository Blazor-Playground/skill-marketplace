# Skill Conventions

These conventions apply when writing or reviewing Copilot CLI skills in this marketplace.

## Naming

- Skill directories use **lowercase kebab-case**: `my-skill-name`
- The `name` field in SKILL.md frontmatter must match the directory name
- Plugin directories also use kebab-case: `my-plugin`

## SKILL.md Quality Bar

Every skill must have:

1. **YAML frontmatter** with `name` and `description`
2. **Trigger keywords** in the description — words/phrases users would say to invoke this skill
3. **USE FOR / DO NOT USE FOR** sections to sharpen invocation boundaries
4. **Actionable instructions** — tell Copilot what to DO, not just what exists
5. **Token budget compliance** — body under 4,000 tokens; use `references/` for extras

## Agent Conventions

- Agent files are named `<agent-name>.agent.md`
- Agents declare their tools in frontmatter (e.g., `tools: [powershell]`)
- Agents handle multi-step workflows; skills handle knowledge/expertise

## Plugin Structure

```
plugins/<plugin-name>/
  plugin.json              # Manifest: name, skills/agents paths, mcpServers
  skills/
    <skill-name>/
      SKILL.md             # Skill definition
      references/          # Optional supplementary material
  agents/
    <agent-name>/
      <agent-name>.agent.md
```

## Review Checklist

Before adding a skill to the marketplace:

- [ ] Name matches directory (kebab-case)
- [ ] Description has trigger keywords
- [ ] USE FOR / DO NOT USE FOR present
- [ ] Body under 4K tokens
- [ ] At least one example
- [ ] plugin.json updated
- [ ] marketplace.json updated
- [ ] `marketplace readme` run to update README
