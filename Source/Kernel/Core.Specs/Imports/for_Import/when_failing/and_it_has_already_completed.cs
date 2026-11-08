// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_failing;

public class and_it_has_already_completed : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
        await _grain.CommitStarted(EventSequenceNumber.First);
        await _grain.CommitCompleted();
    }

    async Task Because() => await _grain.Fail("the commit job was removed after it succeeded");

    [Fact] void should_stay_completed() => Record.State.ShouldEqual(ImportState.Completed);
    [Fact] void should_not_record_a_failure() => Record.FailureReason.ShouldBeNull();
}
