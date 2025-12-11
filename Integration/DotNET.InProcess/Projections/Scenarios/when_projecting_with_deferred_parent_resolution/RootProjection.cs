// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.ReadModels;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution;

public class RootProjection : IProjectionFor<Root>
{
    public void Define(IProjectionBuilderFor<Root> builder) => builder
        .AutoMap()
        .From<RootCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .From<RootUpdated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(_ => _.Children, _ => _
            .IdentifiedBy(c => c.ChildId)
            .From<ChildAddedToRoot>(b => b
                .UsingParentKey(e => e.RootId)
                .UsingKey(e => e.ChildId))
            .From<ChildNameChanged>(b => b
                .UsingParentKey(e => e.ChildId) // This gets deferred if root hasn't been created yet
                .UsingKey(e => e.ChildId)
                .Set(c => c.Name).To(e => e.Name)));
}
