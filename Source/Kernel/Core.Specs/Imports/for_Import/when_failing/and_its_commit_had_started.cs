// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_failing;

public class and_its_commit_had_started : given.an_import
{
    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
        await _grain.CommitStarted(EventSequenceNumber.First);
    }

    async Task Because() => await _grain.Fail("the commit job was removed");

    [Fact] void should_fail() => Record.State.ShouldEqual(ImportState.Failed);
    [Fact] void should_say_why() => Record.FailureReason.ShouldEqual("the commit job was removed");
    [Fact] async Task should_discard_the_staging_it_will_never_commit() => (await _imports.GetChunkSummaries(_key.ImportId)).ShouldBeEmpty();
}
