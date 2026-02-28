// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Concepts.System;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Patching.for_PatchStorage;

public class when_getting_all_patches : given.a_patch_storage
{
    IEnumerable<Patch> _result;
    List<Patch> _patches;

    void Establish()
    {
        _patches = new List<Patch>
        {
            new("Patch1", new SemanticVersion(1, 0, 0), DateTimeOffset.UtcNow),
            new("Patch2", new SemanticVersion(1, 1, 0), DateTimeOffset.UtcNow)
        };

        var cursor = Substitute.For<IAsyncCursor<Patch>>();
        cursor.Current.Returns(_patches);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));

        _collection.FindAsync(
            Arg.Any<FilterDefinition<Patch>>(),
            Arg.Any<FindOptions<Patch>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
    }

    async Task Because() => _result = await _storage.GetAll();

    [Fact] void should_return_all_patches() => _result.Count().ShouldEqual(2);
    [Fact] void should_query_collection() => _collection.Received(1).FindAsync(
        Arg.Any<FilterDefinition<Patch>>(),
        Arg.Any<FindOptions<Patch>>(),
        Arg.Any<CancellationToken>());
}
