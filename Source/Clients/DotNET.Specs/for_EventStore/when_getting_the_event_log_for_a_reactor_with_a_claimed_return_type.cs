// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore;

public class when_getting_the_event_log_for_a_reactor_with_a_claimed_return_type : Specification
{
    EventStore _eventStore;
    ServiceProvider _provider;
    EventSequenceNumber _result;
    Contracts.Sequences.TailSequenceNumberRequest _request;

    async Task Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var sequences = Substitute.For<Contracts.Sequences.IEventSequences>();
        var services = Substitute.For<IServices>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        services.Sequences.Returns(sequences);
        sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), CallContext.Default)
            .Returns(call =>
            {
                _request = call.Arg<Contracts.Sequences.TailSequenceNumberRequest>();
                return QueryResult<Contracts.Sequences.EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 44 });
            });

        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(MyEvent)]);
        var handlers = Substitute.For<IReactorSideEffectHandlers>();
        handlers.CanHandleReturnType(typeof(ClaimedSideEffect)).Returns(true);

        var servicesForClient = new ServiceCollection();
        servicesForClient.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        servicesForClient.AddKeyedSingleton<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reactors.Reactors>>());
        servicesForClient.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers.Reducers>>());
        _provider = servicesForClient.BuildServiceProvider();
        _eventStore = new EventStore(
            "store",
            "default",
            connection,
            artifacts,
            Substitute.For<IEventTypeMigrators>(),
            Substitute.For<ICorrelationIdAccessor>(),
            Substitute.For<IConcurrencyScopeStrategies>(),
            Substitute.For<ICausationManager>(),
            Substitute.For<IIdentityProvider>(),
            Substitute.For<IJsonSchemaGenerator>(),
            new DefaultNamingPolicy(),
            _provider,
            handlers,
            Substitute.For<IClientArtifactsActivator>(),
            false,
            JsonSerializerOptions.Default,
            false,
            Options.Create(new ChronicleOptions()),
            NullLoggerFactory.Instance);
        await _eventStore.EventTypes.Discover();
    }

    async Task Because() => _result = await _eventStore.GetEventSequence(EventSequenceId.Log).GetTailSequenceNumberForObserver(typeof(ClaimedReactor));

    void Destroy() => _provider.Dispose();

    [Fact] void should_use_the_cached_event_log() => _eventStore.GetEventSequence(EventSequenceId.Log).ShouldEqual(_eventStore.EventLog);
    [Fact] void should_return_the_tail_number() => _result.ShouldEqual((EventSequenceNumber)44UL);
    [Fact] void should_filter_by_the_reactors_event_type() => _request.EventTypeIds.ShouldContain(typeof(MyEvent).GetEventType().Id.Value);

    class ClaimedReactor : IReactor
    {
        public ClaimedSideEffect Handle(MyEvent @event) => new();
    }

    record ClaimedSideEffect;
}
