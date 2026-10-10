// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_completing_a_stream_scope.given;

public class an_event_sequence_with_closed_streams : for_EventSequence.given.an_event_sequence
{
    protected ClosedStreamsConstraintStorage _closures;

    void Establish()
    {
        _closures = new();
        _namespaceStorage.GetClosedStreamsConstraints(SequenceId).Returns(_closures);
    }
}
