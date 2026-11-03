// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Services.Projections.for_InferredReadModelCoverage.given;

public class a_projection_definition : Specification
{
    protected static readonly EventType EventType = new("ItemAdded", EventTypeGeneration.First);
    protected ProjectionDefinition _definition;
    protected IReadOnlyList<string> _uncovered;

    void Establish() => _definition = new(
        ProjectionOwner.Server,
        EventSequenceId.Log,
        "overview",
        ReadModelIdentifier.Inferred,
        true,
        true,
        new JsonObject(),
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());

    protected static ChildrenDefinition Child() => new(
        "ProductId",
        new Dictionary<EventType, FromDefinition>
        {
            [EventType] = new(new Dictionary<PropertyPath, string> { ["Quantity"] = "Quantity" }, "ProductId", null)
        },
        new Dictionary<EventType, JoinDefinition>
        {
            [EventType] = new("ProductId", new Dictionary<PropertyPath, string> { ["ProductName"] = "Name" }, "ProductId")
        },
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());
}
