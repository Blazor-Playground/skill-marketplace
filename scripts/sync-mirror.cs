#!/usr/bin/env dotnet
#:package System.CommandLine@*

#pragma warning disable CS8604

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

// ============================================================================
// Configuration
// ============================================================================

var repoRoot = GetRepoRoot();
var manifestPath = Path.Combine(repoRoot, "public-mirrors.json");

// ============================================================================
// CLI
// ============================================================================

var mirrorOption = new Option<string?>("--mirror") { Description = "Sync only the named mirror" };
var outputOption = new Option<string?>("--output") { Description = "Output directory (default: temp)" };
var dryRunOption = new Option<bool>("--dry-run") { Description = "Build output but don't push" };
var pushOption = new Option<bool>("--push") { Description = "Clone, commit, and push to target repos" };

var rootCommand = new RootCommand("Sync allowed skills to public mirror repos")
{
    mirrorOption, outputOption, dryRunOption, pushOption
};
rootCommand.SetAction(pr => RunSync(
    pr.GetValue(mirrorOption),
    pr.GetValue(outputOption),
    pr.GetValue(dryRunOption),
    pr.GetValue(pushOption)));

return rootCommand.Parse(args).Invoke();

// ============================================================================
// Core
// ============================================================================

void RunSync(string? mirrorName, string? outputDir, bool dryRun, bool push)
{
    if (!File.Exists(manifestPath))
    {
        PrintError($"public-mirrors.json not found at {manifestPath}");
        return;
    }

    var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!;
    var sourceCommit = RunCapture("git", $"-C \"{repoRoot}\" rev-parse --short HEAD") ?? "unknown";
    outputDir ??= Path.Combine(Path.GetTempPath(), "copilot-mirror-sync");

    var mirrors = manifest["mirrors"]!.AsArray();
    if (mirrorName != null)
    {
        mirrors = new JsonArray(mirrors
            .Where(m => m!["name"]!.GetValue<string>() == mirrorName)
            .Select(m => m!.DeepClone())
            .ToArray());
        if (mirrors.Count == 0)
        {
            PrintError($"Mirror '{mirrorName}' not found");
            return;
        }
    }

    foreach (var mirror in mirrors)
    {
        var name = mirror!["name"]!.GetValue<string>();
        var targetRepo = mirror["targetRepo"]!.GetValue<string>();
        var description = mirror["description"]?.GetValue<string>() ?? "";
        var plugins = mirror["plugins"]!.AsObject();

        PrintHeader($"=== {name} ===");
        Console.WriteLine($"  Target: {targetRepo}");

        var mirrorDir = Path.Combine(outputDir, name);

        // Clone or create fresh output
        if (push)
        {
            if (Directory.Exists(mirrorDir)) ForceDeleteDirectory(mirrorDir);
            Console.WriteLine($"  Cloning {targetRepo}...");
            if (Run("gh", $"repo clone {targetRepo} \"{mirrorDir}\"") != 0)
            {
                Console.WriteLine("  Creating repo...");
                Run("gh", $"repo create {targetRepo} --public --description \"{description}\"");
                Directory.CreateDirectory(mirrorDir);
                Run("git", $"-C \"{mirrorDir}\" init");
                Run("git", $"-C \"{mirrorDir}\" remote add origin https://github.com/{targetRepo}.git");
            }
            var existingPlugins = Path.Combine(mirrorDir, "plugins");
            if (Directory.Exists(existingPlugins)) Directory.Delete(existingPlugins, true);
        }
        else
        {
            if (Directory.Exists(mirrorDir)) ForceDeleteDirectory(mirrorDir);
            Directory.CreateDirectory(mirrorDir);
        }

        var marketplacePlugins = new JsonArray();
        var pluginNames = plugins.Select(p => p.Key).ToList();
        var totalSkills = 0;
        var totalAgents = 0;
        var totalMcp = 0;
        var totalLsp = 0;
        var totalHooks = 0;

        foreach (var (pluginName, skillSelectionNode) in plugins)
        {
            var srcPlugin = Path.Combine(repoRoot, "plugins", pluginName);
            var dstPlugin = Path.Combine(mirrorDir, "plugins", pluginName);

            if (!Directory.Exists(srcPlugin))
            {
                PrintWarning($"  Plugin '{pluginName}' not found — skipping");
                continue;
            }

            var srcJson = JsonNode.Parse(File.ReadAllText(Path.Combine(srcPlugin, "plugin.json")))!;

            // Determine allowed skills
            List<string> allowed;
            if (skillSelectionNode is JsonValue v && v.GetValue<string>() == "*")
            {
                var skillsDir = Path.Combine(srcPlugin, "skills");
                allowed = Directory.Exists(skillsDir)
                    ? Directory.GetDirectories(skillsDir).Select(Path.GetFileName).ToList()!
                    : new List<string>();
            }
            else
            {
                allowed = skillSelectionNode!.AsArray().Select(s => s!.GetValue<string>()).ToList();
            }

            Console.WriteLine($"  {pluginName}: {string.Join(", ", allowed)}");

            // Copy skill directories
            foreach (var skill in allowed)
            {
                var srcSkill = Path.Combine(srcPlugin, "skills", skill);
                var dstSkill = Path.Combine(dstPlugin, "skills", skill);
                if (Directory.Exists(srcSkill))
                    CopyDirectoryRecursive(srcSkill, dstSkill);
                else
                    PrintWarning($"    Skill '{skill}' not found — skipping");
            }
            totalSkills += allowed.Count;

            // Copy agents/ if present
            var srcAgents = Path.Combine(srcPlugin, "agents");
            if (Directory.Exists(srcAgents))
            {
                CopyDirectoryRecursive(srcAgents, Path.Combine(dstPlugin, "agents"));
                totalAgents += Directory.GetFiles(srcAgents, "*.agent.md").Length;
            }

            // Copy hooks/ if present
            var srcHooks = Path.Combine(srcPlugin, "hooks");
            if (Directory.Exists(srcHooks))
            {
                CopyDirectoryRecursive(srcHooks, Path.Combine(dstPlugin, "hooks"));
                totalHooks++;
            }

            // Write filtered plugin.json
            var filtered = new JsonObject
            {
                ["name"] = srcJson["name"]?.DeepClone(),
                ["version"] = srcJson["version"]?.DeepClone(),
                ["description"] = srcJson["description"]?.DeepClone(),
                ["author"] = new JsonObject { ["name"] = targetRepo.Split('/')[0] },
                ["license"] = srcJson["license"]?.DeepClone(),
                ["keywords"] = srcJson["keywords"]?.DeepClone(),
                ["skills"] = new JsonArray(allowed.Order().Select(s => JsonValue.Create($"skills/{s}")).ToArray())
            };
            if (srcJson["mcpServers"] != null)
            {
                filtered["mcpServers"] = srcJson["mcpServers"]!.DeepClone();
                totalMcp += srcJson["mcpServers"]!.AsObject().Count;
            }
            if (srcJson["lspServers"] != null)
            {
                filtered["lspServers"] = srcJson["lspServers"]!.DeepClone();
                totalLsp += srcJson["lspServers"]!.AsObject().Count;
            }
            if (srcJson["agents"] != null)
                filtered["agents"] = srcJson["agents"]!.DeepClone();
            if (srcJson["hooks"] != null)
                filtered["hooks"] = srcJson["hooks"]!.DeepClone();

            // Both Copilot CLI and Claude Code read .claude-plugin/plugin.json
            WriteJson(Path.Combine(dstPlugin, ".claude-plugin", "plugin.json"), filtered);

            // Write per-plugin README.md
            var mktName = name;
            var pluginReadmeLines = new List<string>
            {
                $"# {pluginName}", "",
                srcJson["description"]?.GetValue<string>() ?? "", "",
                "## Installation", "",
                "### Copilot CLI / Claude Code", "",
                "Via marketplace:",
                "```",
                $"/plugin marketplace add {targetRepo}",
                $"/plugin install {pluginName}@{mktName}",
                $"/plugin update {pluginName}@{mktName}",
                "```", "",
                "Or install directly from GitHub (Copilot CLI only):",
                "```",
                $"/plugin install {targetRepo}:plugins/{pluginName}",
                "```", "",
                "### VS Code (Preview)", "",
                $"Add the marketplace to your VS Code settings:", "",
                "```jsonc",
                "// settings.json",
                "{",
                $"  \"chat.plugins.enabled\": true,",
                $"  \"chat.plugins.marketplaces\": [\"{targetRepo}\"]",
                "}",
                "```", "",
                "Then use `/plugins` in Copilot Chat to browse and install.", "",
                "## Uninstall", "",
                "```",
                $"# Copilot CLI / Claude Code",
                $"/plugin uninstall {pluginName}@{mktName}",
                "",
                "# VS Code: remove the marketplace entry from chat.plugins.marketplaces in settings.json",
                "```", ""
            };
            if (allowed.Count > 0)
            {
                pluginReadmeLines.Add("## Skills");
                pluginReadmeLines.Add("");
            }
            foreach (var skill in allowed.Order())
            {
                var skillMd = Path.Combine(dstPlugin, "skills", skill, "SKILL.md");
                if (File.Exists(skillMd))
                {
                    var (sName, sDesc) = ReadSkillFrontmatter(skillMd);
                    pluginReadmeLines.Add($"### [{sName ?? skill}](skills/{skill}/SKILL.md)");
                    pluginReadmeLines.Add("");
                    if (sDesc != null) { pluginReadmeLines.Add(sDesc); pluginReadmeLines.Add(""); }

                    // List reference docs if present
                    var refsDir = Path.Combine(dstPlugin, "skills", skill, "references");
                    if (Directory.Exists(refsDir))
                    {
                        pluginReadmeLines.Add("**References:**");
                        foreach (var refFile in Directory.GetFiles(refsDir, "*.md").Order())
                            pluginReadmeLines.Add($"- [{Path.GetFileName(refFile)}](skills/{skill}/references/{Path.GetFileName(refFile)})");
                        pluginReadmeLines.Add("");
                    }
                }
            }

            // Document MCP servers if present, with links to plugin.json lines
            if (filtered["mcpServers"] is JsonObject mcpServers && mcpServers.Count > 0)
            {
                var mcpLineRanges = FindJsonBlockRanges(Path.Combine(dstPlugin, ".claude-plugin", "plugin.json"), mcpServers.Select(kv => kv.Key));

                pluginReadmeLines.Add("## MCP Servers");
                pluginReadmeLines.Add("");
                pluginReadmeLines.Add("This plugin configures the following [MCP servers](https://modelcontextprotocol.io/) automatically when installed:");
                pluginReadmeLines.Add("");
                foreach (var (mcpName, mcpConfig) in mcpServers)
                {
                    var mcpType = mcpConfig!["type"]?.GetValue<string>();
                    var cmd = mcpConfig["command"]?.GetValue<string>();
                    var summary = mcpType == "http"
                        ? mcpConfig["url"]?.GetValue<string>() ?? "HTTP endpoint"
                        : cmd != null ? $"`{cmd}` tool" : "local tool";
                    var nameLink = mcpLineRanges.TryGetValue(mcpName, out var range)
                        ? $"[{mcpName}](.claude-plugin/plugin.json#L{range.start}-L{range.end})"
                        : mcpName;
                    pluginReadmeLines.Add($"- **{nameLink}** — {summary}");
                }
                pluginReadmeLines.Add("");
            }

            // Document agents if present
            if (filtered["agents"] is JsonArray agentsList && agentsList.Count > 0)
            {
                pluginReadmeLines.Add("## Agents");
                pluginReadmeLines.Add("");
                foreach (var agentPath in agentsList)
                {
                    var agentFile = agentPath!.GetValue<string>();
                    var agentName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(agentFile)); // strip .agent.md
                    var agentFullPath = Path.Combine(dstPlugin, agentFile);
                    var agentDesc = "";
                    if (File.Exists(agentFullPath))
                    {
                        var (_, desc) = ReadSkillFrontmatter(agentFullPath);
                        agentDesc = desc ?? "";
                    }
                    pluginReadmeLines.Add($"### [{agentName}]({agentFile})");
                    pluginReadmeLines.Add("");
                    if (!string.IsNullOrEmpty(agentDesc))
                    {
                        pluginReadmeLines.Add(agentDesc);
                        pluginReadmeLines.Add("");
                    }
                }
            }

            // Document LSP servers if present
            if (filtered["lspServers"] is JsonObject lspServers && lspServers.Count > 0)
            {
                var lspLineRanges = FindJsonBlockRanges(Path.Combine(dstPlugin, ".claude-plugin", "plugin.json"), lspServers.Select(kv => kv.Key));

                pluginReadmeLines.Add("## LSP Servers");
                pluginReadmeLines.Add("");
                pluginReadmeLines.Add("This plugin configures the following language servers:");
                pluginReadmeLines.Add("");
                foreach (var (lspName, lspConfig) in lspServers)
                {
                    var cmd = lspConfig!["command"]?.GetValue<string>() ?? "language server";
                    var langs = lspConfig["extensionToLanguage"]?.AsObject()
                        .Select(kv => kv.Value!.GetValue<string>()).Distinct().Order().ToList();
                    var langInfo = langs?.Count > 0 ? $" — languages: {string.Join(", ", langs)}" : "";
                    var nameLink = lspLineRanges.TryGetValue(lspName, out var range)
                        ? $"[{lspName}](.claude-plugin/plugin.json#L{range.start}-L{range.end})"
                        : lspName;
                    pluginReadmeLines.Add($"- **{nameLink}** — `{cmd}`{langInfo}");
                }
                pluginReadmeLines.Add("");
            }

            // Document hooks if present
            if (filtered["hooks"] != null)
            {
                pluginReadmeLines.Add("## Hooks");
                pluginReadmeLines.Add("");
                pluginReadmeLines.Add("This plugin includes lifecycle hooks. See [hooks/](hooks/) for configuration.");
                pluginReadmeLines.Add("");
            }

            File.WriteAllText(Path.Combine(dstPlugin, "README.md"), string.Join("\n", pluginReadmeLines));

            var mktEntry = new JsonObject
            {
                ["name"] = pluginName,
                ["source"] = $"./plugins/{pluginName}",
                ["description"] = srcJson["description"]?.DeepClone(),
                ["version"] = srcJson["version"]?.DeepClone()
            };
            marketplacePlugins.Add(mktEntry);
        }

        // Write marketplace.json to .claude-plugin/ (both Copilot CLI and Claude Code read this location)
        var mktDir = Path.Combine(mirrorDir, ".claude-plugin");
        Directory.CreateDirectory(mktDir);
        WriteJson(Path.Combine(mktDir, "marketplace.json"), new JsonObject
        {
            ["name"] = name,
            ["metadata"] = new JsonObject
            {
                ["description"] = description,
                ["version"] = "0.1.0",
                ["sourceCommit"] = sourceCommit
            },
            ["owner"] = new JsonObject { ["name"] = targetRepo.Split('/')[0] },
            ["plugins"] = marketplacePlugins
        });

        // Copy CLI script as scripts/plugin-cli.cs
        var cliSrc = Path.Combine(repoRoot, "scripts", "blazor-ai.cs");
        if (File.Exists(cliSrc))
        {
            var cliDstDir = Path.Combine(mirrorDir, "scripts");
            Directory.CreateDirectory(cliDstDir);
            File.Copy(cliSrc, Path.Combine(cliDstDir, "plugin-cli.cs"), true);

            // Generate repo-contributor skill
            var skillDir = Path.Combine(mirrorDir, ".github", "skills", "repo-contributor");
            Directory.CreateDirectory(skillDir);
            File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), $"""
                ---
                name: repo-contributor
                description: >
                  Guide contributions to this plugin marketplace repository.
                  USE FOR: installing skills, syncing assets, diffing installed vs repo,
                  checking what's out of date, "how do I install a skill", "sync my skills",
                  "what's out of date", "how do I use the CLI".
                  DO NOT USE FOR: building skills from scratch (use skill-builder).
                ---

                # Contributing to {targetRepo.Split('/').Last()}

                This repo is a plugin marketplace. Use the included CLI tool to install and manage skills.

                ## CLI Tool

                Run with `dotnet scripts/plugin-cli.cs -- <command>`.

                > **Prerequisite:** .NET 10 SDK (or later).

                ### Commands

                Each asset category (`skills`, `agents`, `prompts`, `instructions`, `mcp`, `settings`) supports:

                | Subcommand | What it does |
                |------------|--------------|
                | `list` | Show what's installed vs what's in the repo |
                | `install` | Copy from repo → installed location |
                | `uninstall` | Remove repo-managed assets from installed location |
                | `diff` | Compare repo vs installed — shows missing, extra, and changed files |

                Use `all` to operate across every category: `all list`, `all install`, `all diff`.

                ### Common Workflows

                ```powershell
                # See what's out of sync
                dotnet scripts/plugin-cli.cs -- skills diff --verbose

                # Install a specific skill
                dotnet scripts/plugin-cli.cs -- skills install --skill ci-analysis --force

                # Install everything from the repo
                dotnet scripts/plugin-cli.cs -- all install --force

                # Dry-run to preview changes
                dotnet scripts/plugin-cli.cs -- all install --dry-run

                # Full sync: install repo assets AND remove extras not in repo
                dotnet scripts/plugin-cli.cs -- all install --exact
                ```

                ### Key Options

                | Option | Effect |
                |--------|--------|
                | `--skill <name>` | Filter to one skill |
                | `--plugin <name>` | Filter to one plugin group |
                | `--scope personal\|project` | Install to `~/.copilot/skills/` (default) or `.github/skills/` |
                | `--force` | Overwrite without prompting |
                | `--exact` | Full sync — removes installed files not in repo (after backup) |
                | `--dry-run` | Preview without changes |
                | `--verbose` | Show detailed output |

                > 💡 After editing a skill, run `skills install --skill <name> --force` to update your local copy.

                ## Adding a Skill

                Every new skill requires updates to **4 locations**:

                | Step | File | Action |
                |------|------|--------|
                | 1 | `plugins/<group>/skills/<name>/SKILL.md` | Create with `name` + `description` in YAML frontmatter |
                | 2 | `plugins/<group>/plugin.json` | Add path to `"skills"` array |
                | 3 | `.claude-plugin/marketplace.json` | Ensure plugin group is listed |
                | 4 | `README.md` | Verify skill appears in Available Plugins |
                """.ReplaceLineEndings("\n"));
        }

        // Write README
        var marketplaceName = name;
        var badgePlugins = $"![plugins](https://img.shields.io/badge/plugins-{pluginNames.Count}-blue)";
        var badgeSkills = $"![skills](https://img.shields.io/badge/skills-{totalSkills}-green)";
        var badgeAgents = totalAgents > 0
            ? $" ![agents](https://img.shields.io/badge/agents-{totalAgents}-purple)"
            : "";
        var badgeMcp = totalMcp > 0
            ? $" ![MCP servers](https://img.shields.io/badge/MCP_servers-{totalMcp}-yellow)"
            : "";
        var badgeLsp = totalLsp > 0
            ? $" ![LSP servers](https://img.shields.io/badge/LSP_servers-{totalLsp}-orange)"
            : "";
        var badgeHooks = totalHooks > 0
            ? $" ![hooks](https://img.shields.io/badge/hooks-{totalHooks}-red)"
            : "";
        var badgeCopilot = "![GitHub Copilot](https://img.shields.io/badge/GitHub_Copilot-compatible-black?logo=github)";
        var badgeClaude = "![Claude Code](https://img.shields.io/badge/Claude_Code-compatible-cc785c?logo=anthropic)";
        var lines = new List<string>
        {
            $"# {targetRepo.Split('/')[0]}/agent-plugins", "",
            $"{badgePlugins} {badgeSkills}{badgeAgents}{badgeMcp}{badgeLsp}{badgeHooks}", "",
            $"{badgeCopilot} {badgeClaude}", "",
            description, "",
            "A **plugin marketplace** for [Copilot Agent Skills](https://docs.github.com/en/copilot/concepts/agents/about-agent-skills). Install plugins to get skills and MCP server configs automatically.",
            "", "## Installation", "",
            "### Claude Code", "",
            "```",
            $"/plugin marketplace add {targetRepo}",
            $"/plugin   # → go to Discover tab",
            $"/plugin install <plugin-name>@{marketplaceName}",
            $"/plugin update <plugin-name>@{marketplaceName}",
            "```", "",
            "### GitHub Copilot CLI", "",
            "Via marketplace:",
            "```",
            $"/plugin marketplace add {targetRepo}",
            $"/plugin marketplace browse {marketplaceName}",
            $"/plugin install <plugin-name>@{marketplaceName}",
            $"/plugin update <plugin-name>@{marketplaceName}",
            $"/plugin list",
            "```", "",
            "Or install directly from GitHub:",
            "```",
            $"/plugin install {targetRepo}:plugins/<plugin-name>",
            "```", "",
            "List and manage installed skills:",
            "```",
            "/skills list",
            "/skills        # toggle on/off with arrow keys + spacebar",
            "/skills reload # pick up newly added skills",
            "```", "",
            "### VS Code / VS Code Insiders (Preview)", "",
            "> **Note:** VS Code plugin support is a preview feature. You may need to enable it first.", "",
            "```jsonc",
            "// settings.json",
            "{",
            $"  \"chat.plugins.enabled\": true,",
            $"  \"chat.plugins.marketplaces\": [\"{targetRepo}\"]",
            "}", "",
            "```", "",
            "Once configured, type `/plugins` in Copilot Chat to browse and install plugins from the marketplace.", "",
            "## Uninstall", "",
            "```",
            "# Copilot CLI / Claude Code",
            $"/plugin uninstall <plugin-name>@{marketplaceName}",
            "",
            "# VS Code — remove the entry from chat.plugins.marketplaces in settings.json",
            "```",
            "", "## CLI Tool", "",
            "This repo includes a standalone CLI for managing installed skills, agents, and MCP configs outside of the editor plugin commands.", "",
            "```powershell",
            "# Requires .NET 10 SDK",
            "dotnet scripts/plugin-cli.cs -- all list      # see what's installed",
            "dotnet scripts/plugin-cli.cs -- all install    # install everything",
            "dotnet scripts/plugin-cli.cs -- skills diff    # compare repo vs installed",
            "dotnet scripts/plugin-cli.cs -- all install --exact  # full sync",
            "```", "",
            "Run `dotnet scripts/plugin-cli.cs -- --help` for all commands and options.", "",
            "## Available Plugins", ""
        };
        foreach (var entry in marketplacePlugins)
        {
            var pn = entry!["name"]!.GetValue<string>();
            var sel = plugins[pn];
            lines.Add($"### [{pn}](plugins/{pn}/)");
            lines.Add("");
            lines.Add(entry["description"]?.GetValue<string>() ?? "");
            lines.Add("");

            var skillNames = (sel is JsonValue sv && sv.GetValue<string>() == "*")
                ? new List<string> { "(all)" }
                : sel!.AsArray().Select(s => s!.GetValue<string>()).Order().ToList();

            if (skillNames.Count > 0)
            {
                lines.Add("| Skill | References |");
                lines.Add("|-------|------------|");
                foreach (var skill in skillNames)
                {
                    var refsDir = Path.Combine(mirrorDir, "plugins", pn, "skills", skill, "references");
                    var refLinks = "";
                    if (Directory.Exists(refsDir))
                    {
                        refLinks = string.Join(", ", Directory.GetFiles(refsDir, "*.md").Order()
                            .Select(f => $"[{Path.GetFileNameWithoutExtension(f)}](plugins/{pn}/skills/{skill}/references/{Path.GetFileName(f)})"));
                    }
                    lines.Add($"| [{skill}](plugins/{pn}/skills/{skill}/SKILL.md) | {refLinks} |");
                }
                lines.Add("");
            }

            // List LSP servers if present
            var dstPluginJson = Path.Combine(mirrorDir, "plugins", pn, "plugin.json");
            if (File.Exists(dstPluginJson))
            {
                var pjson = JsonNode.Parse(File.ReadAllText(dstPluginJson));
                if (pjson?["lspServers"] is JsonObject lspBlock && lspBlock.Count > 0)
                {
                    lines.Add("**LSP Servers:** " + string.Join(", ", lspBlock.Select(kv => kv.Key)));
                    lines.Add("");
                }
                if (pjson?["hooks"] != null)
                {
                    lines.Add("**Hooks:** [hooks/](plugins/" + pn + "/hooks/)");
                    lines.Add("");
                }
            }

            // List agents if present
            var agentsDir = Path.Combine(mirrorDir, "plugins", pn, "agents");
            if (Directory.Exists(agentsDir))
            {
                var agentFiles = Directory.GetFiles(agentsDir, "*.agent.md").Order().ToList();
                if (agentFiles.Count > 0)
                {
                    lines.Add("**Agents:** " + string.Join(", ",
                        agentFiles.Select(f => $"[{Path.GetFileNameWithoutExtension(f)}](plugins/{pn}/agents/{Path.GetFileName(f)})")));
                    lines.Add("");
                }
            }
        }
        File.WriteAllText(Path.Combine(mirrorDir, "README.md"), string.Join("\n", lines));

        PrintSuccess($"\n  Output: {mirrorDir}");
        Console.WriteLine($"  Plugins: {pluginNames.Count}, Skills: {totalSkills}, Agents: {totalAgents}, MCP: {totalMcp}, LSP: {totalLsp}, Hooks: {totalHooks}");

        // Commit and push
        if (push)
        {
            Run("git", $"-C \"{mirrorDir}\" add -A");
            if (Run("git", $"-C \"{mirrorDir}\" diff --cached --quiet") != 0)
            {
                Run("git", $"-C \"{mirrorDir}\" commit -m \"Sync from source @ {sourceCommit}\" -m \"Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>\"");
                Run("git", $"-C \"{mirrorDir}\" push origin HEAD");
                PrintSuccess($"  Pushed to {targetRepo}");
            }
            else
            {
                PrintWarning("  No changes to push");
            }
        }
        else if (dryRun)
        {
            PrintWarning($"  [DRY RUN] Inspect output at {mirrorDir}");
        }
    }

    PrintHeader("\nDone.");
}

