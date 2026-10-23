<!-- cratis-ai-managed: skills/dotnet-trace-collect/references/tool-selection.md -->
# Diagnostic tool selection

## Contents

- Step 2: Recommend diagnostic tools

### Step 2: Recommend diagnostic tools

Select tools based on the environment using the priority rules below. Once a tool is selected, load the corresponding reference file for detailed command-line usage.

#### Tool reference lookup

| Environment | Reference file(s) |
|-------------|-------------------|
| Windows + modern .NET + admin | `references/perfview.md` |
| Windows + modern .NET, no admin | `references/dotnet-trace-collect.md` |
| Windows + .NET Framework | `references/perfview.md` |
| Linux + .NET 10+ + root | `references/dotnet-trace-collect-linux.md` |
| Linux + pre-.NET 10 | `references/dotnet-trace-collect.md` |
| Linux + native stacks needed | `references/perfcollect.md` |
| Container/K8s (console access) | `references/dotnet-trace-collect.md` (or `dotnet-trace-collect-linux.md`) |
| Container/K8s (no console) | `references/dotnet-monitor.md` |

#### Quick decision matrix (first-pass triage)

| Environment | Preferred tool | Fallback / Notes |
|-------------|----------------|------------------|
| Windows + modern .NET + admin | PerfView | If admin is unavailable, use `dotnet-trace` |
| Windows + .NET Framework + admin | PerfView | Without admin, there is no trace fallback; for hangs/memory leaks, provide dump commands directly (`procdump -ma` or Task Manager) since `dump-collect` does not support .NET Framework |
| Linux + .NET 10+ + root | `dotnet-trace collect-linux` | Use `dotnet-trace` if root or kernel prerequisites are not met |
| Linux + pre-.NET 10 | `dotnet-trace` | Add `perfcollect` when native stacks are needed (requires root) |
| Linux container/Kubernetes | Console tools if in workload context; `dotnet-monitor` if no console access | See Linux Container / Kubernetes section for details |

#### Windows (non-container, modern .NET)

1. **PerfView** (preferred) — produces richer ETW-based data; requires admin privileges. For **slow requests**, add `/ThreadTime` to capture thread-level wait and block detail.
2. **`dotnet-trace`** — fallback when admin privileges are not available.
3. For **long-running repros**: use PerfView with a `/StopOn` trigger that fires on the **symptom you want to capture** (e.g., `/StopOnPerfCounter`, `/StopOnGCEvent`, `/StopOnException`) and a circular buffer (`/CircularMB` + `/BufferSizeMB`). **Critical: the stop trigger must fire on the interesting event, not the recovery.** The circular buffer continuously overwrites old data, so if you trigger on recovery, the buffer may have already overwritten the interesting behavior by the time collection stops. Only add `/StartOn` if the start event is known to precede the stop event. For **slow requests**, do not include a stop trigger by default — let the user design one based on their specific scenario.

#### Windows containers

1. **PerfView** — most Windows containers (including Kubernetes on Windows) use process-isolation by default. Collect from the host with `/EnableEventsInContainers`. After collection, you have two options:
   - **Analyze locally while the container is still running** — PerfView can reach into the live container to resolve symbols, so you can open the trace immediately on the host machine.
   - **Analyze off-machine** — before the container shuts down, copy the `.etl.zip` into the container and run `PerfViewCollect merge /ImageIDsOnly` inside it to embed symbol information. Then copy the merged trace out. Without this merge step, symbols for binaries inside the container will be unresolvable on other machines.

   For the less common Hyper-V containers, collect inside the container directly. See [references/perfview.md](perfview.md) for detailed commands.
2. **`dotnet-monitor`**, **`dotnet-trace`** — inside the container if the tools are installed in the image. For dumps, invoke the **`dump-collect`** skill.

#### Windows (.NET Framework)

