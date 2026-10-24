// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_saving;

public class and_another_owner_holds_the_identifier : given.an_observer_insert_collision
{
    void Establish() => _collection.UpdateOneAsync(
        Arg.Any<FilterDefinition<ObserverState>>(),
        Arg.Any<UpdateDefinition<ObserverState>>(),
        Arg.Is<UpdateOptions>(options => options == null || !options.IsUpsert),
        Arg.Any<CancellationToken>()).Returns(new UpdateResult.Acknowledged(0, 0, null));

    async Task Because() => _accepted = await _storage.Save(new() { Identifier = "observer" }, EventSequenceId.Log);

    [Fact] void should_refuse_the_other_owner() => _accepted.ShouldBeFalse();
    [Fact] async Task should_retry_the_same_write_without_upserting() => await _collection.Received(1).UpdateOneAsync(
        _filter, _update, Arg.Is<UpdateOptions>(options => options == null || !options.IsUpsert), Arg.Any<CancellationToken>());
}
