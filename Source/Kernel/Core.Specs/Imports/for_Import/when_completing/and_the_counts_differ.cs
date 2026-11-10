// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_completing;

public class and_the_counts_differ : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
    }

    async Task Because() => _result = await _grain.Complete(2, 5);

    [Fact] void should_refuse() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_fail_the_import() => Record.State.ShouldEqual(ImportState.Failed);
    [Fact] void should_start_no_commit() => _startedJobs.ShouldBeEmpty();
    [Fact] async Task should_discard_the_staging() => (await _imports.GetChunkSummaries(_key.ImportId)).ShouldBeEmpty();
}
