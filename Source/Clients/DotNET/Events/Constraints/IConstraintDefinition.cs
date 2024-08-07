// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using Cratis.Chronicle.Contracts.Events.Constraints;

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

    /// <summary>
    /// Converts the definition to a <see cref="Contract"/> representation.
    /// </summary>
    /// <returns>A new <see cref="Contract"/> representation.</returns>
    Constraint ToContract();
}
