// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_EventSources.when_discovering;

public class an_event_source_without_a_name : given.all_dependencies
{
    EventSources _subject;

    void Establish()
    {
        _clientArtifacts.EventSources.Returns([typeof(OrderEventSource)]);
        _subject = new EventSources(_eventStore, _clientArtifacts);
    }

    async Task Because() => await _subject.Discover();

    [Fact] void should_derive_the_name_from_the_type() => _subject.All[0].Name.ShouldEqual("Order");
    [Fact] void should_have_no_streams() => _subject.All[0].Streams.Count.ShouldEqual(0);
    [Fact] void should_have_no_concurrency_dimensions() => _subject.All[0].Concurrency.ShouldEqual(ConcurrencyDimensions.None);
}
