// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemStorage.when_setting_patching_kernel_version;

public class and_system_information_exists : given.a_system_storage
{
    readonly SemanticVersion _version = new(19, 100, 0);
    BsonDocument _filter;

    async Task Establish()
    {
        await _storage.SetSystemInformation(new SystemInformation(new SemanticVersion(19, 90, 0)));
        _collection.ClearReceivedCalls();
        _ = _collection.ReplaceOneAsync(
            Arg.Do<FilterDefinition<MongoDBSystemInformation>>(filter => _filter = Render(filter)),
            Arg.Any<MongoDBSystemInformation>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
    }

    async Task Because() => await _storage.SetPatchingKernelVersion(_version);

    [Fact] void should_only_target_the_separate_record() => _filter["_id"].AsInt32.ShouldEqual(1);
    [Fact] void should_upsert_the_kernel_version_without_overwriting_system_information() => _collection.Received(1).ReplaceOneAsync(
        Arg.Any<FilterDefinition<MongoDBSystemInformation>>(),
        Arg.Is<MongoDBSystemInformation>(document => document.Id == 1 && document.Version == _version),
        Arg.Is<ReplaceOptions>(options => options.IsUpsert),
        Arg.Any<CancellationToken>());

    static BsonDocument Render(FilterDefinition<MongoDBSystemInformation> filter) =>
        filter.Render(new RenderArgs<MongoDBSystemInformation>(BsonSerializer.LookupSerializer<MongoDBSystemInformation>(), BsonSerializer.SerializerRegistry));
}
