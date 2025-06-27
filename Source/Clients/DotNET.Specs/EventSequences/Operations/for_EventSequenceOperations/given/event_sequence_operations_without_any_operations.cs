// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations.given;

public class event_sequence_operations_without_any_operations : Specification
{
    protected EventSequenceOperations operations;
    protected IEventSequence eventSequence;


    void Establish()
    {
        eventSequence = Substitute.For<IEventSequence>();
        operations = new(eventSequence);
    }
}
