// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

public class UniqueReservedName : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder.Unique(constraint => constraint.On<NameReserved>(@event => @event.Name));
}
