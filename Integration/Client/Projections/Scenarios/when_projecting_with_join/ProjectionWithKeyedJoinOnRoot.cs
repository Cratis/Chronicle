// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join;

public class ProjectionWithKeyedJoinOnRoot : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<UserCreated>(from => from.Set(model => model.Name).To(@event => @event.Name))
        .From<UserAddedToGroup>(from => from
            .UsingKey(@event => @event.UserId)
            .Set(model => model.GroupId).ToEventSourceId())
        .Join<GroupNamed>(join => join
            .On(model => model.GroupId)
            .UsingKey(@event => @event.GroupId)
            .Set(model => model.GroupName).To(@event => @event.Name));
}
