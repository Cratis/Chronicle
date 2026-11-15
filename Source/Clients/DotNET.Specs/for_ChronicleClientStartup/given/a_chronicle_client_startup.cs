// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.for_ChronicleClientStartup.given;

public class a_chronicle_client_startup : Specification
{
    protected ChronicleClientOptions _options;
    protected IChronicleClient _client;
    protected IEventStore _eventStore;
    protected IChronicleConnection _connection;
    protected IHostEnvironment _environment;
    protected IHostApplicationLifetime _lifetime;
    protected CancellationTokenSource _started;
    protected CancellationTokenSource _stopping;
    internal CapturingLogger<ChronicleClientStartup> _logger;
    protected bool _shouldConnect;
    protected TaskCompletionSource _connectCalled;
    internal ChronicleClientStartup _startup;

    void Establish()
    {
        _options = new ChronicleClientOptions { EventStore = "the-store" };
        _client = Substitute.For<IChronicleClient>();
        _eventStore = Substitute.For<IEventStore>();
        _connection = Substitute.For<IChronicleConnection>();
        _environment = Substitute.For<IHostEnvironment>();
        _environment.EnvironmentName.Returns(Environments.Development);
        _started = new();
        _stopping = new();
        _lifetime = Substitute.For<IHostApplicationLifetime>();
        _lifetime.ApplicationStarted.Returns(_started.Token);
        _lifetime.ApplicationStopping.Returns(_stopping.Token);
        _logger = new();
        _shouldConnect = true;
        _connectCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        _eventStore.Connection.Returns(_connection);
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromResult(_eventStore));
        _connection.Connect().Returns(_ =>
        {
            _connectCalled.TrySetResult();
            return Task.CompletedTask;
        });

        _startup = new(_options, _client, _environment, _lifetime, _logger, () => _shouldConnect);
    }

    protected async Task StartAndSignalStarted()
    {
        await _startup.StartAsync(CancellationToken.None);
        await _started.CancelAsync();
    }

    internal class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public TaskCompletionSource WarningLogged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (Entries)
            {
                Entries.Add((logLevel, formatter(state, exception)));
            }

            if (logLevel == LogLevel.Warning)
            {
                WarningLogged.TrySetResult();
            }
        }
    }
}
