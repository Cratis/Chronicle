// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading;

public class and_the_released_kernel_revised_after_recorded_history : given.an_event_with_revision_history
{
    async Task Establish()
    {
        await Revise();
        await ReleasedRevise();
    }
    async Task Because() => await Read();

    [Fact] void should_keep_the_known_history() => _read.Revisions.Count().ShouldEqual(1);
    [Fact] void should_return_the_latest_released_content() => _read.Context.Hash.Value.ShouldEqual("hash-released");
    [Fact] void should_keep_the_known_original() => _read.OriginalContent.ShouldContain("original");
    [Fact] void should_preserve_the_envelope_for_diagnosis() => _entry.OriginalContent.ShouldNotBeNull();
}
