// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_pii_unique_constraint;

public class UniqueEmailConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(b => b.On<UserRegistered>(e => e.Email));
}
