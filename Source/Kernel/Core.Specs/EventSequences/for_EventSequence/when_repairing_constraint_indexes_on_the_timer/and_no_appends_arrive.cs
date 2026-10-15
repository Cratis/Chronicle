// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_repairing_constraint_indexes_on_the_timer;

public class and_no_appends_arrive : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber InitialNext => 1;
    Task Establish() => Seed(0);
    Task Because() => given.ConstraintRepairTimer.Tick(_silo);

    [Fact] async Task should_repair_without_an_append() => (await _claims.GetFor(_eventSourceId, _uniqueDefinition)).ShouldNotBeEmpty();
    [Fact] async Task should_persist_the_completed_watermark() => (await _log.GetState()).ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)1);
}
