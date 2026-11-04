// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using ReactorService = Cratis.Chronicle.Reactors.Reactors;

namespace Cratis.Chronicle.Integration.SqlClustering.for_ClusterLoadDeliveryDiagnostics;

public class when_capturing_the_enabled_transport : Specification
{
    ClusterLoadDeliveryDiagnostics _diagnostics = null!;
    readonly ConcurrentQueue<(Guid, ulong)> _queue = new();
    readonly Guid _stream = Guid.NewGuid();
    readonly Guid _source = Guid.NewGuid();
    readonly Guid[] _ids = Enumerable.Range(0, 10101).Select(_ => Guid.NewGuid()).ToArray();
    ILoggerFactory _factory = null!;
    ILogger<ReactorService> _logger = null!;
    Emit _emit = null!;
    string _report = string.Empty;
    bool _enabled;
    bool _sampleWritten;
    IDisposable? _scope;
    ActivitySource? _activitySource;
    IDisposable? _span;
    long _rateBytes;
    long _rateTicks;
    long _burstBytes;
    TimeSpan _burstElapsed;
    long _baselineRateBytes;
    long _baselineRateTicks;
    long _baselineBurstBytes;
    TimeSpan _baselineBurstElapsed;

    delegate void Emit(ILogger<ReactorService> logger, int stage, Guid stream, long batch, ulong delivery, ulong sequence, ulong previous, int state, int count, Guid partition, Guid type, Guid source, Guid correlation, int redacted);

    async Task Because()
    {
        var type = typeof(ChronicleClient).Assembly.GetType("Cratis.Chronicle.Reactors.ReactorsDeliveryDiagnosticsLogging");
        _enabled = type is not null;
        var samplePath = Environment.GetEnvironmentVariable("CRATIS_ROLLING_OVERHEAD_SAMPLE");
        if (!string.IsNullOrEmpty(samplePath))
        {
            await MeasureArm(type, samplePath);
            return;
        }
        _diagnostics = new();
        if (!_enabled) return;
        InitializeTransport(type!, 10021);
        var baselineQueue = new ConcurrentQueue<(Guid, ulong)>();
        EmitEvent(0); // Warm the actual source-generated logger/provider/callback path outside measurement.
        for (ulong sequence = 1; sequence <= 20; sequence++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            var baselineAllocated = GC.GetAllocatedBytesForCurrentThread();
            var baselineStarted = Stopwatch.GetTimestamp();
            baselineQueue.Enqueue((_ids[(int)sequence], sequence));
            _baselineRateTicks += Stopwatch.GetTimestamp() - baselineStarted;
            _baselineRateBytes += GC.GetAllocatedBytesForCurrentThread() - baselineAllocated;
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            EmitEvent(sequence);
            _rateTicks += Stopwatch.GetTimestamp() - started;
            _rateBytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
        }
        MeasureBaselineBurst(baselineQueue);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        for (ulong sequence = 21; sequence <= 10020; sequence++) EmitEvent(sequence);
        timer.Stop();
        _burstElapsed = timer.Elapsed;
        _burstBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        EmitStage(8, sequence: 10020);
        EmitStage(9, sequence: 10020);
        EmitStage(2);
        _report = _diagnostics.Describe([(_ids[1], 1UL)], []);
        Console.WriteLine($"Original queue baseline: rate=20/s samples=20 enqueue-ms={TimeSpan.FromSeconds((double)_baselineRateTicks / Stopwatch.Frequency).TotalMilliseconds:F3} bytes={_baselineRateBytes}; burst=10000 elapsed-ms={_baselineBurstElapsed.TotalMilliseconds:F3} bytes={_baselineBurstBytes}.");
        Console.WriteLine($"Diagnostic transport+collector+fixture queue: rate=20/s samples=20 hook-ms={TimeSpan.FromSeconds((double)_rateTicks / Stopwatch.Frequency).TotalMilliseconds:F3} bytes={_rateBytes}; burst=10000 elapsed-ms={_burstElapsed.TotalMilliseconds:F3} bytes={_burstBytes}; storage-upper-bound={_diagnostics.StorageUpperBound}. Not a production benchmark; enabled diagnostics can perturb recurrence.");
    }

    void InitializeTransport(Type type, int count)
    {
        _emit = type.GetMethod("Record", BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate<Emit>();
        var beginScope = type.GetMethod("BeginScope", BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate<Func<ILogger<ReactorService>, Guid, long, IDisposable?>>();
        _factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new ClusterLoadDeliveryDiagnosticsLogging(_diagnostics)));
        _logger = _factory.CreateLogger<ReactorService>();
        EmitStage(1);
        EmitStage(5, count: count);
        _scope = beginScope(_logger, _stream, 1);
        _activitySource = new ActivitySource(ClusterLoadDeliveryDiagnosticsLogging.Category);
        var traces = typeof(ChronicleClient).Assembly.GetType("Cratis.Chronicle.Reactors.ReactorTraces")!;
        _span = (IDisposable)traces.GetMethod("Handle", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [new TraceSource(_activitySource), "synthetic-store", "synthetic-namespace", "synthetic-sequence", "cluster-integrity"])!;
    }

