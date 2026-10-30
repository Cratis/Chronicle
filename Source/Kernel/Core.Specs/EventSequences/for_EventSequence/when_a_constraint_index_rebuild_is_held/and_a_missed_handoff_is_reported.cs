// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_a_missed_handoff_is_reported : given.a_held_constraint_index_rebuild
{
    async Task Establish() => await HoldRebuild();

    async Task Because()
    {
        await PassLivenessCheck();
        await Tick();
    }

    [Fact]
    void should_warn_once_with_the_reason_and_identity()
    {
        var warning = _heldLogger.Entries.Single(entry => entry.Level == LogLevel.Warning).Message;
        warning.ShouldContain("Releasing constraint rebuild");
        warning.ShouldContain(_heldRunId.Value.ToString());
        warning.ShouldContain(_heldRunId.Attempt.ToString());
        warning.ShouldContain(_jobId.ToString());
        warning.ShouldContain("Failed");
        warning.ShouldContain("unique");
        warning.ShouldContain("catching the index up from the event sequence");
    }

    [Fact] void should_not_call_it_a_delivered_handoff() => _heldLogger.Entries.Exists(entry => entry.Message.Contains("Constraint index rebuild handed over")).ShouldBeFalse();
}
