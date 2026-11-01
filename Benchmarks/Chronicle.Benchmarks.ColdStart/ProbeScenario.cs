// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Runs one cold-start scenario in this process and prints one JSON line with its timings and allocations.
/// </summary>
public static class ProbeScenario
{
    /// <summary>
    /// The scenario that only connects.
    /// </summary>
    public const string Connect = "connect";

    /// <summary>
    /// The scenario that connects and auto discovers and registers the artifacts of this assembly.
    /// </summary>
    public const string Auto = "auto";

    /// <summary>
    /// Runs the scenario.
    /// </summary>
    /// <param name="scenario">The scenario name.</param>
    /// <param name="mainStart">The <see cref="Stopwatch.GetTimestamp"/> value taken at the start of Main.</param>
    /// <returns>Process exit code.</returns>
    public static async Task<int> Run(string scenario, long mainStart)
    {
        var connectionString = Environment.GetEnvironmentVariable(ColdStartKernel.EnvironmentVariable);
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.Error.WriteLine($"{ColdStartKernel.EnvironmentVariable} is not set; run through probe-loop.");
            return 2;
        }

        var runtimeStartMs = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds - Stopwatch.GetElapsedTime(mainStart).TotalMilliseconds;
        var allocatedAtMain = GC.GetTotalAllocatedBytes(precise: true);

        var options = new ChronicleOptions(
            connectionString: new ChronicleConnectionString(connectionString),
            autoDiscoverAndRegister: scenario == Auto,
            connectTimeout: 30);

        using var client = new ChronicleClient(options);
        var p1 = Stopwatch.GetElapsedTime(mainStart).TotalMilliseconds;

        var eventStore = await client.GetEventStore(ColdStartKernel.EventStoreName);
        var p2 = Stopwatch.GetElapsedTime(mainStart).TotalMilliseconds;
        var allocatedAtReady = GC.GetTotalAllocatedBytes(precise: true);

        await eventStore.EventLog.Append(EventSourceId.New(), new BenchmarkEvent("Test", 42, DateTimeOffset.UtcNow));
        var p3 = Stopwatch.GetElapsedTime(mainStart).TotalMilliseconds;
        var allocatedAtAppend = GC.GetTotalAllocatedBytes(precise: true);

        Console.WriteLine(JsonSerializer.Serialize(new Dictionary<string, double>
        {
            ["p0RuntimeStartMs"] = runtimeStartMs,
            ["p1ClientCreatedMs"] = p1,
            ["p2ClientReadyMs"] = p2,
            ["p3FirstAppendMs"] = p3,
            ["allocatedAtReadyBytes"] = allocatedAtReady - allocatedAtMain,
            ["allocatedAtFirstAppendBytes"] = allocatedAtAppend - allocatedAtMain,
        }));
        return 0;
    }
}
