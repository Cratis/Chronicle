// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_abandoning;

public class and_it_is_staging : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
    }

    async Task Because() => _result = await _grain.Abandon("not needed");

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_abandoned() => Record.State.ShouldEqual(ImportState.Abandoned);
    [Fact] async Task should_discard_the_staging() => (await _imports.GetChunkSummaries(_key.ImportId)).ShouldBeEmpty();
    [Fact] async Task should_accept_no_more_chunks() => (await _grain.EnsureAcceptsChunks()).IsSuccess.ShouldBeFalse();
}
