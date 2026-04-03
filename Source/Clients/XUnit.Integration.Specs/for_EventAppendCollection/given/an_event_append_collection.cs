// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection.given;

public class an_event_append_collection : Specification
{
    protected IEventLog _eventLog;
    protected EventAppendCollection _collection;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog, IObservableEventSequence>();
        _collection = new EventAppendCollection(_eventLog);
    }

    protected void FireEvent(ulong sequenceNumber = 7) =>
        _collection.OnAppend(
            CorrelationId.New(),
            EventSourceId.New(),
            new object(),
            ImmutableList<Causation>.Empty,
            AppendResult.Success(CorrelationId.New(), new EventSequenceNumber(sequenceNumber)));
}
