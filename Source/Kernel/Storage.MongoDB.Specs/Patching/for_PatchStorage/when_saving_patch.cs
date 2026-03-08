// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Concepts.System;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Patching.for_PatchStorage;

public class when_saving_patch : given.a_patch_storage
{
    Patch _patch;

    void Establish() => _patch = new Patch("TestPatch", new SemanticVersion(1, 0, 0), DateTimeOffset.UtcNow);

    async Task Because() => await _storage.Save(_patch);

    [Fact] void should_replace_document_in_collection() => _collection.Received(1).ReplaceOneAsync(
        Arg.Any<FilterDefinition<Patch>>(),
        _patch,
        Arg.Any<ReplaceOptions>(),
        Arg.Any<CancellationToken>());
}
