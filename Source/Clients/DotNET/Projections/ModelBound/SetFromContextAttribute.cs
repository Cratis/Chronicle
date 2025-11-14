// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property value should be set from an event context property.
/// </summary>
/// <typeparam name="TEvent">The type of event to set from.</typeparam>
/// <param name="contextPropertyName">Optional name of the property on the event context. If not specified, uses the model property name.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SetFromContextAttribute<TEvent>(string? contextPropertyName = default) : Attribute, IProjectionAnnotation, ISetFromContextAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public string? ContextPropertyName { get; } = contextPropertyName;
}
