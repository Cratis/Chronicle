// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_SequenceHistogramBucket;

public class when_histogramming_with_named_tags : Specification
{
    IEventSequenceStorage _eventSequence;
    IStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var eventStoreNamespace = Substitute.For<IEventStoreNamespaceStorage>();
        _eventSequence = Substitute.For<IEventSequenceStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespace);
        eventStoreNamespace.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(_eventSequence);
        _eventSequence.GetHistogram(Arg.Any<HistogramResolution>(), Arg.Any<EventSequenceQueryCriteria>()).Returns([]);
    }

    async Task Because() => await SequenceHistogramBucket.SequenceHistogramWithNamedTags(
        _storage,
        "store",
        "namespace",
        "log",
        [new("account", ["one"])],
        resolution: "Day",
        eventSourceType: "user");

    [Fact] void should_retain_the_resolution_and_both_dimensions() => _eventSequence.Received(1).GetHistogram(
        HistogramResolution.Day,
        Arg.Is<EventSequenceQueryCriteria>(_ =>
            _.EventSourceType.Value == "user" &&
            _.NamedTags.Single().Name.Value == "account" &&
            _.NamedTags.Single().Values.Single() == "one"));
}
