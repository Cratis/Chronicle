// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property should be populated through a join with an event.
/// </summary>
/// <typeparam name="TEvent">The type of event to join with.</typeparam>
/// <param name="on">Optional property name on the model to join on. If not specified for root projections, must be specified.</param>
/// <param name="eventPropertyName">Optional name of the property on the event. If not specified, uses the model property name.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class JoinAttribute<TEvent>(string? on = default, string? eventPropertyName = default) : Attribute, IProjectionAnnotation, IJoinAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <summary>
    /// Gets the property name on the model to join on.
    /// </summary>
    public string? On { get; } = on;

    /// <summary>
    /// Gets the name of the property on the event.
    /// </summary>
    public string? EventPropertyName { get; } = eventPropertyName;
}
