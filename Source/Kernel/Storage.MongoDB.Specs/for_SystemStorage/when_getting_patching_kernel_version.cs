// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemStorage;

public class when_getting_patching_kernel_version : given.a_system_storage
{
    SemanticVersion? _result;
    BsonDocument _filter;

    void Establish()
    {
        var cursor = Substitute.For<IAsyncCursor<MongoDBSystemInformation>>();
        cursor.Current.Returns([new MongoDBSystemInformation(1, new SemanticVersion(19, 100, 0))]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        _collection.FindAsync(
            Arg.Do<FilterDefinition<MongoDBSystemInformation>>(filter => _filter = Render(filter)),
            Arg.Any<FindOptions<MongoDBSystemInformation>>(),
            Arg.Any<CancellationToken>()).Returns(cursor);
    }

    async Task Because() => _result = await _storage.GetPatchingKernelVersion();

    [Fact] void should_read_the_separate_record() => _filter["_id"].AsInt32.ShouldEqual(1);
    [Fact] void should_return_the_kernel_version() => _result.ShouldEqual(new SemanticVersion(19, 100, 0));

    static BsonDocument Render(FilterDefinition<MongoDBSystemInformation> filter) =>
        filter.Render(new RenderArgs<MongoDBSystemInformation>(BsonSerializer.LookupSerializer<MongoDBSystemInformation>(), BsonSerializer.SerializerRegistry));
}
