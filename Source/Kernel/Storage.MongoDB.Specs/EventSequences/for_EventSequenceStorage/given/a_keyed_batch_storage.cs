// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

public abstract class a_keyed_batch_storage(ReplicaSetMongoDBFixture fixture) : when_publishing.a_publication(fixture)
{
    protected EventToAppendToStorage _event;
    void Establish() => _event = PublicationEvent(0);
}
