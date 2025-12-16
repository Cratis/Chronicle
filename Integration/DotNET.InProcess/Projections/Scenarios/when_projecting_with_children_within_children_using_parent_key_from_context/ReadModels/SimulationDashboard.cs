// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Concepts;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.ReadModels;

public class SimulationDashboard
{
    public SimulationId Id { get; set; } = SimulationId.NotSet;
    public string Name { get; set; } = string.Empty;
    public IList<SimulationConfiguration> Configurations { get; set; } = [];
    public EventSequenceNumber __lastHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;
}
