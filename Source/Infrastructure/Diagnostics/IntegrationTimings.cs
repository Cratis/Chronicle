// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Chronicle.Diagnostics;

// Internal, opt-in test diagnostics. Never include application data in these records.
internal sealed class IntegrationTimings
{
    static readonly bool _enabled = Environment.GetEnvironmentVariable("CHRONICLE_INTEGRATION_TIMINGS") == "1";
    static readonly AsyncLocal<Phase?> _current = new();
    static readonly object _outputLock = new();
    static readonly string _clockDomain = Guid.NewGuid().ToString("N");
    readonly Action<string>? _write;
    readonly Func<long> _clock;
    readonly long _frequency;
    readonly string _instance = Guid.NewGuid().ToString("N");
    readonly string _type;
    readonly string _mode;
    readonly string _provider;
    readonly long _started;
    long _sequence;
    int _open;
    int _completed;
    bool _outputFailed;

    internal IntegrationTimings(string type, string mode = "unavailable", string provider = "unavailable")
        : this(_enabled ? Write : null, Stopwatch.GetTimestamp, Stopwatch.Frequency, type, (mode, provider))
    {
    }

    internal IntegrationTimings(Action<string>? write, Func<long> clock, long frequency, string type, (string Mode, string Provider)? labels = null)
    {
        _write = write;
        _clock = clock;
        _frequency = frequency;
        _type = type;
        (_mode, _provider) = labels ?? ("unavailable", "unavailable");
        _started = write is null ? 0 : clock();
    }

    internal static bool Enabled => _enabled;
    internal bool IsRecording => _write is not null;

    internal static Phase? CurrentPhase => _current.Value;

    internal Phase? Begin(string name)
    {
        if (_write is null) return null;
        var phase = new Phase(this, name, _current.Value, Interlocked.Increment(ref _sequence), _clock());
        Interlocked.Increment(ref _open);
        _current.Value = phase;
        Emit("CHRONICLE_INTEGRATION_TIMING ", phase, ("started", "partial", 0));
        return phase;
    }

    internal static Phase? BeginCurrent(string name) => _current.Value?.Owner.Begin(name);

    internal Task Run(string name, Func<Task> action) => _write is null ? action() : RunMeasured(name, action);

    internal T Measure<T>(string name, Func<T> action)
    {
        if (_write is null) return action();
        using var phase = Begin(name);
        try
        {
            var result = action();
            phase?.Success();
            return result;
        }
        catch
        {
            phase?.Failure();
            throw;
        }
    }

    async Task RunMeasured(string name, Func<Task> action)
    {
        using var phase = Begin(name);
        try
        {
            await action();
            phase?.Success();
        }
        catch
        {
            phase?.Failure();
            throw;
        }
    }

    internal void Complete()
    {
        if (_write is null || Interlocked.Exchange(ref _completed, 1) != 0) return;
        Emit("CHRONICLE_INTEGRATION_SUMMARY ", null, ("finished", _open == 0 && !_outputFailed ? "complete" : "partial", Elapsed(_started)));
    }

    double Elapsed(long started) => (_clock() - started) * 1000d / _frequency;

    internal readonly record struct CommandTotals(double ElapsedMs, int Count, int Failures, bool Observed);

    internal void Aggregate(string name, CommandTotals totals)
    {
        if (_write is null) return;
        var phase = new Phase(this, name, null, Interlocked.Increment(ref _sequence), _started);
        Emit("CHRONICLE_INTEGRATION_TIMING ", phase, (totals.Failures > 0 ? "failure" : "success", totals.Observed ? "complete" : "partial", totals.ElapsedMs), totals);
    }

    static string Label(string name) => Environment.GetEnvironmentVariable(name) ?? "unavailable";

