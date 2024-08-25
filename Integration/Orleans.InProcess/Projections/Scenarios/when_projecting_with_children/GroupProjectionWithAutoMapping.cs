// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.Models;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_children;

public class GroupProjectionWithAutoMapping : IProjectionFor<Group>
{
    public void Define(IProjectionBuilderFor<Group> builder) => builder
        .AutoMap()
        .From<GroupCreated>()
        .Children(_ => _.Users, _ => _
            .IdentifiedBy(e => e.UserId)
            .From<UserAddedToGroup>(b => b
                .UsingKey(e => e.UserId)));
}
