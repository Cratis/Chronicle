// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionConverters.when_round_tripping;

public class with_pinned_delivery : Specification
{
    Observation.ObserverDefinition _result;
    void Because() => _result = new Observation.ObserverDefinition("observer", [new EventType("person-registered", 2)], EventSequenceId.Log, ObserverType.Reducer, ObserverOwner.Client, true) { GenerationDelivery = EventGenerationDelivery.Pinned }.ToSql().ToKernel();
    [Fact] void should_preserve_the_policy() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_generation() => _result.EventTypes.Single().Generation.Value.ShouldEqual(2U);
}
