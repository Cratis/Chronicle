// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Patching.for_PatchStorage;

public class when_checking_if_patch_exists : given.a_patch_storage
{
    bool _result;

    void Establish() => _collection.CountDocumentsAsync(Arg.Any<FilterDefinition<Patch>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(1L));

    async Task Because() => _result = await _storage.Has("TestPatch");

    [Fact] void should_return_true() => _result.ShouldBeTrue();
    [Fact] void should_query_collection() => _collection.Received(1).CountDocumentsAsync(
        Arg.Any<FilterDefinition<Patch>>(),
        Arg.Any<CountOptions>(),
        Arg.Any<CancellationToken>());
}
