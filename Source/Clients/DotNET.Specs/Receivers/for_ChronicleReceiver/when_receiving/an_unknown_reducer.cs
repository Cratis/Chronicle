// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class an_unknown_reducer : given.a_receiver
{
    void Establish()
    {
        _delivery = _delivery with { Target = _target with { ObserverType = ObserverType.Reducer } };
        _reducers.Deliver(Arg.Any<ReducerId>(), Arg.Any<ReduceOperationMessage>()).Returns<ReducerResult?>(_ => throw new ReducerDoesNotExist("missing"));
    }
    Task Because() => Receive();
    [Fact] void should_report_unknown_observer() => _outcome.Status.ShouldEqual(DeliveryStatus.UnknownObserver);
}