1. **PerfView** — the primary diagnostic tool for .NET Framework on Windows. Requires admin.
2. Same trigger guidance for long repros: use `/StopOn` triggers that fire on the symptom (e.g., `/StopOnPerfCounter`, `/StopOnGCEvent`, `/StopOnException`) with `/CircularMB` + `/BufferSizeMB`.
3. **Without admin**: PerfView requires admin, and there are no alternative trace tools for .NET Framework. Process dumps can still be captured without admin — provide dump commands directly (e.g., `procdump -ma <PID>` or Task Manager) since the `dump-collect` skill does not support .NET Framework. Dumps can help diagnose hangs and memory leaks. However, for **high CPU**, **slow requests**, and **excessive GC**, there is no way to investigate on .NET Framework without admin access. Advise the user to obtain admin privileges.

#### Linux (non-container, .NET 10+)

1. **`dotnet-trace collect-linux`** (preferred) — uses `perf_events` for richer traces including native call stacks and kernel events. Captures machine-wide by default (no PID required). Requires root and kernel >= 6.4.
2. **`dotnet-trace`** — fallback when root privileges are not available or kernel requirements are not met. Managed stacks only.

#### Linux (non-container, pre-.NET 10)

1. **`dotnet-trace`** (preferred) — managed trace collection; no admin required.
2. **`perfcollect`** — when **native call stacks** are needed (requires admin/root).

#### Linux Container / Kubernetes

**If running in the context of the workload** (i.e., you have console access to the container), prefer console-based tools. These are easier to set up than `dotnet-monitor`, which requires authentication configuration and sidecar deployment:

1. **`dotnet-trace collect-linux`** (.NET 10+ with root) — produces the richest traces including native call stacks and kernel events.
2. **`dotnet-trace`** — inside the container if the tool is installed in the image. For dumps, invoke the **`dump-collect`** skill.
> **Cratis:** Requires the user's explicit approval: downloads and runs a script as root / changes container security.
3. **`perfcollect`** — inside the container when native stacks are needed on pre-.NET 10 (requires `SYS_ADMIN` / `--privileged`).

**If not running in the workload context** (no console access), or if `dotnet-monitor` is already deployed:

1. **`dotnet-monitor`** — designed for containers; runs as a sidecar. No tools needed in the app container. Easiest option when console access is not available.

#### Memory dumps

When dumps are needed (memory leaks, hangs), **do not provide dump collection commands directly** for modern .NET — invoke the **`dump-collect`** skill instead. The `dump-collect` skill only supports modern .NET (.NET Core 3.0+). For **.NET Framework**, provide dump collection guidance directly (e.g., `procdump -ma <PID>` or Task Manager). This skill focuses on trace collection only.

#### Memory leaks

- **Capture two dumps** as memory is increasing (e.g., one early, one after significant growth). Invoke the **`dump-collect`** skill for dump collection — do not provide dump commands directly. Diff the dumps in PerfView to see which objects have increased — this is the most effective way to identify what is leaking.
- **Without admin privileges**: Two process dumps can give a sense of what's growing on the heap, but may not be enough to identify the root cause. If dumps aren't sufficient, reproduce the issue in an environment where admin privileges are available to collect richer data (traces).
- **Modern .NET on Linux (pre-.NET 10)**: Recommend two dump captures (invoke `dump-collect` skill) for heap diff, plus `dotnet-trace` while memory is growing (for allocation tracking). No trigger needed — capture during the growth period. Both together give the best picture.
- **Modern .NET 10+ on Linux with admin**: Recommend two dump captures (invoke `dump-collect` skill) for heap diff, plus `dotnet-trace collect-linux` while memory is growing (richer data including native stacks). No trigger needed.
- **.NET Framework**: Recommend two dumps plus a PerfView trace while memory is growing to see what is being allocated. The `dump-collect` skill does not support .NET Framework, so provide dump commands directly (e.g., `procdump -ma <PID>` or right-click → Create Dump File in Task Manager). No trigger is needed — just capture the trace during the growth period. Do not wait for an `OutOfMemoryException`.

#### Excessive GC

Excessive GC requires a **trace** to analyze GC events, pause times, and allocation patterns — a dump is not sufficient.