// ============================================================================
// Helpers
// ============================================================================

string GetRepoRoot([System.Runtime.CompilerServices.CallerFilePath] string? callerPath = null)
{
    var scriptsDir = Path.GetDirectoryName(callerPath)
        ?? throw new InvalidOperationException("Could not determine script directory.");
    return Path.GetDirectoryName(scriptsDir)
        ?? throw new InvalidOperationException("Could not determine repo root.");
}

void ForceDeleteDirectory(string path)
{
    // Git marks .git/objects as read-only; Directory.Delete fails on Windows
    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
    {
        var attr = File.GetAttributes(file);
        if (attr.HasFlag(FileAttributes.ReadOnly))
            File.SetAttributes(file, attr & ~FileAttributes.ReadOnly);
    }
    Directory.Delete(path, true);
}

void CopyDirectoryRecursive(string source, string target)
{
    Directory.CreateDirectory(target);
    foreach (var file in Directory.GetFiles(source))
        File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
    foreach (var dir in Directory.GetDirectories(source))
        CopyDirectoryRecursive(dir, Path.Combine(target, Path.GetFileName(dir)));
}

void WriteJson(string path, JsonNode node)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
}

Dictionary<string, (int start, int end)> FindJsonBlockRanges(string filePath, IEnumerable<string> keys)
{
    var lines = File.ReadAllLines(filePath);
    var ranges = new Dictionary<string, (int start, int end)>();
    for (int i = 0; i < lines.Length; i++)
    {
        var trimmed = lines[i].TrimStart();
        foreach (var key in keys)
        {
            if (!trimmed.StartsWith($"\"{key}\":", StringComparison.Ordinal)) continue;
            int startLine = i + 1;
            int depth = 0;
            for (int j = i; j < lines.Length; j++)
            {
                depth += lines[j].Count(c => c == '{') - lines[j].Count(c => c == '}');
                if (depth <= 0) { ranges[key] = (startLine, j + 1); break; }
            }
        }
    }
    return ranges;
}

