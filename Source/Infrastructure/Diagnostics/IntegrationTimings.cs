// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Internal, opt-in test diagnostics. Never include application data in these records.
/// </summary>
internal sealed class IntegrationTimings
{
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

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTimings"/> class writing to the process outputs.
    /// </summary>
    /// <param name="type">The type being measured.</param>
    /// <param name="mode">The mode label.</param>
    /// <param name="provider">The provider label.</param>
    internal IntegrationTimings(string type, string mode = "unavailable", string provider = "unavailable")
        : this(Enabled ? Write : null, Stopwatch.GetTimestamp, Stopwatch.Frequency, type, (mode, provider))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTimings"/> class.
    /// </summary>
    /// <param name="write">The record writer, or null when not recording.</param>
    /// <param name="clock">The clock returning timestamps.</param>
    /// <param name="frequency">The clock frequency in ticks per second.</param>
    /// <param name="type">The type being measured.</param>
    /// <param name="labels">The optional mode and provider labels.</param>
    internal IntegrationTimings(Action<string>? write, Func<long> clock, long frequency, string type, (string Mode, string Provider)? labels = null)
    {
        _write = write;
        _clock = clock;
        _frequency = frequency;
        _type = type;
        (_mode, _provider) = labels ?? ("unavailable", "unavailable");
        _started = write is null ? 0 : clock();
    }

    /// <summary>
    /// Gets a value indicating whether timings are enabled for this process.
    /// </summary>
    internal static bool Enabled { get; } = Environment.GetEnvironmentVariable("CHRONICLE_INTEGRATION_TIMINGS") == "1";

    /// <summary>
    /// Gets the ambient phase, if any.
    /// </summary>
    internal static Phase? CurrentPhase => _current.Value;

    /// <summary>
    /// Gets a value indicating whether this instance is recording.
    /// </summary>
    internal bool IsRecording => _write is not null;

    /// <summary>
    /// Begins a phase in the ambient phase of the current context.
    /// </summary>
    /// <param name="name">The phase name.</param>
    /// <returns>The phase, or null when not recording.</returns>
    internal static Phase? BeginCurrent(string name) => _current.Value?.Owner.Begin(name);

    /// <summary>
    /// Writes an already formatted record, never failing the caller.
    /// </summary>
    /// <param name="record">The record to write.</param>
    internal static void Replay(string record)
    {
        if (!Enabled) return;
        try { Write(record); }
        catch
        {
            try { Console.Error.WriteLine("CHRONICLE_INTEGRATION_TIMING_WARNING kernel replay unavailable; report is incomplete"); }
            catch { /* Diagnostics cannot replace the fixture outcome. */ }
        }
    }

    /// <summary>
    /// Begins a phase.
    /// </summary>
    /// <param name="name">The phase name.</param>
    /// <returns>The phase, or null when not recording.</returns>
    internal Phase? Begin(string name)
    {
        if (_write is null) return null;
        var phase = new Phase(this, name, _current.Value, Interlocked.Increment(ref _sequence), _clock());
        Interlocked.Increment(ref _open);
        _current.Value = phase;
        Emit("CHRONICLE_INTEGRATION_TIMING ", phase, ("started", "partial", 0));
        return phase;
    }

    /// <summary>
    /// Runs an asynchronous action, measuring it when recording.
    /// </summary>
    /// <param name="name">The phase name.</param>
    /// <param name="action">The action to run.</param>
    /// <returns>A task representing the action.</returns>
    internal Task Run(string name, Func<Task> action) => _write is null ? action() : RunMeasured(name, action);

    /// <summary>
    /// Runs a synchronous action, measuring it when recording.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="name">The phase name.</param>
    /// <param name="action">The action to run.</param>
    /// <returns>The result of the action.</returns>
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

    /// <summary>
    /// Emits the summary record once.
    /// </summary>
    internal void Complete()
    {
        if (_write is null || Interlocked.Exchange(ref _completed, 1) != 0) return;
        Emit("CHRONICLE_INTEGRATION_SUMMARY ", null, ("finished", _open == 0 && !_outputFailed ? "complete" : "partial", Elapsed(_started)));
    }

    /// <summary>
    /// Emits an aggregate record for a named set of commands.
    /// </summary>
    /// <param name="name">The aggregate name.</param>
    /// <param name="totals">The totals.</param>
    internal void Aggregate(string name, CommandTotals totals)
    {
        if (_write is null) return;
        using var phase = Phase.CreateDetached(this, name, Interlocked.Increment(ref _sequence), _started);
        Emit("CHRONICLE_INTEGRATION_TIMING ", phase, (totals.Failures > 0 ? "failure" : "success", totals.Observed ? "complete" : "partial", totals.ElapsedMs), totals);
    }

