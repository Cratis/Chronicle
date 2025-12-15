// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.Concepts;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.ReadModels;

public record Simulation(
    SimulationId Id,
    string Name,
    IList<Configuration> Configurations,
    EventSequenceNumber __lastHandledEventSequenceNumber = default!);
