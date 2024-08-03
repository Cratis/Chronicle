// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system to work with <see cref="IConstraints">constraints</see>.
/// </summary>
public interface IConstraints
{
    /// <summary>
    /// Check if a constraint exists for a specific <see cref="ConstraintName"/>.
    /// </summary>
    /// <param name="constraintName"><see cref="ConstraintName"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool HasFor(ConstraintName constraintName);

    /// <summary>
    /// Get a specific constraint by its <see cref="ConstraintName"/>.
    /// </summary>
    /// <param name="constraintName"><see cref="ConstraintName"/>.</param>
    /// <returns><see cref="IConstraintDefinition"/>.</returns>
    /// <exception cref="UnknownConstraint">Thrown if the constraint is unknown.</exception>
    IConstraintDefinition GetFor(ConstraintName constraintName);

    /// <summary>
    /// Discover all constraints in the system.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all constraints with the Chronicle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();
}
