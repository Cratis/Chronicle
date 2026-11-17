// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The member being processed together with the definition its attributes are written into.
/// </summary>
/// <param name="Member">The <see cref="ProjectedMember"/> carrying the attributes.</param>
/// <param name="Definition">The root <see cref="ProjectionDefinition"/>.</param>
/// <param name="ClassLevelFromEvents">The class-level FromEvent attributes that apply to the member.</param>
/// <param name="IsRoot">True when the member belongs to the root read model, false when it belongs to a child.</param>
/// <param name="ModelType">The read model type owning the member, if known.</param>
/// <param name="ChildrenDefinition">The children definition the member belongs to when it is not on the root.</param>
internal sealed record MemberProcessingScope(
    ProjectedMember Member,
    ProjectionDefinition Definition,
    List<Attribute> ClassLevelFromEvents,
    bool IsRoot,
    Type? ModelType,
    ChildrenDefinition? ChildrenDefinition)
{
    /// <summary>
    /// Gets the name of the target property, after the naming policy has been applied.
    /// </summary>
    internal string PropertyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the from definitions the member writes to.
    /// </summary>
    internal IDictionary<EventType, FromDefinition> From => IsRoot ? Definition.From : ChildrenDefinition?.From ?? Definition.From;

    /// <summary>
    /// Gets the join definitions the member writes to.
    /// </summary>
    internal IDictionary<EventType, JoinDefinition> Join => IsRoot ? Definition.Join : ChildrenDefinition?.Join ?? Definition.Join;

    /// <summary>
    /// Gets the removed-with definitions the member writes to.
    /// </summary>
    internal IDictionary<EventType, RemovedWithDefinition> RemovedWith => IsRoot ? Definition.RemovedWith : ChildrenDefinition?.RemovedWith ?? Definition.RemovedWith;

    /// <summary>
    /// Gets the removed-with-join definitions the member writes to.
    /// </summary>
    internal IDictionary<EventType, RemovedWithJoinDefinition> RemovedWithJoin => IsRoot ? Definition.RemovedWithJoin : ChildrenDefinition?.RemovedWithJoin ?? Definition.RemovedWithJoin;

    /// <summary>
    /// Gets the every-event definition the member writes to.
    /// </summary>
    internal FromEveryDefinition All => IsRoot ? Definition.All : ChildrenDefinition!.All;

    /// <summary>
    /// Gets a value indicating whether the member is a collection populated by a ChildrenFrom attribute.
    /// </summary>
    internal bool IsChildrenCollection => Member.Provider.GetAttributesOfGenericType<ChildrenFromAttribute<object>>().Any();
}
