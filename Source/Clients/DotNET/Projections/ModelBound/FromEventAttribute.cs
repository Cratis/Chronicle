// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used at class level to indicate that properties should be set from an event by convention.
/// </summary>
/// <typeparam name="TEvent">The type of event to set from.</typeparam>
/// <param name="key">Optional property name on the event that identifies the read model instance. Defaults to using the event source identifier.</param>
/// <param name="parentKey">Optional property name on the event that identifies the parent read model instance for child relationships.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class FromEventAttribute<TEvent>(string? key = default, string? parentKey = default) : Attribute, IProjectionAnnotation, IFromEventAttribute
{
    /// <summary>
    /// Gets the type of event this attribute projects from.
    /// </summary>
    public Type EventType => typeof(TEvent);

    /// <summary>
    /// Gets the property name on the event that identifies the read model instance.
    /// </summary>
    public string? Key { get; } = key;

    /// <summary>
    /// Gets the property name on the event that identifies the parent read model instance for child relationships.
    /// </summary>
    public string? ParentKey { get; } = parentKey;

    /// <summary>
    /// Gets or sets a constant value to use as the key. All events of this type will update the same read model instance.
    /// </summary>
    public string? ConstantKey { get; init; }
}