    void Emit(string prefix, Phase? phase, (string Outcome, string Completeness, double Elapsed) result, CommandTotals? aggregate = null)
    {
        phase ??= new Phase(this, "instance", null, Interlocked.Increment(ref _sequence), _started);
        var counts = aggregate ?? new CommandTotals(result.Elapsed, 1, 0, true);
        try
        {
            _write!(prefix + JsonSerializer.Serialize(new
            {
                schema = 1,
                run = Label("CHRONICLE_INTEGRATION_RUN_ID"),
                sha = Label("GITHUB_SHA"),
                process = Environment.ProcessId,
                clockDomain = _clockDomain,
                instance = _instance,
                type = _type,
                mode = _mode,
                provider = _provider,
                attempt = Label("GITHUB_RUN_ATTEMPT"),
                filter = Label("CHRONICLE_INTEGRATION_FILTER"),
                sequence = phase.Sequence,
                parent = phase.Parent?.Sequence,
                parentInstance = phase.Parent?.Owner._instance,
                phase = phase.Name,
                // Process-monotonic coordinates allow union accounting across recorder instances.
                startMs = phase.Started * 1000d / _frequency,
                elapsedMs = result.Elapsed,
                count = counts.Count,
                failures = counts.Failures,
                accounting = aggregate.HasValue ? "aggregate-command-duration" : "interval",
                polls = phase.Polls,
                retries = phase.Retries,
                targets = phase.Targets,
                tables = phase.Tables,
                outcome = result.Outcome,
                completeness = result.Completeness,
                outputFailed = _outputFailed
            }));
        }
        catch
        {
            _outputFailed = true;
            // Diagnostics must never replace a test failure, including a broken console/sidecar.
            try { Console.Error.WriteLine("CHRONICLE_INTEGRATION_TIMING_WARNING output unavailable; report is incomplete"); }
            catch { /* The diagnostic channel itself is unavailable; preserve the original outcome. */ }
        }
    }

    internal static void Replay(string record)
    {
        if (!_enabled) return;
        try { Write(record); }
        catch
        {
            try { Console.Error.WriteLine("CHRONICLE_INTEGRATION_TIMING_WARNING kernel replay unavailable; report is incomplete"); }
            catch { /* Diagnostics cannot replace the fixture outcome. */ }
        }
    }

    static void Write(string record)
    {
        lock (_outputLock)
        {
            var path = Environment.GetEnvironmentVariable("CHRONICLE_INTEGRATION_TIMINGS_FILE");
            // Write the sidecar first: xUnit may suppress successful-test console output.
            try
            {
                if (!string.IsNullOrEmpty(path)) File.AppendAllText($"{path}.{Environment.ProcessId}.jsonl", record + Environment.NewLine);
            }
            finally
            {
                // A broken sidecar must not prevent the primary channel from receiving the record.
                Console.WriteLine(record);
            }
        }
    }

    internal sealed class Phase(IntegrationTimings owner, string name, Phase? parent, long sequence, long started) : IDisposable
    {
        string _outcome = "unobserved";
        int _disposed;

        internal IntegrationTimings Owner { get; } = owner;
        internal string Name { get; } = name;
        internal Phase? Parent { get; } = parent;
        internal long Sequence { get; } = sequence;
        internal long Started { get; } = started;
        int _polls;
        int _retries;
        int _targets;
        int _tables = -1;

        internal int Targets => _targets;
        internal int? Tables => _tables < 0 ? null : _tables;
        internal void Target() => Interlocked.Increment(ref _targets);
        internal void TablesObserved() => Interlocked.CompareExchange(ref _tables, 0, -1);
        internal void Table() => Interlocked.Increment(ref _tables);

        internal int Polls => _polls;
        internal int Retries => _retries;

        internal void Poll() => Interlocked.Increment(ref _polls);
        internal void Retry() => Interlocked.Increment(ref _retries);
        internal void Success() => _outcome = "success";
        internal void SuccessUnlessIncomplete(bool successful, bool timedOut)
        {
            if (successful && !timedOut) Success();
        }
        internal void Failure() => _outcome = "failure";

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _current.Value = Parent;
            Interlocked.Decrement(ref Owner._open);
            Owner.Emit("CHRONICLE_INTEGRATION_TIMING ", this, (_outcome, "complete", Owner.Elapsed(Started)));
        }
    }
}