- **Windows (PerfView)**: Use `PerfView collect /GCCollectOnly` to capture GC events.
- **Linux (dotnet-trace)**: Use `dotnet-trace collect -p <PID> --profile gc-verbose`.
- **Linux .NET 10+ with root**: Use `dotnet-trace collect-linux --profile gc-verbose` for richer data with native stacks.
- **Containers**: `dotnet-monitor` can capture GC traces via its REST API (`/trace?profile=gc-verbose`).

#### Slow Requests

Slow requests require a **thread time trace** to see where threads are spending time — waiting on locks, I/O, external calls, etc. Use larger buffers since thread time traces generate more data. For ASP.NET Core applications, also enable `Microsoft.AspNetCore.Hosting` and `Microsoft-AspNetCore-Server-Kestrel` providers to get server-side request lifecycle timing (when requests arrive, how long they take to process).

- **Windows (PerfView)**: Use `PerfView /ThreadTime collect /BufferSizeMB:1024 /CircularMB:2048`. The `/ThreadTime` argument adds thread-level wait and block detail. For ASP.NET Core, add Kestrel providers: `PerfView /ThreadTime collect /BufferSizeMB:1024 /CircularMB:2048 /Providers:*Microsoft.AspNetCore.Hosting,*Microsoft-AspNetCore-Server-Kestrel`. Do not include a stop trigger by default — let the user design one based on their specific scenario.
- **Linux (dotnet-trace)**: `dotnet-trace` captures thread time data by default — no special arguments needed. Use `dotnet-trace collect -p <PID>`. For ASP.NET Core, add Kestrel providers: `dotnet-trace collect -p <PID> --providers Microsoft.AspNetCore.Hosting,Microsoft-AspNetCore-Server-Kestrel`.
- **Linux .NET 10+ with root**: Use `dotnet-trace collect-linux --profile thread-time` for richer data with native stacks. For ASP.NET Core, add: `--providers Microsoft.AspNetCore.Hosting,Microsoft-AspNetCore-Server-Kestrel`.
- **Containers**: `dotnet-monitor` can capture traces via its REST API (`/trace?pid=<PID>&durationSeconds=30`).

#### Hangs

1. **Start with a trace** to understand what threads are doing. Use the appropriate trace tool for the environment (PerfView with `/ThreadTime` on Windows, `dotnet-trace` on Linux, `dotnet-trace collect-linux --profile thread-time` on .NET 10+ Linux with root). The trace can reveal:
   - **Livelocks** (threads spinning without forward progress) — threads appear busy but the application makes no progress.
   - **Thread starvation** — the ThreadPool is exhausted and queued work items are not being processed. This can look like a deadlock but has a different root cause.
   - **Whether there is any forward progress at all** — if some threads are making progress, the issue may be a bottleneck rather than a true hang.
2. **If the trace does not explain the hang**, the issue may be a **true deadlock** (threads waiting on each other in a cycle). In this case, invoke the **`dump-collect`** skill to collect a process dump — do not provide dump commands directly.
3. **Analyze the dump with a debugger** to inspect thread stacks and identify the lock cycle:
   - **Windows**: Visual Studio or WinDbg with the SOS debugger extension.
   - **Linux**: `lldb` with the SOS debugger extension.

#### Networking Issues

Networking issues (HTTP 5xx errors from downstream services, request timeouts, connection failures, DNS resolution failures, TLS handshake failures, connection pool exhaustion) require **both** a thread-time trace and networking event providers. The thread-time trace shows where threads are blocked (slow downstream calls, thread starvation), while the networking events show the request lifecycle — which requests failed, what status codes came back, how long DNS resolution and TLS handshakes took, and how long requests waited for a connection from the pool.

For **.NET Framework**, `PerfView /ThreadTime` already collects the relevant networking events (from the `System.Net` ETW provider) — no additional providers are needed.

For **modern .NET**, you must explicitly enable the `System.Net.*` EventSource providers:

