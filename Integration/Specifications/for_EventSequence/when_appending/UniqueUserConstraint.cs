// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending;

public class UniqueUserConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(b => b
            .On<UserOnboardingStarted>(e => e.UserName, e => e.Name)
            .RemovedWith<UserRemoved>());
}
