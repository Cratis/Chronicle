// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;

using KernelEventType = Cratis.Chronicle.Concepts.Events.EventType;
using KernelProjectionDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

public class when_round_tripping_a_registering_version : Specification
{
    KernelProjectionDefinition _restored;

    void Because()
    {
        var definition = new KernelProjectionDefinition(ProjectionOwner.Server, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<KernelEventType, FromDefinition>(), new Dictionary<KernelEventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<KernelEventType, RemovedWithDefinition>(), new Dictionary<KernelEventType, RemovedWithJoinDefinition>()) { Version = "2.1.0" };
        _restored = definition.ToMongoDB().ToKernel();
    }

    [Fact] void should_preserve_the_version() => _restored.Version.ShouldEqual("2.1.0");
}
