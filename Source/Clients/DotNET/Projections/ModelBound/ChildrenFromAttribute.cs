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
/// <param name="identifiedBy">Optional property name on the child model that identifies it. If not specified, will look for a [Key] attribute, then an Id property by convention, then a child property matching <paramref name="key"/>, finally defaulting to WellKnownExpressions.EventSourceId.</param>
/// <param name="parentKey">Optional property name that identifies the parent. Defaults to WellKnownExpressions.EventSourceId.</param>
/// <remarks>
/// This subscribes <typeparamref name="TEvent"/> only for the child collection - AutoMap eligibility for its
/// payload stays scoped to the child, and it cannot, by itself, overwrite a root property of the same name.
/// That changes the moment anything else on the same model root also references <typeparamref name="TEvent"/>
/// (for example a <c>[SetFromContext&lt;TEvent&gt;]</c> on an unrelated root property that only wants an
/// <see cref="Cratis.Chronicle.Events.EventContext"/> value): the event then also gets a root-level subscription, and every one of its
/// payload properties becomes AutoMap-eligible against any same-named root property - independent of which
/// attribute added the root subscription. Fence an affected root property with <see cref="NoAutoMapAttribute"/>
/// when this is not wanted.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ChildrenFromAttribute<TEvent>(
    string? key = default,
    string? identifiedBy = default,
    string? parentKey = default) : Attribute, IProjectionAnnotation, IChildrenFromAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public string Key { get; } = key ?? WellKnownExpressions.EventSourceId;

    /// <inheritdoc/>
    public string? ParentKey { get; } = parentKey;

    /// <inheritdoc/>
    public string? IdentifiedBy { get; } = identifiedBy;
}
