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
    readonly ILoggerFactory _loggerFactory;
    readonly ChronicleBenchmarkFixture _fixture;
    IEventStore? _eventStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClientHelper"/> class.
    /// </summary>
    /// <param name="fixture">The fixture that manages the Chronicle test container.</param>
    public ChronicleClientHelper(ChronicleBenchmarkFixture fixture)
    {
        _fixture = fixture;
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        _ = _fixture.Container;

        var options = new ChronicleOptions(
            connectionString: new ChronicleConnectionString(_fixture.ChronicleUrl),
            connectTimeout: 30)
        {
            ManagementPort = 8081
        };

        _client = new ChronicleClient(options, loggerFactory: _loggerFactory);
        _eventStore = _client.GetEventStore("benchmarks").GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the event log used by the benchmarks.
    /// </summary>
    public IEventLog EventLog => _eventStore!.EventLog;

    /// <summary>
    /// Waits until the Chronicle connection is ready to accept requests.
    /// </summary>
    /// <returns>A task that completes when the Chronicle connection is ready.</returns>
    public async Task WaitForConnection()
    {
        const int retries = 10;

        for (var i = 0; i < retries; i++)
        {
            try
            {
                _eventStore ??= await _client.GetEventStore("benchmarks");
                await _eventStore.EventLog.GetTailSequenceNumber();
                return;
            }
            catch (Exception) when (i < retries - 1)
            {
                _eventStore = null;
                var delay = TimeSpan.FromMilliseconds(500 * (1 << i));
                await Task.Delay(delay);
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
