---
name: check-bin-obj-clash
description: 'Detects MSBuild projects with conflicting OutputPath or IntermediateOutputPath and fixes the clash. Use when builds fail with ''Cannot create a file when that file already exists'' or ''The process cannot access the file because it is being used by another process'', fail intermittently but pass on retry, or overwrite outputs in multi-project or multi-targeting builds where bin/obj or project.assets.json collide. Not for OS-level file locking, single-project single-TFM builds, or non-MSBuild build systems.'
license: MIT
---
<!-- cratis-ai-managed: skills/check-bin-obj-clash/SKILL.md -->

<!-- Modified by Cratis from dotnet's original (MIT): frontmatter, applicability, navigation and the changes listed in NOTICE. Upstream: https://github.com/dotnet/skills (plugins/dotnet-msbuild/skills/check-bin-obj-clash/SKILL.md; see NOTICE) -->

# Detecting OutputPath and IntermediateOutputPath Clashes

Cratis rules in `.cratis/ai/rules/` (including `csharp.md`, `specs.csharp.md`, and `orleans.md`) and the `cratis-*` skills win where they differ.

## Overview

This skill helps identify when multiple MSBuild project evaluations share the same `OutputPath` or `IntermediateOutputPath`. This is a common source of build failures including:

- File access conflicts during parallel builds
- Missing or overwritten output files
- Intermittent build failures
- "File in use" errors
- **NuGet restore errors like `Cannot create a file when that file already exists`** - this strongly indicates multiple projects share the same `IntermediateOutputPath` where `project.assets.json` is written

Clashes can occur between:
- **Different projects** sharing the same output directory
- **Multi-targeting builds** (e.g., `TargetFrameworks=net8.0;net9.0`) where the path doesn't include the target framework
- **Multiple solution builds** where the same project is built from different solutions in a single build

**Cratis qualification:** `BuildProjectReferences=false` only suppresses building referenced projects; it does not prove that this project instance is read-only. Exclude an instance only when its actual targets and writes show a metadata-only query (for example, `GetTargetPath`).

## When to Use This Skill

**Invoke this skill immediately when you see:**
- `Cannot create a file when that file already exists` during NuGet restore
- `The process cannot access the file because it is being used by another process`
- Intermittent build failures that succeed on retry
- Missing output files or unexpected overwriting

## Step 1: Generate a Binary Log

Use the `binlog-generation` skill to generate a binary log with the correct naming convention.

## Primary workflow — binlog MCP

The MCP server exposes structured tools for inspecting a `.binlog` without
parsing text logs. Call them directly instead of replaying the binlog to a text
file. Call `tools/list` for the MCP first if you are unsure which tools are available.

**Important constraints:**
- The `.binlog` file is a **binary format** — do NOT try to `cat`, `head`, `strings`, or read it directly. Use only the MCP tools to query it.
- **Synthesize findings as you go.** Do not spend all available time investigating — once you have enough evidence, present your conclusions.

### Step 2: Get an overview and list projects

Use the MCP overview and projects tools to understand the build and list all projects that participated.

### Step 3: Check evaluations and global properties

Use the MCP `evaluations` and `evaluation_global_properties` tools to find all evaluations per project. Look for:
- Multiple evaluations for the same project (indicates multi-targeting or multiple build configurations)
- Differing global properties between evaluations (`TargetFramework`, `Configuration`, `RuntimeIdentifier`, `SolutionFileName`, `PublishReadyToRun`, etc.)

### Step 4: Get output paths for each evaluation

Use the MCP properties tool to query `OutputPath`, `IntermediateOutputPath`, `BaseOutputPath`, and `BaseIntermediateOutputPath` for each project evaluation.

### Step 5: Check for double writes

Use the MCP double_writes tool if available — it directly detects files written by multiple project instances.

### Step 6: Identify clashes

Compare the `OutputPath` and `IntermediateOutputPath` values across all evaluations:
1. **Normalize paths** - Convert to absolute paths and normalize separators
2. **Group by path** - Find evaluations that share the same OutputPath or IntermediateOutputPath
3. **Filter out non-build evaluations** - Exclude only instances proven by their targets and writes to be metadata-only queries; `BuildProjectReferences=false` alone is insufficient.
4. **Report clashes** - Any group with more than one evaluation indicates a clash

## Fallback workflow — text-log replay (when MCP is unavailable)

Use this only when the MCP server cannot be started.

Replay the binlog to a diagnostic text log, then grep for the same signals the MCP tools surface:

