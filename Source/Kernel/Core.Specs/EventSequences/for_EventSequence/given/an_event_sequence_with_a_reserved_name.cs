// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_a_reserved_name : an_event_sequence
{
    protected override EventSequenceId SequenceId => "observers";
    protected virtual bool SequenceExists => false;

    protected override void ConfigureSequenceName()
    {
        _reservedEventSequenceIds.IsReserved(SequenceId).Returns(true);
        _storage.GetEventStores().Returns([EventStore]);
        _namespaceStorage.GetEventSequences().Returns(SequenceExists ? [SequenceId] : []);
    }
}
