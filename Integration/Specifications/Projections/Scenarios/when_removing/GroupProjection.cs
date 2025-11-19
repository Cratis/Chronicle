// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_removing;

public class GroupProjection : IProjectionFor<Group>
{
    public void Define(IProjectionBuilderFor<Group> builder) => builder
        .AutoMap()
        .From<GroupCreated>()
        .Children(_ => _.Users, _ => _
            .IdentifiedBy(e => e.UserId)
            .From<UserAddedToGroup>(b => b
                .UsingKey(e => e.UserId))
            .RemovedWith<UserRemovedFromGroup>(b => b
                .UsingKey(e => e.UserId)));
}
