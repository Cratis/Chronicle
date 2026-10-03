// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_EventSources.when_discovering;

public class an_event_source_with_streams : given.all_dependencies
{
    EventSources _subject;

    void Establish()
    {
        _clientArtifacts.EventSources.Returns([typeof(ShoppingCartEventSource)]);
        _subject = new EventSources(_eventStore, _clientArtifacts);
    }

    async Task Because() => await _subject.Discover();

    [Fact] void should_discover_the_definition() => _subject.All.Count.ShouldEqual(1);
    [Fact] void should_use_the_name_from_the_attribute() => _subject.All[0].Name.ShouldEqual("ShoppingCart");
    [Fact] void should_use_the_name_as_event_source_type() => _subject.All[0].EventSourceType.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_carry_the_description() => _subject.All[0].Description.ShouldEqual("A cart");
    [Fact] void should_carry_the_default_concurrency() => _subject.All[0].Concurrency.ShouldEqual(ConcurrencyDimensions.EventSourceId);
    [Fact] void should_discover_both_streams() => _subject.All[0].Streams.Count.ShouldEqual(2);
    [Fact] void should_carry_the_concurrency_of_a_stream() => _subject.All[0].FindStream("Items")!.Concurrency.ShouldEqual(ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType);
    [Fact] void should_find_the_definition_by_type() => _subject.GetFor(typeof(ShoppingCartEventSource)).Name.ShouldEqual("ShoppingCart");
    [Fact] void should_find_the_definition_by_name() => _subject.GetFor("ShoppingCart").ClrType.ShouldEqual(typeof(ShoppingCartEventSource));
}
