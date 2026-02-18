// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemStorage;

public class when_getting_system_information_and_no_document_exists : given.a_system_storage
{
    IAsyncCursor<MongoDBSystemInformation> _cursor;
    SystemInformation? _result;

    void Establish()
    {
        _cursor = Substitute.For<IAsyncCursor<MongoDBSystemInformation>>();
        _cursor.Current.Returns([]);
        _cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        _collection.FindAsync(Arg.Any<FilterDefinition<MongoDBSystemInformation>>(), Arg.Any<FindOptions<MongoDBSystemInformation>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_cursor));
    }

    async Task Because() => _result = await _storage.GetSystemInformation();

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
