// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join_for_children;

public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<UserCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(_ => _.Groups, _ => _
            .IdentifiedBy(e => e.GroupId)
            .From<UserAddedToGroup>(b => b
                .UsingParentKey(e => e.UserId))
            .Join<GroupCreated>(j => j
                .Set(m => m.GroupName).To(e => e.Name)));
}
