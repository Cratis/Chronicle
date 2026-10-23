---
name: binlog-generation
description: 'Generates MSBuild binary logs (binlogs) by adding /bl:{} to dotnet build, test, pack, publish, or restore commands, capturing a full build trace for later investigation. Use when a build error or build-performance problem needs a binlog and none exists. The {} placeholder needs a recent MSBuild; PowerShell must quote the switch as ''-bl:{}''. Not for analyzing an existing binlog or measuring build performance (use the msbuild skill) or for non-MSBuild build systems such as npm, Maven, or CMake.'
license: MIT
---
<!-- cratis-ai-managed: skills/binlog-generation/SKILL.md -->

<!-- Modified by Cratis from dotnet's original (MIT): frontmatter, applicability, navigation and the changes listed in NOTICE. Upstream: https://github.com/dotnet/skills (plugins/dotnet-msbuild/skills/binlog-generation/SKILL.md; see NOTICE) -->

# Generate Binary Logs

Cratis rules in `.cratis/ai/rules/` (including `csharp.md`, `specs.csharp.md`, and `orleans.md`) and the `cratis-*` skills win where they differ. Binlogs embed project and imported file contents: keep them local and task-scoped.

**Pass the `/bl` switch when running any MSBuild-based command.** This is a non-negotiable requirement for all .NET builds.

## Commands That Require /bl

You MUST add the `/bl:{}` flag to:
- `dotnet build`
- `dotnet test`
- `dotnet pack`
- `dotnet publish`
- `dotnet restore`
- `msbuild` or `msbuild.exe`
- Any other command that invokes MSBuild

## Preferred: Use `{}` for Automatic Unique Names

> **Note:** The `{}` placeholder requires MSBuild 17.12+ / .NET 9.0.100 SDK or later.

The `{}` placeholder in the binlog filename is replaced by MSBuild with a unique identifier, guaranteeing no two builds ever overwrite each other — without needing to track or check existing files.

```bash
# Every invocation produces a distinct file automatically
dotnet build /bl:{}
dotnet test /bl:{}
dotnet build --configuration Release /bl:{}
```

**PowerShell requires quoting the complete switch:**

```powershell
# Keep the literal {} placeholder in one argument
dotnet build '-bl:{}'
dotnet test '-bl:{}'
```

## Why This Matters

1. **Unique names prevent overwrites** - You can always go back and analyze previous builds
2. **Failure analysis** - When a build fails, the binlog is already there for immediate analysis
3. **Comparison** - You can compare builds before and after changes
4. **No re-running builds** - You never need to re-run a failed build just to generate a binlog

## Examples

```bash
# ✅ CORRECT - {} generates a unique name automatically (bash/cmd)
dotnet build /bl:{}
dotnet test /bl:{}

# ✅ CORRECT - quote the complete PowerShell argument
dotnet build '-bl:{}'
dotnet test '-bl:{}'

# ❌ WRONG - Missing /bl flag entirely
dotnet build
dotnet test

# ❌ WRONG - No filename (overwrites the same msbuild.binlog every time)
dotnet build /bl
dotnet build /bl
```

## One build = one binlog

Add `/bl:{}` to **every** MSBuild invocation separately — never reuse a name and
never rely on bare `/bl`:

- Building several configurations, projects, or retrying a failed build? Each
  command still gets its own `/bl:{}` so the logs never overwrite each other.

```bash
dotnet build -c Debug   /bl:{}   # unique file
dotnet build -c Release /bl:{}   # another unique file
```

## Verify the binlog exists

After the build, confirm a `.binlog` was actually produced before moving on to
analysis — a build that fails *before* MSBuild starts (e.g. a bad argument)
writes no binlog:

```bash
ls -1 *.binlog       # bash
dir /b *.binlog      # Windows cmd
```

```powershell
Get-ChildItem *.binlog   # PowerShell
```

Note the resulting path so binlog failure analysis or build perf diagnostics
can consume it.

## When a Specific Filename Is Required

If the binlog filename needs to be known upfront (e.g., for CI artifact upload), or if `{}` is not available in the installed MSBuild version, pick a name that won't collide with existing files:

1. Check for existing `*.binlog` files in the directory
2. Choose a name not already taken (e.g., by incrementing a counter from the highest existing number)

```bash
# Example: directory contains 3.binlog — use 4.binlog
dotnet build /bl:4.binlog
```

## Cleaning the Repository

When cleaning the repository with `git clean`, **always exclude binlog files** to preserve your build history:

```bash
# ✅ CORRECT - Exclude binlog files from cleaning
git clean -fdx -e "*.binlog"

# ❌ WRONG - This deletes binlog files (they're usually in .gitignore)
git clean -fdx
```

This is especially important when iterating on build fixes - you need the binlogs to analyze what changed between builds.
