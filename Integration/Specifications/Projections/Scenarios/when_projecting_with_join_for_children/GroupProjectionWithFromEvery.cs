// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join_for_children;

public class GroupProjectionWithFromEvery : IProjectionFor<GroupWithLastUpdated>
{
    public void Define(IProjectionBuilderFor<GroupWithLastUpdated> builder) => builder
        .FromEvery(_ => _.Set(m => m.LastUpdated).ToEventContextProperty(e => e.Occurred))
        .From<GroupCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(_ => _.Users, _ => _
            .IdentifiedBy(e => e.UserId)
            .From<UserAddedToGroup>(b => b
                .UsingKey(e => e.UserId))
            .Join<UserCreated>(j => j
                .Set(m => m.Name).To(e => e.Name)));
}
