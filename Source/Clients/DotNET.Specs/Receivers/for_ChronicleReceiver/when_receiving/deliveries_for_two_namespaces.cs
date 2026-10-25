// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class deliveries_for_two_namespaces : given.a_receiver
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IReceiveReactorDeliveries _otherReactors;
    DeliveryOutcome[] _outcomes;

    void Establish()
    {
        var otherStore = Substitute.For<IEventStore>();
        var otherReactors = Substitute.For<Cratis.Chronicle.Reactors.IReactors, IReceiveReactorDeliveries>();
        _otherReactors = (IReceiveReactorDeliveries)otherReactors;
        otherStore.Reactors.Returns(otherReactors);
        _client.GetEventStore((EventStoreName)"store", (EventStoreNamespaceName)"other").Returns(otherStore);
        _decoder.Decode(Arg.Any<DeliveryRequest>(), Arg.Any<CancellationToken>()).Returns(call => _delivery with { Target = call.Arg<DeliveryRequest>().Target! });
        _reactors.Deliver(Arg.Any<ReactorId>(), Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>()).Returns<Task<ReactorResult?>>(async _ =>
        {
            _entered.TrySetResult();
            await _release.Task;
            return new ReactorResult { State = ObservationState.Success, Partition = "order-42", LastSuccessfulObservation = 12 };
        });
        _otherReactors.Deliver(Arg.Any<ReactorId>(), Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _release.TrySetResult();
            return new ReactorResult { State = ObservationState.Success, Partition = "order-42", LastSuccessfulObservation = 13 };
        });
    }

    async Task Because()
    {
        var first = _receiver.Receive(new(_body, _target), CancellationToken.None);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = _receiver.Receive(new(_body, _target with { Namespace = "other", ObserverId = "other-handler" }), CancellationToken.None);
        _outcomes = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_keep_the_first_stores_result() => _outcomes[0].LastSuccessfulObservation.Value.ShouldEqual(12UL);
    [Fact] void should_keep_the_other_stores_result() => _outcomes[1].LastSuccessfulObservation.Value.ShouldEqual(13UL);
    [Fact] void should_not_deliver_the_other_observer_to_the_first_store() => _reactors.DidNotReceive().Deliver((ReactorId)"other-handler", Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_deliver_the_first_observer_to_the_other_store() => _otherReactors.DidNotReceive().Deliver((ReactorId)"handler", Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>());
}
