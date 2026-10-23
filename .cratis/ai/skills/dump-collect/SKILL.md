---
name: dump-collect
description: 'Configures and collects crash dumps for modern .NET applications: enabling automatic crash dumps for CoreCLR or NativeAOT, capturing dumps from running processes, setting up collection in Docker or Kubernetes, and using dotnet-dump collect or createdump. Use when a crash dump must be enabled or captured. Not for analyzing or debugging dumps (lldb, windbg, dotnet-dump analyze), profiling or tracing (use dotnet-trace-collect), or .NET Framework processes.'
license: MIT
---
<!-- cratis-ai-managed: skills/dump-collect/SKILL.md -->

<!-- Modified by Cratis from dotnet's original (MIT): frontmatter, applicability, navigation and the changes listed in NOTICE. Upstream: https://github.com/dotnet/skills (plugins/dotnet-diag/skills/dump-collect/SKILL.md; see NOTICE) -->

# .NET Crash Dump Collection

Cratis rules in `.cratis/ai/rules/` (including `csharp.md`, `specs.csharp.md`, and `orleans.md`) and the `cratis-*` skills win where they differ. Dumps contain all process memory, including secrets and personal data: keep them local and task-scoped, never attach to issues or PRs; changing the host `core_pattern` needs the user's approval.

This skill configures and collects crash dumps for modern .NET applications (CoreCLR and NativeAOT) on Linux, macOS, and Windows — including containers.

## Stop Signals

🚨 **Read before starting any workflow.**

- **Stop after dumps are enabled or collected.** Do not open, analyze, or triage dump files.
- **If the user already has a dump file**, this skill does not cover analysis. Let them know analysis is out of scope.
- **Do not install analysis tools** (dotnet-dump analyze, windbg). Only install collection tools (dotnet-dump collect). Using `lldb` for on-demand dump capture on macOS is allowed — it ships with Xcode command-line tools and is not being used for analysis.
- **Do not trace root cause** of crashes. Report the dump file location and move on.
- **Do not modify application code.** Configuration is environment-only (env vars, OS settings, container specs).

## Step 1 — Identify the Scenario

Ask or determine:

1. **Goal**: Enable automatic crash dumps, or capture a dump from a running process right now?
2. **Platform**: Linux, macOS, or Windows? Running in a container (Docker/Kubernetes)?
3. **Runtime**: CoreCLR or NativeAOT?

### Detecting CoreCLR vs NativeAOT

Decide from positive evidence. A native executable that lacks one marker is not thereby NativeAOT: modern CoreCLR apphost executables are native launchers too, so `CorExeMain` (an import of managed .NET Framework-style executables) is not a reliable CoreCLR test. Treat the result as unknown, and ask the user, when the evidence below conflicts or is missing.

**From files next to the executable (any platform):**
- A CoreCLR apphost (framework-dependent or self-contained) sits beside `<app>.dll` and `<app>.runtimeconfig.json`, usually also `<app>.deps.json`. A self-contained app also ships `libcoreclr.so`, `libcoreclr.dylib` or `coreclr.dll`. Treat these as CoreCLR.
- A CoreCLR single-file app has no sibling `.dll`/`.runtimeconfig.json` because they are bundled into the executable. Do not read that as NativeAOT; use the loaded-module check below, and ask the user how the app was published if it is still unclear.
- A NativeAOT app is a single native executable (plus optional native libraries) with no `<app>.dll`, no `<app>.runtimeconfig.json` and no runtime libraries beside it. It embeds the Redhawk runtime, whose symbols and strings start with `Rhp`.

```bash
# Linux/macOS: positive NativeAOT hint (Redhawk runtime strings/symbols)
strings <binary> | grep -q "Rhp" && echo "NativeAOT (Rhp present)"
nm <binary> 2>/dev/null | grep -qi "Rhp" && echo "NativeAOT (Rhp symbols)"
```

```powershell
# Windows: positive NativeAOT hint (Redhawk symbols)
dumpbin /symbols <binary.exe> | Select-String "Rhp" -Quiet
```

**From a running process (Linux):**
```bash
# CoreCLR loads libcoreclr; a NativeAOT process does not
grep -q libcoreclr /proc/<pid>/maps && echo "CoreCLR"
```

**From a running process (macOS):**
```bash
# CoreCLR loads libcoreclr.dylib; a NativeAOT process does not
vmmap <pid> 2>/dev/null | grep -q libcoreclr && echo "CoreCLR"
```

**From a running process (Windows PowerShell):**
```powershell
# CoreCLR — loads coreclr.dll
(Get-Process -Id <pid>).Modules.ModuleName -contains "coreclr.dll"

# .NET Framework — loads clr.dll (this skill does not apply)
(Get-Process -Id <pid>).Modules.ModuleName -contains "clr.dll"
```

A running process that loaded no CoreCLR runtime module and whose binary shows the NativeAOT file layout and `Rhp` evidence is NativeAOT. A single-file self-contained CoreCLR app can link the runtime into the host, so a missing `libcoreclr`/`coreclr.dll` module alone does not prove NativeAOT.

> **If the app is .NET Framework (`clr.dll`), stop.** This skill covers modern .NET (CoreCLR and NativeAOT) only.
>
> **If neither CoreCLR nor NativeAOT is detected, stop.** This skill only applies to .NET applications — do not proceed.

## Step 2 — Load the Appropriate Reference

Based on the scenario identified in Step 1, read the relevant reference file:

| Scenario | Reference |
|----------|-----------|
| CoreCLR app (any platform) | `references/coreclr-dumps.md` |
| NativeAOT app (any platform) | `references/nativeaot-dumps.md` |
| Any app in Docker or Kubernetes | `references/container-dumps.md` (then also load the runtime-specific reference) |

## Step 3 — Execute

Follow the instructions in the loaded reference to configure or collect dumps. Always:

1. **Confirm the dump output directory exists** and has write permissions before enabling collection.
2. **Report the dump file path** back to the user after collection succeeds.
3. **Verify configuration took effect** — for env vars, echo them; for OS settings, read them back.
4. **Remind the user to disable automatic dumps if they were enabled temporarily** — remove or unset `DOTNET_DbgEnableMiniDump` and related env vars to avoid accumulating dump files.
