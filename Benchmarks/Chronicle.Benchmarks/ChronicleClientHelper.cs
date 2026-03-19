// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Provides Chronicle client access for the benchmark suite.
/// </summary>
public class ChronicleClientHelper : IDisposable
{
    readonly ChronicleClient _client;
    readonly IEventStore _eventStore;
    readonly ILoggerFactory _loggerFactory;
    readonly ChronicleBenchmarkFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClientHelper"/> class.
    /// </summary>
    /// <param name="fixture">The fixture that manages the Chronicle test container.</param>
    public ChronicleClientHelper(ChronicleBenchmarkFixture fixture)
    {
        _fixture = fixture;
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        var options = new ChronicleOptions(
            connectionString: new ChronicleConnectionString(_fixture.ChronicleUrl),
            connectTimeout: 30);

        _client = new ChronicleClient(options, loggerFactory: _loggerFactory);
        _eventStore = _client.GetEventStore("benchmarks").GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the event log used by the benchmarks.
    /// </summary>
    public IEventLog EventLog => _eventStore.EventLog;

    /// <summary>
    /// Waits until the Chronicle connection is ready to accept requests.
    /// </summary>
    /// <returns>A task that completes when the Chronicle connection is ready.</returns>
    public async Task WaitForConnection()
    {
        const int retries = 5;
        var delay = 200;

        for (var i = 0; i < retries; i++)
        {
            try
            {
                await _eventStore.EventLog.GetTailSequenceNumber();
                return;
            }
            catch (Exception) when (i < retries - 1)
            {
                await Task.Delay(delay);
                delay *= 2;
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _client.Dispose();
        _loggerFactory.Dispose();
        GC.SuppressFinalize(this);
    }
}
