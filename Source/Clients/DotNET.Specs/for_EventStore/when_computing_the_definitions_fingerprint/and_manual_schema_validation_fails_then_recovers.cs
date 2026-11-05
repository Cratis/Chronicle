// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Serverless;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_computing_the_definitions_fingerprint;

public class and_manual_schema_validation_fails_then_recovers : when_preparing_for_registration.given.an_explicit_store
{
    bool _diagnosticFinished;
    ILogger _logger;
    Exception _registrationFailure;
    bool _readinessUnchanged;
    bool _gateStayedSuccessful;
    bool _rediscover;

    void Establish()
    {
        _artifacts.EventTypes.Returns([typeof(ManualEvent)]);
        _store = CreateStore();
    }

    async Task Because()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<QueryResult<EventStoreDeploymentResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _services.EventStores.GetEventStoreDeployment(Arg.Any<GetEventStoreDeploymentRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            entered.TrySetResult();
            return response.Task;
        });
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(_logger);
        var diagnostic = DeploymentFingerprintCheck.Run(_store, 1, factory, CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _generator.BeforeGenerate = (_, _, _) => throw new InvalidOperationException("manual schema failure");
        await Catch.Exception(_store.EventTypes.Discover);
        var before = _store.Registration;
        response.SetResult(new() { Data = new() { Generation = 1, Fingerprint = "deployed" } });
        await diagnostic.WaitAsync(TimeSpan.FromSeconds(10));
        _diagnosticFinished = diagnostic.IsCompletedSuccessfully;
        _readinessUnchanged = ReferenceEquals(before, _store.Registration);
        _gateStayedSuccessful = _store.LatestPreparationVerdict.IsCompletedSuccessfully;
        _generator.BeforeGenerate = null;
        await _store.EventTypes.Discover();
        _artifacts.EventTypes.Returns(_ =>
        {
            _rediscover = true;
            return [];
        });
        _registrationFailure = await Catch.Exception(_store.RegisterAll);
    }

    [Fact] void should_swallow_the_diagnostic_failure() => _diagnosticFinished.ShouldBeTrue();
    [Fact] void should_log_the_schema_failure() => _logger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log").ShouldBeTrue();
    [Fact] void should_send_the_ordinary_event_payload() => _services.EventTypes.Received(1).RegisterEventTypes(Arg.Any<Contracts.EventTypes.RegisterEventTypesRequest>());
    [Fact] void should_not_publish_diagnostic_failure() => _readinessUnchanged.ShouldBeTrue();
    [Fact] void should_not_cache_a_failed_gate_revision() => _gateStayedSuccessful.ShouldBeTrue();
    [Fact] void should_allow_ordinary_registration_after_repair() => _registrationFailure.ShouldBeNull();
    [Fact] void should_not_rediscover_the_gate() => _rediscover.ShouldBeFalse();

    [Events.EventType("bf6ea963-b6d7-4ea8-9a0c-b2b003a194cc")]
    public record ManualEvent(string Name);
}
