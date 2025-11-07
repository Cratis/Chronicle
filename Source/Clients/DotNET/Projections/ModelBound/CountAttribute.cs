// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property value should count occurrences of an event.
/// </summary>
/// <typeparam name="TEvent">The type of event to count.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class CountAttribute<TEvent> : Attribute, IProjectionAnnotation, ICountAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);
}
