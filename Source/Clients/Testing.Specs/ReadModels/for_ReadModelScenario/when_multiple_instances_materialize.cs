// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that when events materialize more than one instance — here a join whose join-source event is
/// seeded first — <see cref="ReadModelScenario{TReadModel}.Instance"/> fails loud rather than returning a
/// blended result, and the individual instances remain available.
/// </summary>
public class when_multiple_instances_materialize : Specification
{
    ReadModelScenario<JoinOrderSummary> _scenario;
    Guid _customerGuid;
    Exception _instanceError;

    void Establish()
    {
        _scenario = new ReadModelScenario<JoinOrderSummary>();
        _customerGuid = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(new EventSourceId(_customerGuid))
            .Events(new JoinCustomerRegistered("Ada"));

        await _scenario.Given
            .ForEventSource(new EventSourceId(Guid.NewGuid()))
            .Events(new JoinOrderPlaced(new JoinCustomerId(_customerGuid), 100m));

        _instanceError = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_reject_the_ambiguous_single_instance() => _instanceError.ShouldBeOfExactType<MultipleInstancesMaterialized>();
    [Fact] void should_expose_all_instances() => _scenario.Instances.Count.ShouldEqual(2);
}
