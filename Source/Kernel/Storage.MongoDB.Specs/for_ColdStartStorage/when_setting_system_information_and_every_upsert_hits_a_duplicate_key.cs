// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage;

public class when_setting_system_information_and_every_upsert_hits_a_duplicate_key : a_system_information_collection
{
    Exception _result;

    void Establish() => UpsertsReturn(
        Task.FromException<ReplaceOneResult>(duplicate_keys.Duplicate()),
        Task.FromException<ReplaceOneResult>(duplicate_keys.Duplicate()));

    async Task Because() => _result = await Catch.Exception(() => _storage.SetSystemInformation(new Chronicle.Storage.SystemInformation(new SemanticVersion(1, 0, 0))));

    [Fact] void should_surface_the_duplicate_key_error() => _result.ShouldBeOfExactType<MongoWriteException>();
    [Fact] void should_only_retry_once() => _collection.ReceivedWithAnyArgs(2).ReplaceOneAsync(default(FilterDefinition<MongoDBSystemInformation>)!, default(MongoDBSystemInformation)!, default(ReplaceOptions)!, default);
}
