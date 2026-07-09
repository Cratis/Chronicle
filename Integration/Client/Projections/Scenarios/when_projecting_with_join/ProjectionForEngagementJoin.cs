// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join;

public class ProjectionForEngagementJoin : IProjectionFor<EngagementSummary>
{
    public void Define(IProjectionBuilderFor<EngagementSummary> builder) => builder
        .From<EngagementStarted>(b => b.Set(m => m.CustomerOrgNumber).To(e => e.CustomerOrgNumber))
        .Join<CompanyRegistered>(j => j
            .On(m => m.CustomerOrgNumber)
            .Set(m => m.CustomerName).To(e => e.Name));
}