(string? name, string? description) ReadSkillFrontmatter(string skillMdPath)
{
    string? name = null, description = null;
    var content = File.ReadAllLines(skillMdPath);
    if (content.Length < 2 || content[0].Trim() != "---") return (null, null);
    for (int i = 1; i < content.Length; i++)
    {
        if (content[i].Trim() == "---") break;
        if (content[i].StartsWith("name:")) name = content[i]["name:".Length..].Trim();
        if (content[i].StartsWith("description:"))
        {
            var inline = content[i]["description:".Length..].Trim();
            if (inline is ">" or "|" or ">-" or "|-")
            {
                // YAML folded/literal block — collect indented continuation lines
                var parts = new List<string>();
                for (int j = i + 1; j < content.Length; j++)
                {
                    if (content[j].Trim() == "---" || (content[j].Length > 0 && content[j][0] != ' ')) break;
                    parts.Add(content[j].Trim());
                }
                description = string.Join(" ", parts).Trim();
            }
            else
                description = inline;
        }
    }
    return (StripYamlQuotes(name), StripYamlQuotes(description));
}

string? StripYamlQuotes(string? s) =>
    s is { Length: >= 2 } && ((s[0] == '\'' && s[^1] == '\'') || (s[0] == '"' && s[^1] == '"'))
        ? s[1..^1] : s;

int Run(string fileName, string arguments)
{
    var psi = new ProcessStartInfo { FileName = fileName, Arguments = arguments, UseShellExecute = false };
    using var process = Process.Start(psi)!;
    process.WaitForExit();
    return process.ExitCode;
}

string? RunCapture(string fileName, string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = fileName, Arguments = arguments,
        UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
    };
    using var process = Process.Start(psi)!;
    var output = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();
    return process.ExitCode == 0 ? output : null;
}

void PrintHeader(string text) { Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine(text); Console.ResetColor(); }
void PrintSuccess(string text) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(text); Console.ResetColor(); }
void PrintWarning(string text) { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine(text); Console.ResetColor(); }
void PrintError(string text) { Console.ForegroundColor = ConsoleColor.Red; Console.Error.WriteLine(text); Console.ResetColor(); }
