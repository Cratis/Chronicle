// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.ProjectionTypes;

/// <summary>
/// Collects things from every event source into one fixed-key document's membership set.
/// </summary>
/// <remarks>
/// The root carries no event types of its own, so the document exists only because its children do.
/// </remarks>
public class ChildrenWithConstantParentKeyProjection : IProjectionFor<CountsReadModel>
{
    /// <summary>
    /// The constant every event is routed to, regardless of which event source it arrived on.
    /// </summary>
    public const string ConstantKeyValue = "children-constant-parent-key";

    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<CountsReadModel> builder) => builder
        .Children(m => m.Things, children => children
            .IdentifiedBy(child => child.Id)
            .From<CountedThingRegistered>(_ => _
                .UsingConstantParentKey(ConstantKeyValue)
                .Set(child => child.Label).To(e => e.Label)
                .Set(child => child.IsOpen).To(e => e.IsOpen))
            .From<CountedThingClosed>(_ => _
                .UsingConstantParentKey(ConstantKeyValue)
                .Set(child => child.IsOpen).ToValue(false))
            .RemovedWith<CountedThingRemoved>(_ => _
                .UsingConstantParentKey(ConstantKeyValue)));
}
