// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_completing;

public class and_staging_rejected_an_event : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3, new ImportRejection(1, "event-type+1", ["SchemaValidation: required"]));
    }

    async Task Because() => _result = await _grain.Complete(1, 3);

    [Fact] void should_refuse() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_fail_the_import_without_committing() => Record.State.ShouldEqual(ImportState.Failed);
    [Fact] void should_report_the_event_by_its_index() => Record.Rejections.Single().Index.ShouldEqual(1L);
    [Fact] void should_start_no_commit() => _startedJobs.ShouldBeEmpty();
}
