// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations.given;

public class event_sequence_operations_without_any_operations : Specification
{
    protected EventSequenceOperations _operations;
    protected IEventSequence _eventSequence;
    protected AppendManyResult _appendManyResult;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _appendManyResult = new();
        _eventSequence
            .AppendMany(
                Arg.Any<IEnumerable<EventForEventSourceId>>(),
                concurrencyScopes: Arg.Any<Dictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(_appendManyResult);

        _operations = new(_eventSequence);
    }
}
