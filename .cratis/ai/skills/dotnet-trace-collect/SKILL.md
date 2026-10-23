---
name: dotnet-trace-collect
description: 'Guides capturing diagnostic artifacts to diagnose production .NET performance problems: choosing tools, collecting performance data, and understanding trade-offs across Windows and Linux, .NET Framework and modern .NET, and container and non-container environments. Use when the user needs help choosing a diagnostic tool or collecting performance data for a .NET process. Not for crash dumps (use dump-collect), repository-level profiling workflows (use profiling), or analyzing collected traces.'
license: MIT
---
<!-- cratis-ai-managed: skills/dotnet-trace-collect/SKILL.md -->

<!-- Modified by Cratis from dotnet's original (MIT): frontmatter, applicability, navigation and the changes listed in NOTICE. Upstream: https://github.com/dotnet/skills (plugins/dotnet-diag/skills/dotnet-trace-collect/SKILL.md; see NOTICE) -->

# .NET Trace Collect

Cratis rules in `.cratis/ai/rules/` (including `csharp.md`, `specs.csharp.md`, and `orleans.md`) and the `cratis-*` skills win where they differ.

This skill helps developers diagnose production performance issues by recommending the right diagnostic tools for their environment, guiding data collection, and suggesting analysis approaches. It does not analyze code for anti-patterns or perform the analysis itself.

## When to Use

- A developer needs to investigate a production performance issue (high CPU, memory leak, slow requests, excessive GC, networking errors, etc.)
- Choosing the right diagnostic tool for a specific runtime, OS, or deployment topology
- Setting up and running diagnostic tool commands for data collection
- Understanding trade-offs between available tools (e.g. PerfView vs dotnet-trace)
- Collecting diagnostics from containerized or Kubernetes workloads

## When Not to Use

- Reviewing source code for performance anti-patterns (use a code review skill instead)
- Benchmarking during development (e.g. BenchmarkDotNet setup)
- Analyzing collected trace or dump files (this skill recommends tools for analysis, but does not perform it)

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Symptom | Yes | What the developer is observing (high CPU, memory growth, slow requests, hangs, excessive GC, HTTP 5xx errors, networking timeouts, connection failures, assembly loading failures, etc.) |
| Runtime | Yes | .NET Framework or modern .NET (and version, especially whether .NET 10+) |
| OS | Yes | Windows or Linux |
| Deployment | Yes | Non-container, container, or Kubernetes |
| Admin privileges | Recommended | Whether the developer has admin/root access on the target machine |
| Repro characteristics | Recommended | Whether the issue is easy to reproduce or requires a long time to manifest |

## Workflow

### Step 1: Understand the environment

Determine or ask the developer to clarify:

1. **Symptom**: What they are observing (high CPU, memory leak, slow requests, hangs, excessive GC, HTTP 5xx errors, networking timeouts, connection failures, assembly loading failures, etc.)
2. **Runtime**: .NET Framework or modern .NET? If modern .NET, which version? (Especially whether .NET 10 or later.)
3. **OS**: Windows or Linux?
4. **Deployment**: Running directly on the host, in a container, or in Kubernetes?
5. **Admin privileges**: Do they have admin/root access on the target machine or container?
6. **Repro characteristics**: Does the issue reproduce quickly, or does it take a long time to manifest?
7. **Workload context**: Determine or ask the user if you are running in the context of the workload (i.e., on the same machine or connected to the same environment where the issue is occurring). If so, you can run diagnostic commands directly on their behalf. If not, provide the commands as guidance for the user to run themselves.

Use this information to select the right tool in Step 2.

Read references/tool-selection.md when selecting the relevant diagnostic workflow.

### Step 3: Guide data collection

