// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_interleaved_event_sources;

public class GroupProjection : IProjectionFor<Group>
{
    public void Define(IProjectionBuilderFor<Group> builder) => builder
        .From<GroupCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(m => m.Users, c => c
            .IdentifiedBy(m => m.UserId)
            .From<UserAddedToGroup>(b => b.UsingKey(e => e.UserId))
            .Join<UserCreated>(b => b.Set(m => m.Name).To(e => e.Name))
            .RemovedWith<UserRemovedFromGroup>(b => b.UsingKey(e => e.UserId)));
}
