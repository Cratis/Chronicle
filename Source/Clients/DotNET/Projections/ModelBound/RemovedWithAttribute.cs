// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate what event removes a child from a collection.
/// </summary>
/// <typeparam name="TEvent">The type of event that removes the child.</typeparam>
/// <remarks>
/// Initializes a new instance of <see cref="RemovedWithAttribute{TEvent}"/>.
/// </remarks>
/// <param name="key">Optional property name on the event that identifies the child to remove. Defaults to WellKnownExpressions.EventSourceId.</param>
/// <param name="parentKey">Optional property name that identifies the parent. Defaults to WellKnownExpressions.EventSourceId.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class RemovedWithAttribute<TEvent>(string? key = default, string? parentKey = default) : Attribute, IProjectionAnnotation, IKeyedAttribute
{
    /// <summary>
    /// Gets the property name on the event that identifies the child to remove.
    /// </summary>
    public string Key { get; } = key ?? WellKnownExpressions.EventSourceId;

    /// <summary>
    /// Gets the property name that identifies the parent.
    /// </summary>
    public string ParentKey { get; } = parentKey ?? WellKnownExpressions.EventSourceId;
}
