// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute that indicates that a property should be set from an event's source identifier.
/// </summary>
/// <typeparam name="TEvent">The type of event to set the property from.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SetFromEventSourceIdAttribute<TEvent> : Attribute, IProjectionAnnotation, ISetFromEventSourceIdAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public bool SetOnce { get; init; }
}
