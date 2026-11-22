// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// Represents an implementation of <see cref="IKernelLoad"/>.
/// </summary>
/// <remarks>
/// <para>
/// It follows the grain calls of this silo from two sides: the calls the silo makes, from the moment they are sent to
/// the moment they are answered (running or queued at the grain, which is what a client's timeout is made of), and the
/// calls running on the silo's own grains, which for an interleaved grain is the number that grew to almost ten thousand
/// in the incident this was built for. Neither needs a grain: both are counted by the grain call filters in memory.
/// </para>
/// <para>
/// The status is derived from those numbers and the <see cref="Overload"/> limits. It turns degraded the moment a limit
/// is crossed and goes through recovering on the way back, so a silo that flaps around a limit does not flap its report.
/// </para>
/// <para>
/// The instruments are new and have no legacy name, so they are published under the convention names whatever
/// Telemetry:MetricNames selects. The attributes are the grain interface and, where the grain's key carries one, the
/// event store: never a grain key or an event source.
/// </para>
/// </remarks>
public sealed class KernelLoad : IKernelLoad
{
    static readonly TimeSpan _evaluationInterval = TimeSpan.FromSeconds(1);

    readonly ConcurrentDictionary<long, PendingEntry> _pending = new();
    readonly ConcurrentDictionary<Key, Counter> _running = new();
    readonly ConcurrentDictionary<Key, byte> _keys = new();
    readonly IOptions<ChronicleOptions> _options;
    readonly ILogger<KernelLoad> _logger;
    readonly TimeProvider _timeProvider;
    readonly object _evaluationLock = new();
    long _nextCall;
    int _openStreams;
    long _evaluatedAt;
    long _lastDegraded;
    volatile bool _hasEvaluated;
    KernelLoadStatus _status = KernelLoadStatus.Accepting;
    IReadOnlyList<string> _reasons = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelLoad"/> class.
    /// </summary>
    /// <param name="options">The <see cref="IOptions{TOptions}"/> of <see cref="ChronicleOptions"/> holding the limits.</param>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/> for logging.</param>
    /// <param name="meter">The <see cref="IMeter{T}"/> owning the instruments, if the host has one.</param>
    public KernelLoad(
        IOptions<ChronicleOptions> options,
        ILogger<KernelLoad> logger,
        [FromKeyedServices(WellKnown.MeterName)] IMeter<KernelLoad>? meter = null)
        : this(options, logger, meter?.ActualMeter, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelLoad"/> class.
    /// </summary>
    /// <param name="options">The <see cref="IOptions{TOptions}"/> of <see cref="ChronicleOptions"/> holding the limits.</param>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/> for logging.</param>
    /// <param name="meter">The <see cref="Meter"/> owning the instruments, if any.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to measure with.</param>
    internal KernelLoad(IOptions<ChronicleOptions> options, ILogger<KernelLoad> logger, Meter? meter, TimeProvider timeProvider)
    {
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider;
        RegisterInstruments(meter);
    }

    /// <inheritdoc/>
    public KernelLoadStatus Status
    {
        get
        {
            Evaluate();
            return _status;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> Reasons
    {
        get
        {
            Evaluate();
            return _reasons;
        }
    }

    /// <inheritdoc/>
    public KernelLoadReport Report(int busiest = 20)
    {
        Evaluate();
        var grains = Snapshot()
            .OrderByDescending(_ => _.OldestPendingAge)
            .ThenByDescending(_ => _.Pending)
            .ThenByDescending(_ => _.Running)
            .Take(busiest)
            .ToArray();
        return new KernelLoadReport(_status, _reasons, grains, Volatile.Read(ref _openStreams));
    }

    /// <inheritdoc/>
    public PendingCall BeginPending(string grainType, string eventStore)
    {
        var key = new Key(grainType, eventStore);
        _keys.TryAdd(key, 0);
        var id = Interlocked.Increment(ref _nextCall);
        _pending[id] = new PendingEntry(key, _timeProvider.GetTimestamp());
        return new PendingCall(id);
    }

    /// <inheritdoc/>
    public void EndPending(PendingCall call) => _pending.TryRemove(call.Id, out _);

    /// <inheritdoc/>
    public void BeginRunning(string grainType, string eventStore)
    {
        var key = new Key(grainType, eventStore);
        _keys.TryAdd(key, 0);
        Interlocked.Increment(ref _running.GetOrAdd(key, static _ => new Counter()).Value);
    }

    /// <inheritdoc/>
    public void EndRunning(string grainType, string eventStore)
    {
        if (_running.TryGetValue(new Key(grainType, eventStore), out var counter))
        {
            Interlocked.Decrement(ref counter.Value);
        }
    }

    /// <inheritdoc/>
    public void StreamOpened() => Interlocked.Increment(ref _openStreams);

    /// <inheritdoc/>
    public void StreamClosed() => Interlocked.Decrement(ref _openStreams);

    static KeyValuePair<string, object?>[] TagsOf(GrainLoad load) => string.IsNullOrEmpty(load.EventStore)
        ? [new(WellKnownTelemetryAttributes.GrainType, load.GrainType)]
        : [new(WellKnownTelemetryAttributes.GrainType, load.GrainType), new(WellKnownTelemetryAttributes.EventStoreName, load.EventStore)];

    static string Describe(GrainLoad load) => string.IsNullOrEmpty(load.EventStore) ? load.GrainType : $"{load.GrainType} ({load.EventStore})";

    void RegisterInstruments(Meter? meter)
    {
        if (meter is null)
        {
            return;
        }

        meter.CreateObservableGauge(
            WellKnownMetricNames.GrainCallsRunning,
            () => Snapshot().Select(_ => new Measurement<int>(_.Running, TagsOf(_))).ToArray(),
            WellKnownMetricNames.GrainCallsRunningUnit,
            "Number of calls running on the grains of this silo, interleaved or not, per grain type and event store");
        meter.CreateObservableGauge(
            WellKnownMetricNames.GrainCallsPending,
            () => Snapshot().Select(_ => new Measurement<int>(_.Pending, TagsOf(_))).ToArray(),
            WellKnownMetricNames.GrainCallsPendingUnit,
            "Number of calls this silo has made to grains that have not been answered, per grain type and event store");
        meter.CreateObservableGauge(
            WellKnownMetricNames.GrainCallsOldestPendingAge,
            () => Snapshot().Select(_ => new Measurement<double>(_.OldestPendingAge.TotalSeconds, TagsOf(_))).ToArray(),
            WellKnownMetricNames.GrainCallsOldestPendingAgeUnit,
            "How long the oldest unanswered call this silo made to a grain has waited, per grain type and event store");
        meter.CreateObservableGauge(
            WellKnownMetricNames.KernelLoadStatus,
            () => (int)Status,
            WellKnownMetricNames.KernelLoadStatusUnit,
            "The load status of this silo: 0 accepting, 1 degraded, 2 recovering");
        meter.CreateObservableGauge(
            WellKnownMetricNames.GrpcStreamsOpen,
            () => Volatile.Read(ref _openStreams),
            WellKnownMetricNames.GrpcStreamsOpenUnit,
            "Number of gRPC streaming calls this silo is serving");
    }

    GrainLoad[] Snapshot()
    {
        var now = _timeProvider.GetTimestamp();
        var pending = new Dictionary<Key, (int Count, long Oldest)>();
        foreach (var entry in _pending.Values)
        {
            pending.TryGetValue(entry.Key, out var current);
            pending[entry.Key] = (current.Count + 1, current.Count == 0 ? entry.Started : Math.Min(current.Oldest, entry.Started));
        }

        var loads = new List<GrainLoad>();
        foreach (var key in _keys.Keys)
        {
            var running = _running.TryGetValue(key, out var counter) ? Math.Max(Volatile.Read(ref counter.Value), 0) : 0;
            pending.TryGetValue(key, out var waiting);
            var age = waiting.Count == 0 ? TimeSpan.Zero : _timeProvider.GetElapsedTime(waiting.Oldest, now);
            loads.Add(new GrainLoad(key.GrainType, key.EventStore, running, waiting.Count, age));
        }

        return [.. loads];
    }

    void Evaluate()
    {
        var overload = _options.Value.Overload;
        var now = _timeProvider.GetTimestamp();
        if (IsFresh(now))
        {
            return;
        }

        lock (_evaluationLock)
        {
            if (IsFresh(now))
            {
                return;
            }

            var reasons = overload.Enabled ? ReasonsFor(overload) : [];
            var status = _status;
            if (reasons.Count > 0)
            {
                _lastDegraded = now;
                status = KernelLoadStatus.Degraded;
            }
            else if (status != KernelLoadStatus.Accepting)
            {
                status = _timeProvider.GetElapsedTime(_lastDegraded, now) < TimeSpan.FromSeconds(overload.RecoverySeconds)
                    ? KernelLoadStatus.Recovering
                    : KernelLoadStatus.Accepting;
            }

            var previous = _status;
            _status = status;
            _reasons = reasons;
            Volatile.Write(ref _evaluatedAt, now);
            _hasEvaluated = true;
            Announce(previous, status, reasons);
        }
    }

    bool IsFresh(long now) => _hasEvaluated && _timeProvider.GetElapsedTime(Volatile.Read(ref _evaluatedAt), now) < _evaluationInterval;

    List<string> ReasonsFor(Overload overload)
    {
        var reasons = new List<string>();
        foreach (var load in Snapshot())
        {
            if (load.OldestPendingAge.TotalSeconds > overload.MaxOldestPendingCallAgeSeconds)
            {
                reasons.Add($"The oldest call to {Describe(load)} has been waiting {load.OldestPendingAge.TotalSeconds:0} s (limit {overload.MaxOldestPendingCallAgeSeconds} s)");
            }

            if (load.Pending > overload.MaxPendingCalls)
            {
                reasons.Add($"{load.Pending} calls to {Describe(load)} are waiting for an answer (limit {overload.MaxPendingCalls})");
            }

            if (load.Running > overload.MaxRunningCalls)
            {
                reasons.Add($"{load.Running} calls are running on {Describe(load)} (limit {overload.MaxRunningCalls})");
            }
        }

        return reasons;
    }

    void Announce(KernelLoadStatus previous, KernelLoadStatus current, List<string> reasons)
    {
        if (previous == current)
        {
            return;
        }

        if (current == KernelLoadStatus.Degraded)
        {
            _logger.KernelDegraded(string.Join("; ", reasons.Take(5)), reasons.Count);
        }
        else
        {
            _logger.KernelLoadStatusChanged(previous, current);
        }
    }

    sealed record Key(string GrainType, string EventStore);

    sealed record PendingEntry(Key Key, long Started);

    sealed class Counter
    {
        public int Value;
    }
}
