// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join;

public class ProjectionWithJoinOnRoot : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<UserCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .From<UserAddedToGroup>(b => b
            .UsingKey(e => e.UserId)
            .Set(m => m.GroupId).ToEventSourceId())
        .Join<GroupCreated>(j => j
            .On(g => g.GroupId)
            .Set(m => m.GroupName).To(e => e.Name));
}
