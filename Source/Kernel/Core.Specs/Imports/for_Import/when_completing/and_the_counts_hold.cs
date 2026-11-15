// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_completing;

public class and_the_counts_hold : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await StageChunk(1, 2);
    }

    async Task Because() => _result = await _grain.Complete(2, 5);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_start_validating() => Record.State.ShouldEqual(ImportState.Validating);
    [Fact] void should_count_the_staged_events() => Record.EventsStaged.ShouldEqual(5L);
    [Fact] void should_start_the_commit_job() => _startedJobs.Single().ImportId.ShouldEqual(_key.ImportId);
}
