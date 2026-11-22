// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Sequences.for_EventSequences;

public class when_reading_a_page_of_header_events : for_QueryExecutor.given.a_routed_query
{
    EventSequences _service;
    QueryResult<EventSequencePageResponse> _result;
    Concepts.Events.AppendedEvent[] _stored;

    void Establish()
    {
        _stored = header_events.All();
        var storage = Substitute.For<IStorage>();
        var compliance = Substitute.For<IEventCompliance>();
        var store = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        var sequence = Substitute.For<IEventSequenceStorage>();
        var cursor = Substitute.For<IEventCursor>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
        store.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(@namespace);
        @namespace.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(sequence);
        store.EventTypes.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns([]);
        sequence.GetWindow(Arg.Any<EventSequenceWindowCriteria>()).Returns(cursor);
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns(_stored);
        compliance.Release(Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<Concepts.Events.AppendedEvent>>(0).ToArray()));
        _service = new(
            Substitute.For<ICommandPipeline>(),
            storage,
            compliance,
            header_events.Options,
            Substitute.For<IQueryContextManager>(),
            Substitute.For<IGrainFactory>(),
            NamespaceCasingContext.Create(),
            NullLogger<EventSequences>.Instance);
    }

    async Task Because() => _result = await _service.GetPage(
        new() { EventStore = "application-store", Namespace = "namespace", EventSequenceId = "system", From = 12, Count = _stored.Length },
        CallFor(new Probe(), CallerNamed(Administrator)));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_redact_every_content_variant() => header_events.ContainsSentinel(JsonSerializer.Serialize(_result.Data)).ShouldBeFalse();
    [Fact] void should_keep_every_event() => _result.Data.Events.Count().ShouldEqual(_stored.Length);
    [Fact] void should_keep_empty_headers() => _result.Data.Events.All(@event => header_events.HasEmptyHeaders(@event.Content)).ShouldBeTrue();
    [Fact] void should_preserve_internal_content() => _stored.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
