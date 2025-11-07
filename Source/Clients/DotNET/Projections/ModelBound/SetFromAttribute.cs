// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property value should be set from an event property.
/// </summary>
/// <typeparam name="TEvent">The type of event to set from.</typeparam>
/// <param name="eventPropertyName">Optional name of the property on the event. If not specified, uses the model property name.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SetFromAttribute<TEvent>(string? eventPropertyName = default) : Attribute, IProjectionAnnotation, ISetFromAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <summary>
    /// Gets the name of the property on the event.
    /// </summary>
    public string? EventPropertyName { get; } = eventPropertyName;
}
