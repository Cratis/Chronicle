// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionConverters;

public class when_round_tripping_a_registering_version : Specification
{
    ProjectionDefinition _restored;
    ProjectionDefinition _restoredWithoutVersion;

    void Because()
    {
        var definition = new ProjectionDefinition(ProjectionOwner.Server, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>());
        _restored = (definition with { Version = "2.1.0" }).ToSql().ToKernel();
        _restoredWithoutVersion = definition.ToSql().ToKernel();
    }

    [Fact] void should_preserve_the_version() => _restored.Version.ShouldEqual("2.1.0");
    [Fact] void should_read_a_definition_stored_without_a_version_as_having_none() => _restoredWithoutVersion.Version.ShouldBeNull();
}
