// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_EventSources.when_registering;

public class discovered_event_sources : given.all_dependencies
{
    EventSources _subject;

    async Task Establish()
    {
        _clientArtifacts.EventSources.Returns([typeof(ShoppingCartEventSource)]);
        _subject = new EventSources(_eventStore, _clientArtifacts);
        await _subject.Discover();
    }

    async Task Because() => await _subject.Register();

    [Fact] void should_register_for_the_event_store() => _request.EventStore.ShouldEqual("test-store");
    [Fact] void should_register_the_definition() => _request.Sources.Single().Name.ShouldEqual("ShoppingCart");
    [Fact] void should_register_it_as_owned_by_the_client() => _request.Sources.Single().Owner.ShouldEqual(Contracts.EventSources.EventSourceOwner.Client);
    [Fact] void should_register_the_streams() => _request.Sources.Single().Streams.Select(_ => _.Name).ShouldContainOnly("Items", "Payment");
    [Fact] void should_register_stream_concurrency() => _request.Sources.Single().Streams.First(_ => _.Name == "Items").Concurrency.ShouldEqual(Contracts.EventSources.ConcurrencyDimensions.EventSourceId | Contracts.EventSources.ConcurrencyDimensions.EventStreamType);
}
