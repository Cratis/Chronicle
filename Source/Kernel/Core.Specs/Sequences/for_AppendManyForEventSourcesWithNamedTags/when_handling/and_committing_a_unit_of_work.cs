// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

using ChronicleClient = Client::Cratis.Chronicle;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesWithNamedTags.when_handling;

public class and_committing_a_unit_of_work : Sequences.given.an_append_endpoint
{
    ChronicleClient.Transactions.UnitOfWork _unitOfWork;
    Contracts.Sequences.IEventSequences _sequences;

    void Establish()
    {
        var eventTypes = Substitute.For<ChronicleClient.Events.IEventTypes>();
        eventTypes.HasFor(typeof(object)).Returns(true);
        eventTypes.GetEventTypeFor(typeof(object)).Returns(new ChronicleClient.Events.EventType("event", 1));
        var serializer = Substitute.For<ChronicleClient.Events.IEventSerializer>();
        serializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        var causationManager = Substitute.For<ChronicleClient.Auditing.ICausationManager>();
        causationManager.GetCurrentChain().Returns([]);
        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(new CorrelationId(Guid.NewGuid()));
        var identityProvider = Substitute.For<ChronicleClient.Identities.IIdentityProvider>();
        identityProvider.GetCurrent().Returns(ChronicleClient.Identities.Identity.Unknown);
        var eventSources = Substitute.For<ChronicleClient.EventSources.IEventSources>();
        eventSources.GetFor(typeof(registered_source)).Returns(new ChronicleClient.EventSources.EventSourceDefinition(
            typeof(registered_source), "Account", "", ChronicleClient.EventSources.ConcurrencyDimensions.None, []));
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var services = Substitute.For<IServices>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        _sequences = Substitute.For<Contracts.Sequences.IEventSequences>();
        services.Sequences.Returns(_sequences);
        _sequences.AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), Arg.Any<CallContext>())
            .Returns(async call =>
            {
                var request = call.Arg<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>();
                await new AppendManyForEventSourcesWithNamedTags(
                    request.EventStore,
                    request.Namespace,
                    request.EventSequenceId,
                    request.Events.Select(@event => @event.ToApi()))
                    .Handle(_grainFactory, _causation, _principal);
                return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new()
                {
                    SequenceNumbers = [0, 1],
                    IsSuccess = true
                });
            });
        var eventSequence = new ChronicleClient.EventSequences.EventSequence(
            "store",
            "namespace",
            "event-log",
            connection,
            eventTypes,
            Substitute.For<ChronicleClient.Events.Constraints.IConstraints>(),
            serializer,
            correlationIdAccessor,
            Substitute.For<ChronicleClient.EventSequences.Concurrency.IConcurrencyScopeStrategies>(),
            causationManager,
            Substitute.For<ChronicleClient.Transactions.IUnitOfWorkManager>(),
            identityProvider,
            JsonSerializerOptions.Default,
            eventSources: eventSources);
        var eventStore = Substitute.For<ChronicleClient.IEventStore>();
        eventStore.GetEventSequence(ChronicleClient.EventSequences.EventSequenceId.Log).Returns(eventSequence);
        _unitOfWork = new(correlationIdAccessor.Current, _ => { }, eventStore);
        _unitOfWork.AddEvents(
            ChronicleClient.EventSequences.EventSequenceId.Log,
            [
                new ChronicleClient.EventSequences.EventForEventSourceId("source-one", new object())
                {
                    EventSource = typeof(registered_source),
                    NamedTags = [new ChronicleClient.NamedTag("account", "one")]
                },
                new ChronicleClient.EventSequences.EventForEventSourceId("source-two", new object())
                {
                    EventSource = typeof(registered_source)
                }
            ],
            [
                new(new ChronicleClient.Events.EventSourceId("source-one"), ChronicleClient.EventSequences.Concurrency.ConcurrencyScope.None),
                new(new ChronicleClient.Events.EventSourceId("source-two"), ChronicleClient.EventSequences.Concurrency.ConcurrencyScope.None)
            ]);
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] async Task should_commit_through_the_named_tags_endpoint() => await _sequences.Received(1).AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), Arg.Any<CallContext>());
    [Fact] void should_preserve_tagged_events_registered_source() => _appendedEvents[0].EventSource.ShouldEqual(new EventSourceName("Account"));
    [Fact] void should_preserve_untagged_events_registered_source() => _appendedEvents[1].EventSource.ShouldEqual(new EventSourceName("Account"));
    [Fact] void should_preserve_first_events_named_tag() => _appendedEvents[0].NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_leave_second_events_named_tags_empty() => _appendedEvents[1].NamedTags.ShouldBeEmpty();

    record registered_source;
}
