// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_completing;

public class and_staging_rejected_an_event_that_may_be_skipped : given.an_import
{
    async Task Establish()
    {
        await Begin(OnInvalidEvent.Skip);
        await StageChunk(0, 3, new ImportRejection(1, "event-type+1", ["SchemaValidation: required"]));
    }

    async Task Because() => _result = await _grain.Complete(1, 3);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_skipped_event() => Record.RejectedCount.ShouldEqual(1L);
    [Fact] void should_start_the_commit_job() => _startedJobs.Count.ShouldEqual(1);
}
