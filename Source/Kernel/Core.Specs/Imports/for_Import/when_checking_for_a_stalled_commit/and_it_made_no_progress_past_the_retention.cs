// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_checking_for_a_stalled_commit;

public class and_it_made_no_progress_past_the_retention : given.an_import
{
    bool _failed;

    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
        await _grain.CommitStarted(EventSequenceNumber.First);
        _time.Now += TimeSpan.FromHours(25);
    }

    async Task Because() => _failed = await _grain.FailIfStalled();

    [Fact] void should_fail_it() => _failed.ShouldBeTrue();
    [Fact] void should_be_failed() => Record.State.ShouldEqual(ImportState.Failed);
    [Fact] async Task should_discard_its_staging() => (await _imports.GetChunkSummaries(_key.ImportId)).ShouldBeEmpty();
}
