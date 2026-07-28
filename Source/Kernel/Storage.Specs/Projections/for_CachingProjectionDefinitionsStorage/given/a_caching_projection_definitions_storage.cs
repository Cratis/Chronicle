// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.given;

public class a_caching_projection_definitions_storage : Specification
{
    protected CachingProjectionDefinitionsStorage _storage;
    protected IProjectionDefinitionsStorage _inner;
    protected ProjectionId _id;
    protected ProjectionDefinition _definition;

    void Establish()
    {
        _inner = Substitute.For<IProjectionDefinitionsStorage>();
        _id = "some-projection";
        _definition = DefinitionFor(_id);
        _storage = new(_inner);
    }

    protected static ProjectionDefinition DefinitionFor(ProjectionId id) =>
        new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            id,
            "some-read-model",
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
}
