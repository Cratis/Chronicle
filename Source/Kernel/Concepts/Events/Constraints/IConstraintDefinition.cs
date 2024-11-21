// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Defines the definition of a constraint.
/// </summary>
public interface IConstraintDefinition : IEquatable<IConstraintDefinition>
{
    /// <summary>
    /// Gets the name of the constraint.
    /// </summary>
    ConstraintName Name { get; }
}
