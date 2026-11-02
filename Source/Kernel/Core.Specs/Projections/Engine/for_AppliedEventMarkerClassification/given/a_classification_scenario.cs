// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_AppliedEventMarkerClassification.given;

public class a_classification_scenario : Specification
{
    protected static readonly EventType JoinEvent = new("b0c8d4a2-1e3f-4a5b-8c6d-7e9f0a1b2c3d", 1);
    protected static readonly EventType OwnEvent = new("c1d9e5b3-2f4a-4b6c-9d7e-8f0a1b2c3d4e", 1);

    protected ProjectionScope Scope = ProjectionScope.Namespaced;
    protected bool SubscribesToAllEvents;
    protected bool HasChildren;
    protected bool HasNested;
    protected PropertyExpression JoinKey = new(string.Empty);
    protected bool OwnEventResolvesToEventSourceId = true;
    protected bool JoinEventIsAlsoFrom;
    protected IReadOnlyList<AppliedEventMarkerJoin> Result = [];

    protected void Classify()
    {
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var joins = new Dictionary<EventType, JoinDefinition>
        {
            [JoinEvent] = new JoinDefinition("customerId", new Dictionary<Cratis.Chronicle.Properties.PropertyPath, string>(), JoinKey)
        };
        var operations = new Dictionary<EventType, ProjectionOperationType>
        {
            [JoinEvent] = JoinEventIsAlsoFrom ? ProjectionOperationType.Join | ProjectionOperationType.From : ProjectionOperationType.Join,
            [OwnEvent] = ProjectionOperationType.From
        };
        EventTypeWithKeyResolver[] eventTypes =
        [
            new(JoinEvent, keyResolvers.FromEventSourceId, JoinEventIsAlsoFrom),
            new(OwnEvent, keyResolvers.FromEventSourceId, OwnEventResolvesToEventSourceId)
        ];
        Result = AppliedEventMarkerClassification.For(Scope, SubscribesToAllEvents, HasChildren, HasNested, joins, eventTypes, operations);
    }
}
