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

    /// <summary>
    /// Gets a value indicating whether the property is only assigned when it has no value yet. The first
    /// <typeparamref name="TEvent"/> that carries a value wins; later events leave the property as it is.
    /// </summary>
    public bool SetOnce { get; init; }
}
