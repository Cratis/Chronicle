// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_fluently;

public class and_the_handler_fails : given.an_observed_reactor_stream
{
    ReactorResult _outcome;

    async Task Because()
    {
        await _reactors.Register("orders", reactor => reactor.On<OrderPlaced>(_ => throw new Exception("Warehouse unavailable")));
        Deliver(_orderPlaced, "{\"orderNumber\":\"42\"}");
        _outcome = await _result.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_fail_the_partition() => _outcome.State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_not_advance_the_checkpoint() => _outcome.LastSuccessfulObservation.ShouldEqual(ulong.MaxValue);
    [Fact] void should_return_the_failure_reason() => _outcome.ExceptionMessages.ShouldContain("Warehouse unavailable");
}
