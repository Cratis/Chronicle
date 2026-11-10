// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_invalid_events_are_skipped : given.an_import_being_committed
{
    void Establish() => _record = _record with { OnInvalid = OnInvalidEvent.Skip };

    async Task Because() => await _work.Run(CancellationToken.None);

    [Fact] void should_leave_the_settling_of_each_event_to_the_commit() => _validationFirsts.ShouldBeEmpty();
    [Fact] void should_commit_every_chunk() => _committedChunks.ShouldEqual([0, 1]);
}
