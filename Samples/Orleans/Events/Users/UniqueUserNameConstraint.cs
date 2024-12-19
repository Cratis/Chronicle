// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Events.Users;

public class UniqueUserNameConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(_ => _
            .On<OnboardingStarted>(_ => _.UserName)
            .On<UserNameChanged>(_ => _.UserName)
            .RemovedWith<UserRemoved>()
            .WithMessage("User name '{PropertyValue}' is already in use"));
}
