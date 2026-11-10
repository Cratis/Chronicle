// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_checking_for_a_stalled_commit;

public class and_it_is_making_progress : given.an_import
{
    bool _failed;

    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
        await _grain.CommitStarted(EventSequenceNumber.First);
        _time.Now += TimeSpan.FromHours(1);
    }

    async Task Because() => _failed = await _grain.FailIfStalled();

    [Fact] void should_leave_it() => _failed.ShouldBeFalse();
    [Fact] void should_still_be_committing() => Record.State.ShouldEqual(ImportState.Committing);
}
