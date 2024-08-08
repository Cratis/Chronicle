// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines the builder for building constraints.
/// </summary>
public interface IConstraintBuilder
{
    /// <summary>
    /// Start building a unique constraint.
    /// </summary>
    /// <param name="callback">Callback with <see cref="IUniqueConstraintBuilder"/> for building.</param>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder Unique(Action<IUniqueConstraintBuilder> callback);

    /// <summary>
    /// Adds a unique constraint for a specific event type. This means there can only be one instance of this event type per event source identifier.
    /// </summary>
    /// <typeparam name="TEventType">Type of event to add for.</typeparam>
    /// <param name="message">Optional message for the constraint.</param>
    /// <param name="name">Optional name for the constraint.</param>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder Unique<TEventType>(ConstraintViolationMessage? message = default, ConstraintName? name = default);

    /// <summary>
    /// Adds a unique constraint for a specific event type. This means there can only be one instance of this event type per event source identifier.
    /// </summary>
    /// <typeparam name="TEventType">Type of event to add for.</typeparam>
    /// <param name="messageCallback">Callback for providing message for the constraint.</param>
    /// <param name="name">Optional name for the constraint.</param>
    /// <returns>Builder for continuation.</returns>
    IConstraintBuilder Unique<TEventType>(ConstraintViolationMessageProvider messageCallback, ConstraintName? name = default);

    /// <summary>
    /// Add a constraint to the builder.
    /// </summary>
    /// <param name="constraint"><see cref="Constraint"/> to add.</param>
    void AddConstraint(IConstraintDefinition constraint);

    /// <summary>
    /// Build the constraint.
    /// </summary>
    /// <returns>A collection of <see cref="IConstraintDefinition"/> to use for registering with server.</returns>
    IImmutableList<IConstraintDefinition> Build();
}
