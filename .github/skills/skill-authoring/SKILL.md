---
name: skill-authoring
description: Best practices for writing Copilot CLI skills. Covers SKILL.md format, frontmatter conventions, trigger keywords, context budgets, and anti-patterns. USE FOR: writing new skills, improving existing skill definitions, reviewing skill quality. DO NOT USE FOR: managing the marketplace structure (use marketplace-manager agent), running CLI commands.
---

# Skill Authoring Guide

You are helping a user write or improve a Copilot CLI skill definition (SKILL.md). Follow these best practices.

## SKILL.md Structure

Every skill file has two parts:

### 1. YAML Frontmatter

```yaml
---
name: my-skill-name
description: Brief description with trigger keywords. USE FOR: specific scenarios. DO NOT USE FOR: excluded scenarios.
---
```

**Rules:**
- `name`: lowercase kebab-case, matches the directory name
- `description`: one to three sentences. Must include enough context for Copilot to decide when to invoke this skill
- Include "USE FOR:" and "DO NOT USE FOR:" to sharpen invocation accuracy
- Keep the description under 200 characters for the core sentence, then elaborate with USE FOR/DO NOT USE FOR

### 2. Markdown Body

The body contains the actual instructions Copilot follows when the skill is invoked.

```markdown
# Skill Title

Brief introduction of what this skill does and when it applies.

## When to Use

- Scenario A
- Scenario B

## How to Execute

Step-by-step instructions for the most common workflow.

## Important Rules

- Constraint 1
- Constraint 2

## Examples

Show concrete input → output examples when helpful.
```

## Trigger Keywords

The `description` field is how Copilot decides whether to invoke your skill. Include keywords users might say:

- **Good:** "Analyze CI build and test status. USE FOR: checking CI status, investigating failures, 'why is CI red', 'test failures', 'is CI green'."
- **Bad:** "A skill for CI." (too vague — Copilot won't know when to trigger it)

Think about what the user will literally type, and include those phrases.

## Context Budget

Skills are injected into Copilot's context window. Keep them efficient:

- **Target:** 2,000–4,000 tokens for the skill body
- **Maximum:** 6,000 tokens (beyond this, you're crowding out conversation context)
- **Use `references/`:** Put supplementary material (schemas, examples, long lists) in a `references/` subdirectory. Reference them in the skill body but don't inline everything.

```
plugins/my-plugin/skills/my-skill/
  SKILL.md              # Core instructions (under 4K tokens)
  references/
    schema.md           # Detailed schema docs
    examples.md         # Extended examples
    anti-patterns.md    # Common mistakes
```

## Skills vs Agents

| | Skill | Agent |
|---|---|---|
| **Purpose** | Knowledge and domain expertise | Multi-step workflows with tool use |
| **Format** | `SKILL.md` | `<name>.agent.md` |
| **Tools** | None (pure instructions) | Can declare tool access (powershell, etc.) |
| **Location** | `skills/<name>/SKILL.md` | `agents/<name>/<name>.agent.md` |
| **Use when** | Teaching Copilot a domain or convention | Copilot needs to execute commands, read/write files |

## Anti-Patterns

❌ **Wall of text** — Dumping entire documentation into SKILL.md. Use references/ instead.

❌ **No trigger keywords** — Description says "A utility skill" with no indication of when to invoke.

❌ **Too broad** — One skill that tries to cover everything. Split into focused skills.

❌ **Imperative without context** — "Run `npm test`" without explaining when or why.

❌ **Missing DO NOT USE FOR** — Without exclusions, Copilot may invoke the skill for unrelated requests.

## Checklist for a Good Skill

- [ ] `name` matches directory name (kebab-case)
- [ ] `description` has trigger keywords and USE FOR / DO NOT USE FOR
- [ ] Body is under 4,000 tokens
- [ ] Instructions are actionable (tell Copilot what to DO, not just what exists)
- [ ] Supplementary material in `references/` if needed
- [ ] At least one concrete example of expected behavior
