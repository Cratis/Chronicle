// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Integration.Clustering.for_JobsRolePlacement;

public class JobsRoleProjection : IProjectionFor<JobsRoleReadModel>
{
    public ProjectionId Identifier => "jobs-role-projection";

    public void Define(IProjectionBuilderFor<JobsRoleReadModel> builder) => builder
        .From<JobsRoleEvent>(events => events
            .Set(model => model.Number)
            .To(@event => @event.Number));
}
