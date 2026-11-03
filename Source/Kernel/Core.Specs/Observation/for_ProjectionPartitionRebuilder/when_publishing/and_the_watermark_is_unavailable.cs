// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder.when_publishing;

public class and_the_watermark_is_unavailable : given.a_rebuilder
{
    bool _result;

    async Task Establish() => await _rebuilder.Begin(_observerKey, _partition);

    async Task Because() => _result = await _rebuilder.Publish(EventSequenceNumber.Unavailable);

    [Fact] void should_refuse_publication() => _result.ShouldBeFalse();
    [Fact] void should_not_touch_the_live_sink() => _liveSink.ReceivedCalls().ShouldBeEmpty();
}
