// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.ReadModels;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context;

/// <summary>
/// Projection that tests parent key resolution from context for nested children.
/// Uses UsingParentKeyFromContext() to get the parent key from the event's event source ID
/// rather than from a property in the event itself.
/// </summary>
public class SimulationDashboardProjection : IProjectionFor<SimulationDashboard>
{
    public void Define(IProjectionBuilderFor<SimulationDashboard> builder) => builder
        .AutoMap()
        .From<SimulationAdded>()
        .Children(m => m.Configurations, m => m
            .IdentifiedBy(r => r.ConfigurationId)
            .From<SimulationConfigurationAdded>(b => b.UsingKey(e => e.ConfigurationId))
            .From<WeightsSetForSimulationConfiguration>()
            .Children(m => m.Hubs, m => m
                .IdentifiedBy(r => r.HubId)
                .From<HubAddedToSimulationConfiguration>(e => e.UsingKey(e => e.HubId))));
}
