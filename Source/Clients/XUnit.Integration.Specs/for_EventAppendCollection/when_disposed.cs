// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_disposed : given.an_event_append_collection
{
    void Because() => _collection.Dispose();

    [Fact] void should_no_longer_collect_events_after_disposal()
    {
        var correlationId = CorrelationId.New();
        _subject.OnNext([new AppendedEventWithResult(
            new AppendedEvent(EventContext.Empty, new object()),
            AppendResult.Success(correlationId, EventSequenceNumber.First))]);
        _collection.All.Count.ShouldEqual(0);
    }
}
