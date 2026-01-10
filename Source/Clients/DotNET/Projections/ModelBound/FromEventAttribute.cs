// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used at class level to indicate that properties should be set from an event by convention.
/// </summary>
/// <typeparam name="TEvent">The type of event to set from.</typeparam>
/// <param name="key">Optional property name on the event that identifies the read model instance. Defaults to using the event source identifier.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class FromEventAttribute<TEvent>(string? key = default) : Attribute, IProjectionAnnotation, IFromEventAttribute
{
    /// <summary>
    /// Gets the type of event this attribute projects from.
    /// </summary>
    public Type EventType => typeof(TEvent);

    /// <summary>
    /// Gets the property name on the event that identifies the read model instance.
    /// </summary>
    public string? Key { get; } = key;
}
