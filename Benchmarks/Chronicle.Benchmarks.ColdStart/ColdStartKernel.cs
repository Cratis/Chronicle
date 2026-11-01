// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.ColdStart;

/// <summary>
/// Owns the kernel the cold-start probes run against, from the host process.
/// </summary>
public sealed class ColdStartKernel : IAsyncDisposable
{
    /// <summary>
    /// The environment variable that names an already running kernel by its connection string, and through which the
    /// host hands the kernel to the child processes it launches.
    /// </summary>
    public const string EnvironmentVariable = "CHRONICLE_COLDSTART_KERNEL";

    /// <summary>
    /// The event store every probe uses.
    /// </summary>
    public const string EventStoreName = "coldstart";

    ChronicleBenchmarkFixture? _fixture;

    /// <summary>
    /// Starts a kernel container, unless <see cref="EnvironmentVariable"/> names one, and warms it so no measured launch
    /// pays for creating the store and namespace or for registering the event types.
    /// </summary>
    /// <returns>A task that completes when the kernel is warm.</returns>
    public async Task Start()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            _fixture = new ChronicleBenchmarkFixture();
            _ = _fixture.Container;
            Environment.SetEnvironmentVariable(EnvironmentVariable, _fixture.ChronicleUrl);
        }

        var options = new ChronicleOptions(
            connectionString: new ChronicleConnectionString(Environment.GetEnvironmentVariable(EnvironmentVariable)!),
            connectTimeout: 30);
        using var client = new ChronicleClient(options);
        var eventStore = await client.GetEventStore(EventStoreName);
        await eventStore.EventLog.Append(EventSourceId.New(), new BenchmarkEvent("Warm", 1, DateTimeOffset.UtcNow));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
        }
    }
}
