// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;

using KernelEventType = Cratis.Chronicle.Concepts.Events.EventType;
using KernelProjectionDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

public class when_round_tripping_pinned_delivery : Specification
{
    KernelProjectionDefinition _restored;
    readonly KernelEventType _pin = new("PersonRegistered", 2);

    void Because()
    {
        var definition = new KernelProjectionDefinition(ProjectionOwner.Server, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<KernelEventType, FromDefinition> { [_pin] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) }, new Dictionary<KernelEventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<KernelEventType, RemovedWithDefinition>(), new Dictionary<KernelEventType, RemovedWithJoinDefinition>()) { GenerationDelivery = EventGenerationDelivery.Pinned };
        _restored = definition.ToMongoDB().ToKernel();
    }

    [Fact] void should_preserve_the_delivery_policy() => _restored.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_resolved_generation() => _restored.From.Keys.Single().ShouldEqual(_pin);
}
