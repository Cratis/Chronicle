// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_Import.when_completing;

public class and_it_is_completed_again : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
    }

    async Task Because() => _result = await _grain.Complete(1, 3);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_start_the_commit_only_once() => _startedJobs.Count.ShouldEqual(1);
}
