// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_a_constraint_for_the_event_log_only;

public class UniqueNameInEventLog : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .ForEventLog()
        .Unique(b => b
            .On<NameClaimed>(e => e.Name)
            .On<NameClaimedInOutbox>(e => e.Name)
            .WithName(nameof(UniqueNameInEventLog)));
}
