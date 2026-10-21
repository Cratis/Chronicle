// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.EventSources.for_EventSources.when_discovering_explicit_sources;

public class and_append_dimensions_match_marker_sources : EventSequences.for_EventSequence.given.all_dependencies
{
    readonly List<Contracts.Sequences.AppendRequest> _appends = [];
    readonly List<Contracts.Sequences.AppendManyForEventSourcesRequest> _batches = [];
    EventSequence _plain;
    EventSequence _marked;
    EventSources _plainSources;
    EventSources _markedSources;
    Exception _unknownStream;
    EventSourceId _source;

    async Task Establish()
    {
        _source = "source-id";
        var plainProvider = new ExplicitArtifacts().RegisterEventSource<PlainSource>("source-alias").CreateSnapshot().ApplyTo(Substitute.For<IClientArtifactsProvider>());
        var markedProvider = Substitute.For<IClientArtifactsProvider>();
        markedProvider.EventSources.Returns([typeof(MarkedSource)]);
        _plainSources = new(null, plainProvider);
        _markedSources = new(null, markedProvider);
        await _plainSources.Discover();
        await _markedSources.Discover();
        _plain = Create(_plainSources);
        _marked = Create(_markedSources);
        _eventTypes.HasFor(typeof(PlainEvent)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(PlainEvent)).Returns(new EventType("event-alias", 2));
        _eventSerializer.Serialize(Arg.Any<PlainEvent>()).Returns(new JsonObject { ["Name"] = "content" });
        _identityProvider.GetCurrent().Returns(Identity.System);
        _concurrencyScopeStrategy.GetScope(Arg.Any<ConcurrencyDimensions>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(Task.FromResult(_defaultConcurrencyScope));
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>()).Returns(call =>
        {
            _appends.Add(call.Arg<Contracts.Sequences.AppendRequest>());
            return CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new() { IsSuccess = true });
        });
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>()).Returns(call =>
        {
            _batches.Add(call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>());
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { IsSuccess = true, SequenceNumbers = [1, 2] });
        });
    }

    async Task Because()
    {
        await _plain.Append(typeof(PlainSource), _source, new PlainEvent("content"), "stream", "stream-id", subject: "subject");
        await _marked.Append<MarkedSource>(_source, new PlainEvent("content"), "stream", "stream-id", subject: "subject");
        var preparedPlain = await _plain.Prepare(new PlainEvent("content"));
        var preparedMarked = await _marked.Prepare(new PlainEvent("content"));
        await _plain.AppendPreparedThroughEventSource(typeof(PlainSource), _source, preparedPlain, "stream", "stream-id", subject: "subject");
        await _marked.AppendPrepared<MarkedSource>(_source, preparedMarked, "stream", "stream-id", subject: "subject");
        await _plain.Append(typeof(PlainSource), _source, new PlainEvent("fallback"));
        await _marked.Append<MarkedSource>(_source, new PlainEvent("fallback"));
        await _plain.AppendMany(typeof(PlainSource), _source, [new PlainEvent("one"), new PlainEvent("two")], "stream", "stream-id");
        await _marked.AppendMany<MarkedSource>(_source, [new PlainEvent("one"), new PlainEvent("two")], "stream", "stream-id");
        _unknownStream = await Catch.Exception(() => _plain.Append(typeof(PlainSource), _source, new PlainEvent("unknown"), "missing"));
    }

    [Fact] void should_preserve_the_alias_on_direct_and_prepared_appends() => _appends.Take(4).All(_ => _.EventSource == "source-alias" && _.EventSourceType == "source-alias").ShouldBeTrue();
    [Fact] void should_preserve_stream_dimensions_on_direct_and_prepared_appends() => _appends.Take(4).All(_ => _.EventStreamType == "stream" && _.EventStreamId == "stream-id" && _.EventSourceId == "source-id").ShouldBeTrue();
    [Fact] void should_preserve_the_explicit_event_id_and_generation() => _appends.TrueForAll(_ => _.EventType.Id == "event-alias" && _.EventType.Generation == 2).ShouldBeTrue();
    [Fact] void should_preserve_the_explicit_subject() => _appends.Take(4).All(_ => _.Subject == "subject").ShouldBeTrue();
    [Fact] void should_preserve_batch_routing_dimensions() => _batches.SelectMany(_ => _.Events).All(_ => _.EventSource == "source-alias" && _.EventSourceType == "source-alias" && _.EventStreamType == "stream" && _.EventStreamId == "stream-id").ShouldBeTrue();
    [Fact] void should_append_both_batch_events() => _batches.Select(_ => _.Events.Count()).ShouldContainOnly(2, 2);
    [Fact] void should_use_stream_override_concurrency_dimensions_for_both_styles() => Dimensions().Count(_ => _ == (ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)).ShouldEqual(6);
    [Fact] void should_fall_back_to_source_concurrency_dimensions_for_both_styles() => Dimensions().Count(_ => _ == (ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventSourceType)).ShouldEqual(2);
    [Fact] void should_reject_a_stream_outside_the_declared_source() => _unknownStream.ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
    [Fact] void should_send_no_append_for_the_unknown_stream() => _appends.Count.ShouldEqual(6);

    IEnumerable<ConcurrencyDimensions> Dimensions() => _concurrencyScopeStrategy.ReceivedCalls().Where(call => call.GetArguments()[0] is ConcurrencyDimensions).Select(call => (ConcurrencyDimensions)call.GetArguments()[0]!);

    EventSequence Create(IEventSources sources) => new(
        "log",
        "current",
        "default",
        _connection,
        _eventTypes,
        _constraints,
        _eventSerializer,
        _correlationIdAccessor,
        _concurrencyScopeStrategies,
        _causationManager,
        _unitOfWorkManager,
        _identityProvider,
        JsonSerializerOptions.Default,
        eventSources: sources);

    public record PlainEvent(string Name);
    [EventSource(Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventSourceType)]
    [EventStream("stream", Concurrency = ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
    public class PlainSource;
    [EventSource("source-alias", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventSourceType)]
    [EventStream("stream", Concurrency = ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
    public class MarkedSource : IEventSource;
}
