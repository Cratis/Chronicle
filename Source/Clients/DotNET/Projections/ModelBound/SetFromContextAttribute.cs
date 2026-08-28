// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property value should be set from an event context property.
/// </summary>
/// <typeparam name="TEvent">The type of event to set from.</typeparam>
/// <param name="contextPropertyName">Optional name of the property on the event context. If not specified, uses the model property name.</param>
/// <remarks>
/// This only ever reads <see cref="Cratis.Chronicle.Events.EventContext"/>, never <typeparamref name="TEvent"/>'s own payload - but it
/// still subscribes the projection to <typeparamref name="TEvent"/>. Once subscribed, AutoMap becomes eligible
/// for every one of <typeparamref name="TEvent"/>'s payload properties against any same-named property on the
/// model, regardless of which attribute caused the subscription. If <typeparamref name="TEvent"/> is not
/// otherwise referenced, a same-named payload property can silently overwrite an unrelated, explicitly-sourced
/// model property with no attribute anywhere near it that looks responsible. Fence an affected property with
/// <see cref="NoAutoMapAttribute"/> when this is not wanted.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SetFromContextAttribute<TEvent>(string? contextPropertyName = default) : Attribute, IProjectionAnnotation, ISetFromContextAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public string? ContextPropertyName { get; } = contextPropertyName;
}
