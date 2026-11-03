// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder.when_publishing;

public class and_the_partition_was_rebuilt : given.a_rebuilder
{
    ExpandoObject? _published;
    bool _result;

    async Task Establish()
    {
        _liveSink.ReplaceIsolated(Arg.Any<Key>(), Arg.Any<ExpandoObject?>(), Arg.Any<EventSequenceNumber>())
            .Returns(call =>
            {
                _published = (ExpandoObject?)call[1];
                return IsolatedReplaceResult.Replaced;
            });
        await _rebuilder.Begin(_observerKey, _partition);
        await _rebuilder.OnNext(_partition, [EventAt(1), EventAt(2)], new(null));
    }

    async Task Because() => _result = await _rebuilder.Publish(2UL);

    [Fact] void should_report_published() => _result.ShouldBeTrue();
    [Fact] void should_replace_the_partition_at_the_watermark() => _liveSink.Received(1).ReplaceIsolated(_partition, Arg.Any<ExpandoObject?>(), (EventSequenceNumber)2UL);
    [Fact] void should_publish_the_rebuilt_state() => ((IDictionary<string, object?>)_published!)["name"].ShouldEqual("version-2");
}
