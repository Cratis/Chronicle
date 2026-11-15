// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_the_released_kernel_left_no_history : given.an_event_with_revision_history
{
    async Task Establish() => await ReleasedRevise();
    async Task Because()
    {
        await Revise();
        await Read();
    }

    [Fact] void should_not_claim_to_have_the_original() => _read.OriginalContent.ShouldEqual(string.Empty);
    [Fact] void should_only_record_the_new_revision() => _read.Revisions.Count().ShouldEqual(1);
    [Fact] void should_leave_the_original_envelope_null() => _entry.OriginalContent.ShouldBeNull();
    [Fact] void should_return_the_new_content() => _read.Context.Hash.Value.ShouldEqual("hash-recorded");
}
