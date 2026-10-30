// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Generations.Enablement.for_LegacyMaterializations;

public class when_reading_existing_state : given.materializations
{
    async Task Because() => _actual = await _reader.GetAll(new("orders"));

    [Fact] void should_keep_container_name() => _actual.Single().Container.ShouldEqual(_model.ContainerName);
    [Fact] void should_keep_observer_identity() => _actual.Single().Observer.ShouldEqual(_observer.Identifier);
    [Fact] void should_keep_position_evidence() => _actual.Single().PositionEvidence.ShouldEqual("42");
    [Fact] void should_keep_failed_partition_count() => _actual.Single().FailedPartitions.ShouldEqual(2);
    [Fact] void should_keep_namespace() => _actual.Single().Namespace.ShouldEqual(new EventStoreNamespaceName("tenant"));
    [Fact] void should_not_write_observers() => _namespace.Observers.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetAll").ShouldBeTrue();
    [Fact] void should_not_open_a_sink() => _ = _namespace.DidNotReceive().Sinks;
}
