// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.InMemory.Projections.for_ProjectionDefinitionsStorage;

public class when_saving_a_registering_version : Specification
{
    ProjectionDefinition _restored;

    async Task Because()
    {
        var definition = new ProjectionDefinition(ProjectionOwner.Server, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>()) { Version = "2.1.0" };
        var storage = new ProjectionDefinitionsStorage();
        await storage.Save(definition);
        _restored = await storage.Get(definition.Identifier);
    }

    [Fact] void should_preserve_the_version() => _restored.Version.ShouldEqual("2.1.0");
}
