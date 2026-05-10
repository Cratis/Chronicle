// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_with_scoped_constraint;

public class ScopedUniqueUserConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .PerEventSourceType()
        .Unique(b => b.On<ScopedUserRegistered>(e => e.UserName));
}
