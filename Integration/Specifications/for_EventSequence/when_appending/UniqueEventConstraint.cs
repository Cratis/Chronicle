// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending;

public class UniqueEventConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique<UserOnboardingStarted>();
}
