// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class a_reducer_delivery_with_initial_state : given.a_receiver
{
    void Establish() => _delivery = _delivery with { Target = _target with { ObserverType = ObserverType.Reducer }, InitialState = "{\"count\":3}" };
    Task Because() => Receive();
    [Fact] void should_report_success() => _outcome.Status.ShouldEqual(DeliveryStatus.Succeeded);
    [Fact] void should_return_the_reduced_state() => _outcome.ReadModelState.ShouldEqual("{\"count\":4}");
    [Fact] void should_forward_the_initial_state() => _reducers.Received(1).Deliver((ReducerId)"handler", Arg.Is<ReduceOperationMessage>(operation => operation.InitialState == "{\"count\":3}"));
}
