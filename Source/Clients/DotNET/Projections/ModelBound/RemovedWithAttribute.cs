// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate what event removes a read model or a child from a collection.
/// When used on a class, it indicates the event that removes the read model instance.
/// When used on a property or parameter, it indicates the event that removes a child from a collection.
/// </summary>
/// <typeparam name="TEvent">The type of event that removes the child.</typeparam>
/// <param name="key">Optional property name on the event that identifies the instance to remove. Defaults to WellKnownExpressions.EventSourceId.</param>
/// <param name="parentKey">Optional property name that identifies the parent (only used for children). Defaults to WellKnownExpressions.EventSourceId.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class RemovedWithAttribute<TEvent>(string? key = default, string? parentKey = default) : Attribute, IProjectionAnnotation, IRemovedWithAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <summary>
    /// Gets the property name on the event that identifies the instance to remove.
    /// </summary>
    public string Key { get; } = key ?? WellKnownExpressions.EventSourceId;

    /// <summary>
    /// Gets the property name that identifies the parent (only used for children).
    /// </summary>
    public string ParentKey { get; } = parentKey ?? WellKnownExpressions.EventSourceId;
}

