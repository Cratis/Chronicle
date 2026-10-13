// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.given;

public class a_claimed_value : a_unique_constraints_storage
{
    protected EventSourceId _owner;
    protected EventSourceId _otherOwner;
    protected UniqueConstraintValue _value;
    protected EventSequenceNumber _sequenceNumber;

    async Task Establish()
    {
        _owner = EventSourceId.New();
        _otherOwner = EventSourceId.New();
        _value = "claimed-value";
        _sequenceNumber = 42UL;
        await _storage.Save(_owner, _definition, _sequenceNumber, _value);
    }
}
