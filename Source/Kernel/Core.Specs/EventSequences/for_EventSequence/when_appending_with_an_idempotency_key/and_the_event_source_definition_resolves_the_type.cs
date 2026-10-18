// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSources;
using Cratis.Chronicle.Storage.EventSources;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_event_source_definition_resolves_the_type : given.an_event_sequence_with_idempotency_keys
{
    IEventSourcesStorage _sources;
    async Task Establish()
    {
        _sources = Substitute.For<IEventSourcesStorage>();
        _eventStoreStorage.EventSources.Returns(_sources);
        _sources.Find(Arg.Any<EventSourceName>()).Returns(new EventSourceDefinition("customers", "Customers", EventSourceOwner.Client, ConcurrencyDimensions.None, []));
        _request = _request with { EventSource = "customers" };
        (await AppendKeyed()).IsSuccess.ShouldBeTrue();
        _written.Single().EventSourceType.ShouldEqual((EventSourceType)"customers");
        Durable(fingerprint: _written.Single().Identity!.Fingerprint);
        _sources.ClearReceivedCalls();
        _publications.ClearReceivedCalls();
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_compare_the_original_received_route() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] async Task should_not_resolve_again_on_a_hit() => await _sources.DidNotReceive().Find(Arg.Any<EventSourceName>());
    [Fact] async Task should_not_write_again() => await ShouldNotWrite();
}
