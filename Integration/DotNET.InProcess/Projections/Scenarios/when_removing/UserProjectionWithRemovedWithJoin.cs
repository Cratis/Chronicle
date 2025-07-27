// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ReadModels;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_removing;

public class UserProjectionWithRemovedWithJoin : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .AutoMap()
        .From<UserCreated>()
        .Children(_ => _.Groups, _ => _
            .IdentifiedBy(e => e.GroupId)
            .From<UserAddedToGroup>(b => b
                .UsingParentKey(e => e.UserId))
            .RemovedWithJoin<GroupRemoved>());
}
