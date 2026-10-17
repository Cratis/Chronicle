// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using KernelDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;
using KernelEventType = Cratis.Chronicle.Concepts.Events.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters.given;

public class a_populated_definition : Specification
{
    protected KernelDefinition _definition;

    void Establish()
    {
        var eventType = new KernelEventType("added", 1);
        var mappings = new Dictionary<PropertyPath, string> { ["name"] = "Name" };
        var from = new FromDefinition(mappings, "Key", "Parent");
        var every = new FromEveryDefinition(mappings, true) { Key = "Category", AutoMap = AutoMap.Disabled };
        var child = new ChildrenDefinition(
            "id",
            new Dictionary<KernelEventType, FromDefinition> { [eventType] = from },
            new Dictionary<KernelEventType, JoinDefinition> { [eventType] = new("name", mappings, "Key", AutoMap.Disabled) },
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            every,
            new Dictionary<KernelEventType, RemovedWithDefinition> { [eventType] = new("Key", "Parent") },
            new Dictionary<KernelEventType, RemovedWithJoinDefinition> { [eventType] = new("Key") },
            new(eventType, "Items"),
            AutoMap.Disabled);
        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "client-projection",
            "client-model",
            true,
            true,
            new(),
            child.From,
            child.Join,
            new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = child },
            [new([eventType], from)],
            every,
            child.RemovedWith,
            child.RemovedWithJoin,
            child.FromEventProperty,
            Tags: null,
            Nested: new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = child },
            NoAutoMapProperties: null,
            Scope: ProjectionScope.Global);
    }
}
