// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_after_parent_key_changes;

public class RootProjection : IProjectionFor<Root>
{
    public void Define(IProjectionBuilderFor<Root> builder) => builder
        .From<ParentAssigned>(b => b.UsingKey(e => e.RootId))
        .Children(m => m.Children, c => c
            .IdentifiedBy(m => m.ChildId)
            .From<ChildRecorded>(b => b
                .UsingKey(e => e.ChildId)
                .UsingParentKey(e => e.ParentId)));
}
