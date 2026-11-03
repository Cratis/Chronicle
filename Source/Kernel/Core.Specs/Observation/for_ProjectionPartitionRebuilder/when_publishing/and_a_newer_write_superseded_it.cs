// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder.when_publishing;

public class and_a_newer_write_superseded_it : given.a_rebuilder
{
    bool _result;

    async Task Establish()
    {
        _liveSink.ReplaceIsolated(Arg.Any<Key>(), Arg.Any<ExpandoObject?>(), Arg.Any<EventSequenceNumber>()).Returns(IsolatedReplaceResult.Superseded);
        await _rebuilder.Begin(_observerKey, _partition);
        await _rebuilder.OnNext(_partition, [EventAt(1)], new(null));
    }

    async Task Because() => _result = await _rebuilder.Publish(1UL);

    [Fact] void should_report_not_published() => _result.ShouldBeFalse();
}
