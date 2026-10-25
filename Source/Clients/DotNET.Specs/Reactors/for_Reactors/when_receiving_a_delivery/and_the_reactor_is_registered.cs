// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Receivers;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_receiving_a_delivery;

public class and_the_reactor_is_registered : given.a_registered_delegate
{
    ReactorResult _deliveryResult;
    ReactorEvent _event;
    protected override Observation.EventGenerationDelivery GenerationDelivery => Observation.EventGenerationDelivery.Pinned;
    protected override IEnumerable<EventType> RegisteredEventTypes => [new("orders", 2)];

    async Task Because()
    {
        _release.TrySetResult();
        _deliveryResult = await ((IReceiveReactorDeliveries)_reactors).Deliver(
            "bridge",
            new EventsToObserve
            {
                Partition = "order-42",
                Events = [new Contracts.Events.AppendedEvent
                {
                    Context = (EventContext.Empty with { EventType = new("orders", 2), SequenceNumber = 12, AppendedGeneration = 1 }).ToContract(),
                    Content = "{\"order\":42}",
                    GenerationalContent = new Dictionary<int, string> { [2] = "{\"order\":999}" }
                }]
            },
            CancellationToken.None);
        _event = await _received.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_invoke_the_registered_delegate() => _event.Content["order"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_report_success() => _deliveryResult.State.ShouldEqual(ObservationState.Success);
    [Fact] void should_keep_the_last_observation() => _deliveryResult.LastSuccessfulObservation.ShouldEqual(12UL);
    [Fact] void should_keep_the_pinned_generation() => _event.Context.EventType.Generation.Value.ShouldEqual(2U);
    [Fact] void should_keep_the_appended_generation() => _event.Context.AppendedGeneration!.Value.ShouldEqual(1U);
}