    static string Label(string name) => Environment.GetEnvironmentVariable(name) ?? "unavailable";

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

    double Elapsed(long started) => (_clock() - started) * 1000d / _frequency;

    void Emit(string prefix, Phase? phase, (string Outcome, string Completeness, double Elapsed) result, CommandTotals? aggregate = null)
    {
        using var detached = phase is null ? Phase.CreateDetached(this, "instance", Interlocked.Increment(ref _sequence), _started) : null;
        phase ??= detached!;
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

    /// <summary>
    /// Represents the totals of a set of observed commands.
    /// </summary>
    /// <param name="ElapsedMs">The total elapsed milliseconds.</param>
    /// <param name="Count">The number of commands.</param>
    /// <param name="Failures">The number of failed commands.</param>
    /// <param name="Observed">Whether all commands were observed.</param>
    internal readonly record struct CommandTotals(double ElapsedMs, int Count, int Failures, bool Observed);

    /// <summary>
    /// Represents a measured phase, closed when disposed.
    /// </summary>
    /// <param name="owner">The owning recorder.</param>
    /// <param name="name">The phase name.</param>
    /// <param name="parent">The parent phase, if any.</param>
    /// <param name="sequence">The sequence number.</param>
    /// <param name="started">The start timestamp.</param>
    internal sealed class Phase(IntegrationTimings owner, string name, Phase? parent, long sequence, long started) : IDisposable
    {
        string _outcome = "unobserved";
        int _disposed;
        int _polls;
        int _retries;
        int _targets;
        int _tables = -1;

        /// <summary>
        /// Gets the owning recorder.
        /// </summary>
        internal IntegrationTimings Owner { get; } = owner;

        /// <summary>
        /// Gets the phase name.
        /// </summary>
        internal string Name { get; } = name;

        /// <summary>
        /// Gets the parent phase.
        /// </summary>
        internal Phase? Parent { get; } = parent;

        /// <summary>
        /// Gets the sequence number.
        /// </summary>
        internal long Sequence { get; } = sequence;

        /// <summary>
        /// Gets the start timestamp.
        /// </summary>
        internal long Started { get; } = started;

        /// <summary>
        /// Gets the number of observed targets.
        /// </summary>
        internal int Targets => _targets;

        /// <summary>
        /// Gets the number of observed tables, or null when not observed.
        /// </summary>
        internal int? Tables => _tables < 0 ? null : _tables;

        /// <summary>
        /// Gets the number of polls.
        /// </summary>
        internal int Polls => _polls;

        /// <summary>
        /// Gets the number of retries.
        /// </summary>
        internal int Retries => _retries;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _current.Value = Parent;
            Interlocked.Decrement(ref Owner._open);
            Owner.Emit("CHRONICLE_INTEGRATION_TIMING ", this, (_outcome, "complete", Owner.Elapsed(Started)));
        }

        /// <summary>
        /// Creates a phase that is only used to describe a record and never alters the ambient state.
        /// </summary>
        /// <param name="owner">The owning recorder.</param>
        /// <param name="name">The phase name.</param>
        /// <param name="sequence">The sequence number.</param>
        /// <param name="started">The start timestamp.</param>
        /// <returns>The detached phase.</returns>
        internal static Phase CreateDetached(IntegrationTimings owner, string name, long sequence, long started)
        {
            return new Phase(owner, name, null, sequence, started) { _disposed = 1 };
        }

        /// <summary>
        /// Records an observed target.
        /// </summary>
        internal void Target() => Interlocked.Increment(ref _targets);

        /// <summary>
        /// Records that tables were observed.
        /// </summary>
        internal void TablesObserved() => Interlocked.CompareExchange(ref _tables, 0, -1);

        /// <summary>
        /// Records an observed table.
        /// </summary>
        internal void Table() => Interlocked.Increment(ref _tables);

        /// <summary>
        /// Records a poll.
        /// </summary>
        internal void Poll() => Interlocked.Increment(ref _polls);

        /// <summary>
        /// Records a retry.
        /// </summary>
        internal void Retry() => Interlocked.Increment(ref _retries);

        /// <summary>
        /// Marks the phase successful.
        /// </summary>
        internal void Success() => _outcome = "success";

        /// <summary>
        /// Marks the phase successful when it completed without timing out.
        /// </summary>
        /// <param name="successful">Whether the work succeeded.</param>
        /// <param name="timedOut">Whether the work timed out.</param>
        internal void SuccessUnlessIncomplete(bool successful, bool timedOut)
        {
            if (successful && !timedOut) Success();
        }

        /// <summary>
        /// Marks the phase failed.
        /// </summary>
        internal void Failure() => _outcome = "failure";
    }
}
