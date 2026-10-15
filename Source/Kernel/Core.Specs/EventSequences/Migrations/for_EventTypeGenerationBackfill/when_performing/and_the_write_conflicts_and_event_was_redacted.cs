// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_the_write_conflicts_and_event_was_redacted : given.a_backfill
{
    void Establish()
    {
        _writeResult = _ =>
        {
            _snapshot = _snapshot with { EventTypeId = GlobalEventTypes.Redaction };
            return false;
        };
    }

    async Task Because() => await Perform();

    [Fact] void should_not_retry_the_redacted_event() => _writes.Count.ShouldEqual(1);
}
