// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating;

public class and_constraint_index_repair_is_configured : given.an_event_sequence
{
    protected override ConstraintIndexRepairOptions ConstraintIndexRepairConfiguration { get; } = new() { UpdateAttempts = 7 };

    [Fact] void should_use_the_configured_options() => _eventSequence.ConstraintIndexRepair.ShouldEqual(ConstraintIndexRepairConfiguration);
}
