// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionReplayabilityClassifier.given;

public class a_definition : Specification
{
    protected static readonly EventType Event = new("content-changed", 1);
    protected PartitionReplayability Result;
    protected ProjectionDefinition Definition;
    protected virtual ObserverType Type => ObserverType.Projection;

    void Establish() => Definition = Change(new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "content-projection",
        ReadModelIdentifier.NotSet,
        true,
        true,
        new(),
        new Dictionary<EventType, FromDefinition> { [Event] = new(new Dictionary<PropertyPath, string>(), new(""), null) },
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>()));

    protected virtual ProjectionDefinition Change(ProjectionDefinition definition) => definition;
}
