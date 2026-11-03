// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

public class UniqueAssignedCode : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder.Unique(constraint => constraint
        .On<CodeAssigned>(@event => @event.Code)
        .WithMode(UniqueConstraintMode.PerValue));
}
