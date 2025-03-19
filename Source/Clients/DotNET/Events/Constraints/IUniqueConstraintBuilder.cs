// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines the builder for building unique constraints.
/// </summary>
public interface IUniqueConstraintBuilder
{
    /// <summary>
    /// Defines the name of the unique constraint.
    /// </summary>
    /// <param name="name">Name to use.</param>
    /// <returns>Builder for continuation.</returns>
    /// <remarks>
    /// The name is optional and if not provided, it will use the type name in which the unique constraint belongs to.
    /// </remarks>
    IUniqueConstraintBuilder WithName(ConstraintName name);

    /// <summary>
    /// Constrain on a specific property on an event.
    /// </summary>
    /// <param name="properties">Expressions for specifying the properties on the event.</param>
    /// <typeparam name="TEventType">Type of event the property belongs to.</typeparam>
    /// <returns>Builder for continuation.</returns>
    IUniqueConstraintBuilder On<TEventType>(params Expression<Func<TEventType, object>>[] properties);

    /// <summary>
    /// Constrain on a specific property on an event.
    /// </summary>
    /// <param name="eventType">The <see cref="EventType"/> the property belongs to.</param>
    /// <param name="properties">Property names.</param>
    /// <returns>Builder for continuation.</returns>
    IUniqueConstraintBuilder On(EventType eventType, string[] properties);

    /// <summary>
    /// Ignore casing during comparison.
    /// </summary>
    /// <returns>Builder for continuation.</returns>
    IUniqueConstraintBuilder IgnoreCasing();

    /// <summary>
    /// Indicate an event that will remove the unique constraint.
    /// </summary>
    /// <typeparam name="TEventType">The <see cref="EventType"/> that removes the constraint.</typeparam>
    /// <returns>Builder for continuation.</returns>
    IUniqueConstraintBuilder RemovedWith<TEventType>();

    /// <summary>
    /// Indicate an event that will remove the unique constraint.
    /// </summary>
    /// <param name="eventType">The <see cref="EventType"/> that would remove the constraint.</param>
    /// <returns>The builder for continuation.</returns>
    IUniqueConstraintBuilder RemovedWith(EventType eventType);

    /// <summary>
    /// Specifies a message to use when the unique constraint is violated.
    /// </summary>
    /// <param name="message">Message to use.</param>
    /// <returns>Builder for continuation.</returns>
    IUniqueConstraintBuilder WithMessage(string message);

    /// <summary>
    /// Specifies a provider that will provide a message to use when the unique constraint is violated.
    /// </summary>
    /// <param name="messageProvider">Callback that provides the message.</param>
    /// <returns>Builder for continuation.</returns>
    IUniqueConstraintBuilder WithMessage(ConstraintViolationMessageProvider messageProvider);

    /// <summary>
    /// Builds the unique constraint.
    /// </summary>
    /// <returns>A <see cref="IConstraintDefinition"/> to use for registering with server.</returns>
    IConstraintDefinition Build();
}
