// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Verifies each metric path independently, then retains the same non-inlined callbacks as timed no-ops.
/// </summary>
sealed class TelemetryMetricValidation
{
    const int ValidationCalls = 1000;
    Dictionary<(string Name, Type MeasurementType), int>? _measurements;

    /// <summary>Registers all numeric types used by the telemetry producers before listener activation.</summary>
    /// <param name="listener">The benchmark listener.</param>
    internal void AttachCallbacks(MeterListener listener)
    {
        listener.SetMeasurementEventCallback<int>(OnMeasurement);
        listener.SetMeasurementEventCallback<long>(OnMeasurement);
        listener.SetMeasurementEventCallback<double>(OnMeasurement);
        listener.SetMeasurementEventCallback<float>(OnMeasurement);
    }

    /// <summary>Checks the exact per-instrument int measurement counts for one benchmark method.</summary>
    /// <param name="method">The benchmark method to invoke independently.</param>
    /// <param name="names">The expected instruments, each recording once per invocation.</param>
    /// <exception cref="InvalidOperationException">A measurement is missing, duplicated, unexpected, or of an unexpected numeric type.</exception>
    internal void Verify(Action method, params string[] names)
    {
        _measurements = [];
        for (var call = 0; call < ValidationCalls; call++)
        {
            method();
        }

        if (_measurements.Count != names.Length || names.Any(name => _measurements.GetValueOrDefault((name, typeof(int))) != ValidationCalls))
        {
            var received = string.Join(", ", _measurements.Select(entry => $"{entry.Key.Name}<{entry.Key.MeasurementType.Name}>={entry.Value}"));
            throw new InvalidOperationException($"{method.Method.Name}: expected {ValidationCalls} int measurements each for [{string.Join(", ", names)}]; received [{received}].");
        }

        Console.WriteLine($"Validated {method.Method.Name}: {ValidationCalls} calls; {string.Join(", ", names.Select(name => $"{name}<Int32>={ValidationCalls}"))}; total={_measurements.Values.Sum()}.");
        _measurements = null;
    }

    /// <summary>
    /// This exact callback remains installed during timing. NoInlining prevents the no-op from
    /// disappearing into Counter.Add; the null check avoids dictionary work or allocations while timed.
    /// </summary>
    /// <typeparam name="T">The numeric measurement type.</typeparam>
    /// <param name="instrument">The publishing instrument.</param>
    /// <param name="measurement">The recorded value.</param>
    /// <param name="tags">The recorded tags.</param>
    /// <param name="state">The listener state.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    void OnMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
    {
        if (_measurements is { } measurements)
        {
            var key = (instrument.Name, typeof(T));
            measurements[key] = measurements.GetValueOrDefault(key) + 1;
        }
    }
}
