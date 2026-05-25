// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for configuring appended events.
/// </summary>
/// <typeparam name="TEvent">The type of event to append.</typeparam>
public interface IAppendBuilder<TEvent>
    where TEvent : class
{
    /// <summary>
    /// Appends when a property changes.
    /// </summary>
    /// <param name="property">The property that changed.</param>
    /// <returns>The builder continuation.</returns>
    IAppendBuilder<TEvent> WhenPropertyChanges(string property);

    /// <summary>
    /// Appends when any of the specified properties change.
    /// </summary>
    /// <param name="properties">The properties to observe.</param>
    /// <returns>The builder continuation.</returns>
    IAppendBuilder<TEvent> WhenAnyOf(params string[] properties);

    /// <summary>
    /// Appends when all of the specified properties change.
    /// </summary>
    /// <param name="properties">The properties to observe.</param>
    /// <returns>The builder continuation.</returns>
    IAppendBuilder<TEvent> WhenAllOf(params string[] properties);

    /// <summary>
    /// Appends when a property transitions between two values.
    /// </summary>
    /// <param name="property">The property to observe.</param>
    /// <param name="from">The previous value.</param>
    /// <param name="to">The new value.</param>
    /// <returns>The builder continuation.</returns>
    IAppendBuilder<TEvent> WhenTransition(string property, string from, string to);

    /// <summary>
    /// Appends when an item is added.
    /// </summary>
    /// <returns>The builder continuation.</returns>
    IAppendBuilder<TEvent> WhenAdded();

    /// <summary>
    /// Appends when an item is removed.
    /// </summary>
    /// <returns>The builder continuation.</returns>
    IAppendBuilder<TEvent> WhenRemoved();

    /// <summary>
    /// Appends when an expression evaluates to true.
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <returns>The builder continuation.</returns>
    IAppendBuilder<TEvent> WhenExpression(string expression);

    /// <summary>
    /// Sets a field assignment on the appended event.
    /// </summary>
    /// <param name="targetProperty">The target event property.</param>
    /// <param name="sourceExpression">The source expression.</param>
    /// <returns>The builder continuation.</returns>
    IAppendBuilder<TEvent> Set(Expression<Func<TEvent, object>> targetProperty, string sourceExpression);
}
