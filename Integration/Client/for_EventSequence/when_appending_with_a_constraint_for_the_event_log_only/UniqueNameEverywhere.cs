// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_a_constraint_for_the_event_log_only;

public class UniqueNameEverywhere : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(b => b
            .On<NameClaimedEverywhere>(e => e.Name)
            .On<PublicNameClaimedEverywhere>(e => e.Name)
            .WithName(nameof(UniqueNameEverywhere)));
}
