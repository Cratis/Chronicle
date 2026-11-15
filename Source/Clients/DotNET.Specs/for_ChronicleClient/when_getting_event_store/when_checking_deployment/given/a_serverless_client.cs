// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_ChronicleClient.when_checking_deployment.given;

public class a_serverless_client : Specification
{
    protected ChronicleClient _client;
    protected ChronicleOptions _options;
    protected IServices _services;
    protected IChronicleConnection _connection;
    int _queries;
    protected readonly TaskCompletionSource _bothEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<QueryResult<EventStoreDeploymentResponse>> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly DiagnosticLogger _logger = new();

    void Establish()
    {
        _connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IConnectionKeepAlive>();
        ((IConnectionKeepAlive)_connection).IsSkipped.Returns(true);
        var lifecycle = Substitute.For<IConnectionLifecycle>();
        lifecycle.IsConnected.Returns(true);
        _connection.Lifecycle.Returns(lifecycle);
        _services = Substitute.For<IServices>();
        ((IChronicleServicesAccessor)_connection).Services.Returns(_services);
        _services.EventStores.GetEventStoreDeployment(Arg.Any<GetEventStoreDeploymentRequest>(), Arg.Any<CallContext>()).Returns(async call =>
        {
            var token = call.Arg<CallContext>().CancellationToken;
            _entered.TrySetResult();
            if (Interlocked.Increment(ref _queries) == 2) _bothEntered.TrySetResult();
            try
            {
                return await _response.Task.WaitAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Observe cancellation of the RPC token itself; a Register callback can lose a race with WaitAsync's own registration.
                _cancelled.TrySetResult();
                throw;
            }
        });
        _options = ChronicleOptions.ForServerless("chronicle://localhost:35000/", "deployment-check", new Uri("https://example.com"));
        _options.Serverless.DeploymentGeneration = 1;
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
    }

    protected void CreateClient()
    {
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(_logger);
        _client = new(_connection, _options, Substitute.For<IClientArtifactsProvider>(), loggerFactory: factory);
    }

    protected void CompleteQuery(ulong generation, string fingerprint) => _response.TrySetResult(new() { Data = new() { Generation = generation, Fingerprint = fingerprint } });

    void Destroy()
    {
        _client?.Dispose();
        _response.TrySetCanceled();
    }

    public record PlainEvent(string Name);

    public class DiagnosticLogger : ILogger
    {
        public TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public LogLevel Level { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public Exception? Failure { get; private set; }
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (!message.Contains("deployment definitions", StringComparison.OrdinalIgnoreCase) && !message.Contains("deployment fingerprint", StringComparison.OrdinalIgnoreCase)) return;
            Level = logLevel;
            Message = message;
            Failure = exception;
            Finished.TrySetResult();
        }
    }
}
