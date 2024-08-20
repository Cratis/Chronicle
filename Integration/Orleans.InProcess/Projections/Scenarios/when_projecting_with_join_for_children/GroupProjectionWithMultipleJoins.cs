// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join_for_children;

public class GroupProjectionWithMultipleJoins : IProjectionFor<Group>
{
    public void Define(IProjectionBuilderFor<Group> builder) => builder
        .From<GroupCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(_ => _.Users, _ => _
            .IdentifiedBy(e => e.UserId)
            .From<UserAddedToGroup>(b => b
                .UsingKey(e => e.UserId))
            .Join<UserCreated>(j => j
                .Set(m => m.Name).To(e => e.Name))
            .Join<SystemUserCreated>(j => j
                .Set(m => m.Name).To(e => e.Name)));
}
