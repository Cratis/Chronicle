// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverSequenceCheckpointsStorage;

public class when_round_tripping : Specification
{
    ObserverSequenceCheckpointsStorage _storage;
    ObserverSequenceCheckpoint _expected;
    ObserverSequenceCheckpoint _result;
    ObserverSequenceCheckpoint _otherSequence;
    ObserverSequenceCheckpoint _otherObserver;

    void Establish()
    {
        _storage = new();
        _expected = ObserverSequenceCheckpoint.Fresh("observer", "A") with
        {
            NextEventSequenceNumber = new(43),
            LastHandledEventSequenceNumber = new(42),
            TailEventSequenceNumber = new(80),
            FailedPartitionCount = new(2),
            IsReplaying = true,
            PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, new(Guid.NewGuid()), new(3), 42, [])],
            ObserverWideClaim = new(ClaimHolderKind.ObserverCatchUp, new(Guid.NewGuid()), new(7), 50, []),
            DeliveryFences = [new("partition", DateTimeOffset.UnixEpoch.AddMinutes(5), [])]
        };
    }

    async Task Because()
    {
        await _storage.Save(_expected with { NextEventSequenceNumber = new(12) });
        await _storage.Save(ObserverSequenceCheckpoint.Fresh("observer", "B"));
        await _storage.Save(ObserverSequenceCheckpoint.Fresh("other", "A"));
        await _storage.Save(_expected);
        _result = await _storage.Get("observer", "A");
        _otherSequence = await _storage.Get("observer", "B");
        _otherObserver = await _storage.Get("other", "A");
    }

    [Fact] void should_upsert_the_whole_checkpoint() => _result.ShouldEqual(_expected);
    [Fact] void should_keep_sequences_separate() => _otherSequence.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_keep_observers_separate() => _otherObserver.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
