// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemStorage;

public class when_getting_system_information : given.a_system_storage
{
    MongoDBSystemInformation _mongoDocument;
    IAsyncCursor<MongoDBSystemInformation> _cursor;
    SystemInformation? _result;

    void Establish()
    {
        var version = new SemanticVersion(1, 2, 3);
        _mongoDocument = new MongoDBSystemInformation(0, version);
        _cursor = Substitute.For<IAsyncCursor<MongoDBSystemInformation>>();
        _cursor.Current.Returns([_mongoDocument]);
        _cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        _collection.FindAsync(Arg.Any<FilterDefinition<MongoDBSystemInformation>>(), Arg.Any<FindOptions<MongoDBSystemInformation>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_cursor));
    }

    async Task Because() => _result = await _storage.GetSystemInformation();

    [Fact] void should_query_collection_with_id_filter() => _collection.Received(1).FindAsync(
        Arg.Any<FilterDefinition<MongoDBSystemInformation>>(),
        Arg.Any<FindOptions<MongoDBSystemInformation>>(),
        Arg.Any<CancellationToken>());

    [Fact] void should_return_system_information_with_correct_version() => _result!.Version.ShouldEqual(new SemanticVersion(1, 2, 3));
}
