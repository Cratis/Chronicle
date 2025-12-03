// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.Concepts;

public record SimulationId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly SimulationId NotSet = new(Guid.Empty);
    public static implicit operator Guid(SimulationId value) => value.Value;
    public static implicit operator SimulationId(Guid value) => new(value);
    public static implicit operator EventSourceId(SimulationId value) => new(value.Value.ToString());
    public static SimulationId New() => new(Guid.NewGuid());
}
