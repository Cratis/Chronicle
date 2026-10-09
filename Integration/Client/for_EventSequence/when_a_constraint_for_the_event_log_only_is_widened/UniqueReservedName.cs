// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_a_constraint_for_the_event_log_only_is_widened;

public class UniqueReservedName : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .ForEventLog()
        .Unique(b => b
            .On<NameReserved>(e => e.Name)
            .On<NameReservedInOutbox>(e => e.Name)
            .WithName(nameof(UniqueReservedName)));
}
