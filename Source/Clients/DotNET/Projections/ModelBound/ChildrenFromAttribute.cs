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
/// <param name="identifiedBy">Optional property name on the child model that identifies it. If not specified, will look for [Key] attribute, then an Id property by convention, finally defaulting to WellKnownExpressions.EventSourceId.</param>
/// <param name="parentKey">Optional property name that identifies the parent. Defaults to WellKnownExpressions.EventSourceId.</param>
/// <param name="autoMap">Auto mapping behavior for properties from the event to the child model. Defaults to Enabled.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ChildrenFromAttribute<TEvent>(
    string? key = default,
    string? identifiedBy = default,
    string? parentKey = default,
    AutoMap autoMap = AutoMap.Enabled) : Attribute, IProjectionAnnotation, IChildrenFromAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public string Key { get; } = key ?? WellKnownExpressions.EventSourceId;

    /// <inheritdoc/>
    public string? ParentKey { get; } = parentKey;

    /// <inheritdoc/>
    public string? IdentifiedBy { get; } = identifiedBy;

    /// <inheritdoc/>
    public AutoMap AutoMap { get; } = autoMap;
}
