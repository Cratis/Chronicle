// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverSequenceCheckpoints.for_ObserverSequenceCheckpointsStorage;

public class when_round_tripping : given.a_checkpoint_storage
{
    ObserverSequenceCheckpoint _result;
    ObserverSequenceCheckpoint _other;

    async Task Because()
    {
        await _storage.Save(ObserverSequenceCheckpoint.Fresh("observer", "A"));
        await _storage.Save(ObserverSequenceCheckpoint.Fresh("observer", "B"));
        await _storage.Save(ObserverSequenceCheckpoint.Fresh("observer", "A") with { NextEventSequenceNumber = new(42) });
        _result = await _storage.Get("observer", "A");
        _other = await _storage.Get("observer", "B");
    }

    [Fact] void should_update_an_existing_key() => _result.NextEventSequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_keep_the_other_sequence() => _other.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
