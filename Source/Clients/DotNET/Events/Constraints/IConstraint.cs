// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a constraint for events.
/// </summary>
public interface IConstraint
{
    /// <summary>
    /// Define the constraint.
    /// </summary>
    /// <param name="builder"><see cref="IConstraintBuilder"/> to build on.</param>
    void Define(IConstraintBuilder builder);
}
