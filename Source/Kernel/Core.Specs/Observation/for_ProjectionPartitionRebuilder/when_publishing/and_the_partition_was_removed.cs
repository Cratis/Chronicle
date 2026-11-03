// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder.when_publishing;

public class and_the_partition_was_removed : given.a_rebuilder
{
    bool _result;

    async Task Establish()
    {
        _eventsRemoveTheModel = true;
        _liveSink.ReplaceIsolated(Arg.Any<Key>(), Arg.Any<ExpandoObject?>(), Arg.Any<EventSequenceNumber>()).Returns(IsolatedReplaceResult.Removed);
        await _rebuilder.Begin(_observerKey, _partition);
        await _rebuilder.OnNext(_partition, [EventAt(3)], new(null));
    }

    async Task Because() => _result = await _rebuilder.Publish(3UL);

    [Fact] void should_report_published() => _result.ShouldBeTrue();
    [Fact] void should_publish_no_state() => _liveSink.Received(1).ReplaceIsolated(_partition, null, (EventSequenceNumber)3UL);
}
