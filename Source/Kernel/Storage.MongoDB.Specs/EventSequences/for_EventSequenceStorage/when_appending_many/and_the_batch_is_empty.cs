// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_batch_is_empty(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber> _result;

    async Task Because() => _result = await _storage.AppendMany([]);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_acknowledge_events() => _result.AsT0.ShouldBeEmpty();
    [Fact] async Task should_leave_the_log_empty() =>
        (await _storage.GetTailSequenceNumber()).ShouldEqual(EventSequenceNumber.Unavailable);
}
