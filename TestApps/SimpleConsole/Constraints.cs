// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace TestApp;

/// <summary>
/// Prevents an employee from being hired more than once per event source.
/// </summary>
public class UniqueEmployeeHire : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<EmployeeHired>("An employee can only be hired once.");
}

/// <summary>
/// Ensures no two employees share the same email address.
/// </summary>
public class UniqueEmployeeEmail : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique => unique
            .On<EmployeeEmailSet>(e => e.Email)
            .IgnoreCasing()
            .WithMessage("That email address is already in use by another employee."));
}
