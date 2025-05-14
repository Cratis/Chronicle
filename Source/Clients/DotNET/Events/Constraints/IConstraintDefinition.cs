// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines the definition of a constraint.
/// </summary>
public interface IConstraintDefinition
{
    /// <summary>
    /// Gets the name of the constraint.
    /// </summary>
    ConstraintName Name { get; }

    /// <summary>
    /// Gets the callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.
    /// </summary>
    ConstraintViolationMessageProvider MessageCallback { get; }
}
