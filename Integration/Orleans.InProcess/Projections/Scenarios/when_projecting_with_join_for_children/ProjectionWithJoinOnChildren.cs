// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join_for_children;

public class ProjectionWithJoinOnChildren : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<UserCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(_ => _.Groups, _ => _
            .IdentifiedBy(e => e.GroupId)
            .From<UserAddedToGroup>(b => b
                .UsingParentKey(e => e.UserId)
                .Set(m => m.GroupId).ToEventSourceId())
            .Join<GroupCreated>(j => j
                .On(g => g.GroupId)
                .Set(m => m.GroupName).To(e => e.Name)));
}
