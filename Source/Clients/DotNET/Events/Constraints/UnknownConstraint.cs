// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when trying to get a constraint that is unknown.
/// </summary>
/// <param name="constraintName"><see cref="ConstraintName"/> that is unknown.</param>
public class UnknownConstraint(ConstraintName constraintName) : Exception($"Unknown constraint '{constraintName}'")
{
    /// <summary>
    /// Gets the <see cref="ConstraintName"/> that is unknown.
    /// </summary>
    public ConstraintName ConstraintName => constraintName;
}
