// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a property represents a collection of children from an event.
/// </summary>
/// <typeparam name="TEvent">The type of event that adds children.</typeparam>
/// <remarks>
/// Initializes a new instance of <see cref="ChildrenFromAttribute{TEvent}"/>.
/// </remarks>
/// <param name="key">Optional property name on the event that identifies the child. Defaults to WellKnownExpressions.EventSourceId.</param>
/// <param name="identifiedBy">Optional property name on the child model that identifies it. Defaults to WellKnownExpressions.Id.</param>
/// <param name="parentKey">Optional property name that identifies the parent. Defaults to WellKnownExpressions.EventSourceId.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ChildrenFromAttribute<TEvent>(
    string? key = default,
    string? identifiedBy = default,
    string? parentKey = default) : Attribute, IProjectionAnnotation
{
    /// <summary>
    /// Gets the property name on the event that identifies the child.
    /// </summary>
    public string Key { get; } = key ?? WellKnownExpressions.EventSourceId;

    /// <summary>
    /// Gets the property name on the child model that identifies it.
    /// </summary>
    public string IdentifiedBy { get; } = identifiedBy ?? WellKnownExpressions.Id;

    /// <summary>
    /// Gets the property name that identifies the parent.
    /// </summary>
    public string ParentKey { get; } = parentKey ?? WellKnownExpressions.EventSourceId;
}