```bash
dotnet msbuild build.binlog -noconlog -fl "-flp:v=diag;logfile=full.log"
```

Then extract the clash signals:

- **Projects & evaluations** — list evaluation starts and count them per project: `grep 'Evaluation started' full.log | grep -oiE '"[^"]+\.[a-z]+proj"' | sort | uniq -c`. Matching the full **quoted path** keeps same-named projects in different directories distinct (and tolerates spaces in paths); a path with a count ≥ 2 was evaluated more than once (multi-targeting or extra global properties). (`grep -c` alone only totals evaluations across the whole log, so it can't reveal per-project duplication.)
- **Output paths** — `grep -iE 'OutputPath[[:space:]]*=|IntermediateOutputPath[[:space:]]*=|BaseOutputPath[[:space:]]*=|BaseIntermediateOutputPath[[:space:]]*=' full.log | sort -u`, or query a project directly: `dotnet msbuild MyProject.csproj -getProperty:OutputPath` (and `IntermediateOutputPath`, `BaseIntermediateOutputPath`).
- **Distinguishing global properties** — `grep -iE 'TargetFramework|Configuration|Platform|RuntimeIdentifier|SolutionFileName|PublishReadyToRun' full.log`. See the [Global Properties to Check](references/common-causes-and-fixes.md#global-properties-to-check-when-comparing-evaluations) table for which affect the path and which just fork a redundant instance.
- **Corroborating evidence (optional)** — `grep 'Target "CopyFilesToOutputDirectory"' full.log` plus `grep 'SkipUnchangedFiles' full.log` show a second instance writing (or skipping a masked write) to the same path; a long vs ~0 ms `CoreCompile` distinguishes the real build from a redundant instance.

**Then identify clashes:** normalize paths to absolute, group evaluations by `OutputPath` and by `IntermediateOutputPath`, and exclude only instances proven by their targets and writes to be metadata-only queries, not merely marked `BuildProjectReferences=false` — plus, for `OutputPath` only, `MSBuildRestoreSessionId` restore evaluations. Any group with more than one remaining evaluation is a clash.

Read references/common-causes-and-fixes.md when selecting the relevant diagnostic workflow.

## Tips

- The SDK default paths include `$(TargetFramework)` — clashes often occur when projects override these defaults; normalize relative paths to absolute before comparing.
- **Cross-project `IntermediateOutputPath` clashes cannot be fixed with `AppendTargetFrameworkToOutputPath`** — files like `project.assets.json` are written directly to the intermediate path. For multi-targeting clashes *within the same project*, `AppendTargetFrameworkToOutputPath=true` is the correct fix.
- Error messages that indicate a path clash: `Cannot create a file when that file already exists` (NuGet restore), `The process cannot access the file because it is being used by another process`, or intermittent failures that succeed on retry.

### Global Properties to Check When Comparing Evaluations

When multiple evaluations share an output path, compare these global properties to understand why:

| Property | Affects OutputPath? | Notes |
|----------|---------------------|-------|
| `TargetFramework` | Yes | Different TFMs should have different paths. **Exception:** re-injecting a *single-targeting* project's own TFM (e.g. via `SetTargetFramework` with the same value) is path-neutral — it forks a redundant instance sharing the output path (see "`SetTargetFramework` re-injecting...") |
| `RuntimeIdentifier` | Yes | Different RIDs should have different paths |
| `Configuration` | Yes | Debug vs Release |
| `Platform` | Yes | AnyCPU vs x64 etc. |
| `SolutionFileName` | No | Identifies which solution built the project — different values indicate multi-solution clash |
| `SolutionName` | No | Solution name without extension |
| `SolutionPath` | No | Full path to the solution file |
| `SolutionDir` | No | Directory containing the solution file |
| `CurrentSolutionConfigurationContents` | No | XML with project entries — count of entries reveals which solution |
| `BuildProjectReferences` | No | `false` = P2P query, not a real build - ignore these |
| `MSBuildRestoreSessionId` | No | Present = restore phase evaluation |
| `PublishReadyToRun` | No | Publish setting, doesn't change build output path but creates distinct project instances |
| `_IsPublishing` | No | Publish flag; an `<MSBuild>` Build/Publish call with this set (in this project or another that consumes it) forks a publish instance sharing the build output path (see "Explicit `<MSBuild>` Build/Publish with extra global properties") |

## Testing Fixes

After making changes to fix path clashes, clean and rebuild to verify. See the `binlog-generation` skill's "Cleaning the Repository" section on how to clean the repository while preserving binlog files.
