// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Events;

[EventType]
public record WeightsSetForSimulationConfiguration(
    double Distance,
    double Time,
    double Cost,
    double Waste);
