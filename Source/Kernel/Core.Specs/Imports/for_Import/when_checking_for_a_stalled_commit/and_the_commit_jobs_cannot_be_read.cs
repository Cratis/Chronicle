// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Monads;

namespace Cratis.Chronicle.Imports.for_Import.when_checking_for_a_stalled_commit;

/// <summary>
/// Not knowing whether the commit job is there is no evidence that it is gone, so the import is left for the next check.
/// </summary>
public class and_the_commit_jobs_cannot_be_read : given.an_import
{
    bool _failed;

    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
        await _grain.CommitStarted(EventSequenceNumber.First);
        _commitJobsLookup = (Catch<IImmutableList<JobState>, JobError>)new InvalidOperationException("The jobs could not be read.");
        _time.Now += TimeSpan.FromHours(25);
    }

    async Task Because() => _failed = await _grain.FailIfStalled();

    [Fact] void should_leave_it() => _failed.ShouldBeFalse();
    [Fact] void should_still_be_committing() => Record.State.ShouldEqual(ImportState.Committing);
}
