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
    /// <summary>
    /// The name of the event store used when nothing else is specified.
    /// </summary>
    public const string DefaultEventStoreName = "benchmarks";

    readonly ChronicleClient _client;
    readonly ILoggerFactory _loggerFactory;
    readonly ChronicleBenchmarkFixture _fixture;
    readonly EventStoreName _eventStoreName;
    IEventStore? _eventStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClientHelper"/> class.
    /// </summary>
    /// <param name="fixture">The fixture that manages the Chronicle test container.</param>
    public ChronicleClientHelper(ChronicleBenchmarkFixture fixture)
        : this(fixture, DefaultEventStoreName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClientHelper"/> class.
    /// </summary>
    /// <param name="fixture">The fixture that manages the Chronicle test container.</param>
    /// <param name="eventStoreName">The <see cref="EventStoreName"/> to work against.</param>
    /// <param name="artifactsProvider">Optional <see cref="IClientArtifactsProvider"/> narrowing the artifacts that get registered.</param>
    /// <remarks>
    /// Observers, read models and event sequences all live within an event store, so giving every benchmark case its
    /// own event store keeps the state of one case out of the way of every other case running in the same process.
    /// </remarks>
    public ChronicleClientHelper(
        ChronicleBenchmarkFixture fixture,
        EventStoreName eventStoreName,
        IClientArtifactsProvider? artifactsProvider = null)
    {
        _fixture = fixture;
        _eventStoreName = eventStoreName;
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        _ = _fixture.Container;

        var options = new ChronicleOptions(
            connectionString: new ChronicleConnectionString(_fixture.ChronicleUrl),
            connectTimeout: 30);

        _client = new ChronicleClient(options, artifactsProvider: artifactsProvider, loggerFactory: _loggerFactory);
        _eventStore = _client.GetEventStore(_eventStoreName).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the event store used by the benchmarks.
    /// </summary>
    public IEventStore EventStore => _eventStore!;

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
                _eventStore ??= await _client.GetEventStore(_eventStoreName);
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
