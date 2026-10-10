// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_the_write_conflicts_and_event_was_revised : given.a_backfill
{
    void Establish()
    {
        _writeResult = _ =>
        {
            _snapshot = _snapshot with { RevisionCount = 1 };
            return _writes.Count == 2;
        };
    }

    async Task Because() => await Perform();

    [Fact] void should_retry_once() => _writes.Count.ShouldEqual(2);
    [Fact] void should_guard_the_new_revision_count() => _writes[1].RevisionCount.ShouldEqual(1);
}
