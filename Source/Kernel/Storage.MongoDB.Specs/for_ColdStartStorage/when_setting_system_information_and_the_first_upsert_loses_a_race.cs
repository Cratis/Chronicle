// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage;

public class when_setting_system_information_and_the_first_upsert_loses_a_race : a_system_information_collection
{
    Exception _result;

    void Establish() => UpsertsReturn(
        Task.FromException<ReplaceOneResult>(duplicate_keys.Duplicate()),
        Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));

    async Task Because() => _result = await Catch.Exception(() => _storage.SetSystemInformation(new Chronicle.Storage.SystemInformation("1.0.0")));

    [Fact] void should_not_fail() => _result.ShouldBeNull();
    [Fact] void should_retry_the_upsert_once() => _collection.ReceivedWithAnyArgs(2).ReplaceOneAsync(default(FilterDefinition<MongoDBSystemInformation>)!, default(MongoDBSystemInformation)!, default(ReplaceOptions)!, default);
}