    async Task MeasureArm(Type? type, string samplePath)
    {
        if (_enabled)
        {
            _diagnostics = new();
            InitializeTransport(type!, 10100);
        }

        // Warm the same generated transport/callback path in both arms before measuring.
        for (var index = 0; index < 256; index++) MeasureEvent(0);
        while (_queue.TryDequeue(out _)) { }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var window = Stopwatch.StartNew();
        for (ulong sequence = 1; sequence <= 100; sequence++)
        {
            // Absolute scheduling avoids accumulating callback duration into the 20/s cadence.
            var delay = TimeSpan.FromMilliseconds(sequence * 50) - window.Elapsed;
            if (delay > TimeSpan.Zero) await Task.Delay(delay);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            MeasureEvent(sequence);
            _rateTicks += Stopwatch.GetTimestamp() - started;
            _rateBytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
        }
        window.Stop();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var burstStarted = Stopwatch.GetTimestamp();
        for (ulong sequence = 101; sequence <= 10100; sequence++) MeasureEvent(sequence);
        var burstTicks = Stopwatch.GetTimestamp() - burstStarted;
        _burstBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        var sample = new
        {
            schemaVersion = 1,
            enabled = _enabled,
            elapsedBasis = "synchronous event work only; pacing delays excluded",
            steady = new { events = 100, eventsPerSecond = 20, windowMilliseconds = window.Elapsed.TotalMilliseconds, elapsedMilliseconds = TimeSpan.FromSeconds((double)_rateTicks / Stopwatch.Frequency).TotalMilliseconds, allocatedBytes = _rateBytes },
            burst = new { events = 10000, elapsedMilliseconds = TimeSpan.FromSeconds((double)burstTicks / Stopwatch.Frequency).TotalMilliseconds, allocatedBytes = _burstBytes },
            retainedStateUpperBoundBytes = _enabled ? _diagnostics.StorageUpperBound : 0
        };
        await File.WriteAllTextAsync(samplePath, JsonSerializer.Serialize(sample));
        _sampleWritten = true;
    }

    void MeasureEvent(ulong sequence)
    {
        if (_enabled) EmitEvent(sequence);
        else _queue.Enqueue((_ids[(int)sequence], sequence));
    }

    void MeasureBaselineBurst(ConcurrentQueue<(Guid Id, ulong Sequence)> queue)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        for (ulong sequence = 21; sequence <= 10020; sequence++) queue.Enqueue((_ids[(int)sequence], sequence));
        timer.Stop();
        _baselineBurstElapsed = timer.Elapsed;
        _baselineBurstBytes = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    void EmitStage(int stage, ulong sequence = ulong.MaxValue, int count = 0) => _emit(_logger, stage, _stream, 1, 42, sequence, ulong.MaxValue, 0, count, _source, Guid.Empty, _source, Guid.Empty, 1);

    void EmitEvent(ulong sequence)
    {
        EmitStage(6, sequence);
        _diagnostics.Callback(_ids[(int)sequence], sequence, () => _queue.Enqueue((_ids[(int)sequence], sequence)));
        EmitStage(7, sequence);
    }

    void Destroy()
    {
        _span?.Dispose();
        _activitySource?.Dispose();
        _scope?.Dispose();
        _factory?.Dispose();
        _diagnostics?.Dispose();
    }

    sealed class TraceSource(ActivitySource source) : IActivitySource<ReactorService>
    {
        public ActivitySource ActualSource { get; } = source;
    }

    [Fact] public void should_write_the_overhead_sample_when_requested() => (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CRATIS_ROLLING_OVERHEAD_SAMPLE")) || _sampleWritten).ShouldBeTrue();
    [Fact] public void should_capture_the_actual_generated_handle_span_when_enabled() => (!_enabled || _report.Contains("handle-spans=1", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] public void should_keep_each_recent_record_within_128_bytes() => (_diagnostics.RecentRecordBytes <= 128).ShouldBeTrue();
    [Fact] public void should_capture_actual_generated_structured_logs_when_enabled() => (!_enabled || _report.Contains("membership-complete=True", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] public void should_keep_early_batch_results_after_ring_wrap_when_enabled() => (!_enabled || _report.Contains("publish-attempt=True publish-return=True", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] public void should_correlate_the_callback_to_the_actual_dispatch_scope_when_enabled() => (!_enabled || _report.Contains($"last-stream={_stream} last-batch=1", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] public void should_preserve_all_original_enqueues_when_enabled() => (!_enabled || _queue.Count == 10021).ShouldBeTrue();
    [Fact] public void should_keep_the_storage_bound() => (_diagnostics.StorageUpperBound <= 16 * 1024 * 1024).ShouldBeTrue();
}