| Provider | What it covers |
|----------|---------------|
| `System.Net.Http` | HttpClient/SocketsHttpHandler — request lifecycle, HTTP status codes, connection pool |
| `System.Net.NameResolution` | DNS lookups (start/stop, duration) |
| `System.Net.Security` | TLS/SSL handshakes (SslStream) |
| `System.Net.Sockets` | Low-level socket connect/disconnect |

Key events from `System.Net.Http`: `RequestStart` (scheme, host, port, path), `RequestStop` (statusCode — `-1` if no response was received), `RequestFailed` (exception message for timeouts, connection refused, etc.), `RequestLeftQueue` (time waiting for a connection from the pool — indicates connection pool exhaustion), `ConnectionEstablished`, `ConnectionClosed`.

Collect a thread-time trace with networking providers enabled (modern .NET only — .NET Framework needs only `PerfView /ThreadTime`):

- **Windows (PerfView)**: Use `PerfView /ThreadTime collect /BufferSizeMB:1024 /CircularMB:2048 /Providers:*System.Net.Http,*System.Net.NameResolution,*System.Net.Security,*System.Net.Sockets`. For .NET Framework, omit the `/Providers` flag — `/ThreadTime` already includes the networking events. The thread-time trace shows where threads are blocked while the networking events show what requests are failing and why.
- **Linux (dotnet-trace)**: `dotnet-trace` captures thread time data by default, but specifying `--providers` overrides the defaults so you must also include `--profile`: `dotnet-trace collect -p <PID> --profile dotnet-common,dotnet-sampled-thread-time --providers System.Net.Http,System.Net.NameResolution,System.Net.Security,System.Net.Sockets`.
- **Linux .NET 10+ with root**: Use `dotnet-trace collect-linux --profile dotnet-common,cpu-sampling,thread-time --providers System.Net.Http,System.Net.NameResolution,System.Net.Security,System.Net.Sockets`.
- **Containers**: `dotnet-monitor` can capture traces with custom providers via its REST API.

#### Assembly Loading Issues

For modern .NET, assembly loading issues (`FileNotFoundException`, `FileLoadException`, `ReflectionTypeLoadException`, version conflicts, duplicate assembly loads across AssemblyLoadContexts) require collecting **assembly loader binder events** from the `Microsoft-Windows-DotNETRuntime` provider with the Loader keyword (`0x4`). These events trace every step of the runtime's assembly resolution algorithm — which paths were probed, which AssemblyLoadContext handled the load, whether the load succeeded or failed, and why. For .NET Framework, the same provider and keyword work for ETW-based collection; additionally, the Fusion Log Viewer (`fuslogvw.exe`) can diagnose assembly binding failures without requiring a trace.

The provider specification is `Microsoft-Windows-DotNETRuntime:0x4:4` (provider name, AssemblyLoader keyword, Informational verbosity).

- **Windows (PerfView)**: A default PerfView trace already includes binder events - simply run `PerfView collect` with no extra providers. For a smaller trace file, use `PerfView collect /ClrEvents:Default-Profile`, which removes the most verbose default events while keeping the events necessary for diagnosing assembly loading issues.
- **Linux / cross-platform (dotnet-trace)**: Use `dotnet-trace collect --clrevents assemblyloader -- <path-to-built-exe>` to launch and trace the process, or `dotnet-trace collect --clrevents assemblyloader -p <PID>` to attach to a running process.
- **Linux .NET 10+ with root**: Use `dotnet-trace collect-linux --clrevents assemblyloader`.
- **Containers**: `dotnet-monitor` can capture traces with the loader provider via its REST API.

For short-lived processes that fail on startup (common with assembly loading issues), prefer the `dotnet-trace` launch form (`-- <path-to-built-exe>`) over attaching by PID, since the process may exit before you can attach.

Explain the trade-offs when recommending a tool. For example:
- PerfView gives richer data but needs admin; runs on Windows including Windows containers.
- `dotnet-trace` works cross-platform without admin but captures less system-level detail.
- `perfcollect` captures native call stacks but needs admin/root.
- `dotnet-monitor` is the best option for containers/K8s when console access is not available, but requires sidecar deployment and authentication configuration.
