// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute that indicates that a property should be set from an event's source identifier.
/// </summary>
/// <typeparam name="TEvent">The type of event to set the property from.</typeparam>
/// <remarks>
/// This only reads the event's source identifier, never <typeparamref name="TEvent"/>'s own payload - but it
/// still subscribes the projection to <typeparamref name="TEvent"/>. Once subscribed, AutoMap becomes eligible
/// for every one of <typeparamref name="TEvent"/>'s payload properties against any same-named property on the
/// model, regardless of which attribute caused the subscription. If <typeparamref name="TEvent"/> is not
/// otherwise referenced, a same-named payload property can silently overwrite an unrelated, explicitly-sourced
/// model property with no attribute anywhere near it that looks responsible. Fence an affected property with
/// <see cref="NoAutoMapAttribute"/> when this is not wanted.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SetFromEventSourceIdAttribute<TEvent> : Attribute, IProjectionAnnotation, ISetFromEventSourceIdAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public bool SetOnce { get; init; }
}
