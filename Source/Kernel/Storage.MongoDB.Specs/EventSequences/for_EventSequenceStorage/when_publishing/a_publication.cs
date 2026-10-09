// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

public abstract class a_publication(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    void Establish() => _storage.EnsureIndexes().GetAwaiter().GetResult();

    protected EventPublication _publication = new("opaque:id/with:delimiters", "immutable-intent");

    protected EventToAppendToStorage PublicationEvent(EventSequenceNumber number) => EventAt(number) with { Subject = new Subject("separate:subject") };
}
