// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildEligibility.given;

public class an_eligible_candidate : Specification
{
    protected Features _features;
    protected PartitionRebuildCandidate _candidate;
    protected bool _result;

    void Establish()
    {
        _features = new() { IsolatedPartitionReplay = true };
        _candidate = new(ObserverType.Projection, ClosedDefinition(), true, true, true);
    }

    protected static ProjectionDefinition ClosedDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "content-projection",
        ReadModelIdentifier.NotSet,
        true,
        true,
        new(),
        new Dictionary<EventType, FromDefinition> { [new("content-changed", 1)] = new(new Dictionary<PropertyPath, string>(), new(""), null) },
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());
}