Provide the specific commands for the recommended tool. Load the appropriate reference file from the [tool reference lookup](references/tool-selection.md#tool-reference-lookup) table for detailed command-line examples.

Key guidance to include:

> **Cratis:** Downloads or installs a tool: requires the user's approval; prefer a pinned version (`dotnet tool install <tool> --version <x>`)
1. **Installation**: How to install the tool if it is not already available (e.g. `dotnet tool install -g dotnet-trace`). **When recommending multiple tools, provide installation and usage instructions for each one** — do not mention a tool without showing how to install and use it.
2. **PID discovery (required before any `-p <PID>` command)**: Verify the target process first (for example: `dotnet-trace ps`, `curl <monitor-endpoint>/processes`, or `ps` inside a container). If the app is expected to be PID 1 in a container, still verify before collecting.
3. **Collection command**: The exact command to run, including relevant providers, output format, and duration.
4. **Container considerations**:
   - Collecting from **inside** the container: ensure the tool is installed in the image or use `kubectl cp` to copy it in.
   - Collecting from **outside** the container: use `dotnet-monitor` as a sidecar with a shared diagnostic port (Unix domain socket in `/tmp`).
   - Kubernetes: `dotnet-monitor` as a sidecar container, or `kubectl debug` for ephemeral debug containers.
5. **Long-running repros** (Windows/PerfView): show how to use trigger arguments and circular buffer settings.
6. **Output location**: Where the collected file will be saved and how to copy it off the target for analysis.
7. **Artifact handoff checklist**: Include runtime version, OS/kernel, container image tag or build SHA, PID/process name, UTC collection start/end timestamps, exact command used, and final artifact path when handing traces to someone else for analysis.

### Step 4: Recommend analysis approach

After data is collected, recommend the appropriate tool for analysis. Do **not** perform the analysis — just point the developer to the right tool and documentation.

| Collected Data | Analysis Tool | Notes |
|----------------|---------------|-------|
| `.nettrace` file | PerfView (Windows), Speedscope (web) | PerfView gives the richest view on Windows |
| `.etl` / `.etl.zip` file | PerfView | ETW traces from PerfView or perfcollect |
| `perf.data.nl` from perfcollect | PerfView (Windows) | Copy the file to a Windows machine and open with PerfView |

## Validation

- [ ] The recommended tool is compatible with the developer's runtime, OS, and deployment topology
- [ ] The collection command runs without errors
- [ ] The output file is generated in the expected location
- [ ] The developer knows which analysis tool to use for the collected data

## Common Pitfalls

> **Cratis:** Requires the user's explicit approval: downloads and runs a script as root / changes container security.
> **Cratis:** Downloads or installs a tool: requires the user's approval; prefer a pinned version (`dotnet tool install <tool> --version <x>`)

| Pitfall | Solution |
|---------|----------|
| Using `dotnet-trace` on .NET Framework | `dotnet-trace` only works with modern .NET (.NET Core 3.0+). Use PerfView for .NET Framework. |
| PerfView without admin privileges | PerfView requires admin for ETW tracing. Fall back to `dotnet-trace` if admin is not available. |
| `perfcollect` in container without `SYS_ADMIN` | Containers drop `SYS_ADMIN` by default. Run with `--privileged` or add `SYS_ADMIN` capability, or fall back to `dotnet-trace`. |
| Huge trace files from long repros | On Windows, use PerfView `/StopOn` triggers that fire on the symptom you want to capture (e.g., `/StopOnPerfCounter`, `/StopOnGCEvent`, `/StopOnException`) with `/CircularMB` and `/BufferSizeMB`. **Never trigger on recovery** — the circular buffer continuously overwrites old data, so the interesting behavior may be lost by the time collection stops. |
| Diagnostic port not accessible in container | Mount `/tmp` as a shared volume between the app container and `dotnet-monitor` sidecar for the diagnostic Unix domain socket. |
| Forgetting to install tools in container image | Add `dotnet tool install` to your Dockerfile, or use `dotnet-monitor` as a sidecar to avoid modifying the app image. |
| Exposing `dotnet-monitor` with `--no-auth` in production | Keep auth enabled, bind to localhost, and use `kubectl port-forward` for access. Use `--no-auth` only for short-lived isolated debugging. |
| Collecting only CPU/thread-time trace for networking issues | CPU and thread-time traces alone do not show HTTP status codes, DNS timing, or connection pool behavior. Add the networking providers (`System.Net.Http`, `System.Net.NameResolution`, `System.Net.Security`, `System.Net.Sockets`) alongside the thread-time trace. |
| Enabling all networking providers when only one is needed | Each networking provider adds overhead. If the issue is clearly HTTP-level (5xx status codes), `System.Net.Http` alone may be sufficient. Add DNS, TLS, and socket providers when the root cause is unclear. |

## Reference navigation

- Read references/dotnet-monitor.md when working on dotnet monitor.
- Read references/dotnet-trace-collect-linux.md when working on dotnet trace collect linux.
- Read references/dotnet-trace-collect.md when working on dotnet trace collect.
- Read references/perfcollect.md when working on perfcollect.
- Read references/perfview.md when working on perfview.
