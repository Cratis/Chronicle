// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

public class UniqueEventReleasedByRemovalConstraint : IConstraint
{
    public const string Name = "UniqueUserOnboardingPerCycle";

    public void Define(IConstraintBuilder builder) => builder
        .Unique<UserOnboardingStarted>(name: Name)
        .RemovedWith<UserRemoved>();
}
