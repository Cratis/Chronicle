// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

[Collection(nameof(Cratis.Chronicle.Testing.for_EventStoreForTesting.given.schema_sharing_collection))]
public class when_different_catalogs_run_concurrently : Specification
{
    EventStoreForTesting _first;
    EventStoreForTesting _second;
    EventSequenceNumber _firstTail;
    EventSequenceNumber _secondTail;
    readonly DeliveryProbe _firstDeliveries = new();
    readonly DeliveryProbe _secondDeliveries = new();

    async Task Because()
    {
        await Task.WhenAll(
            Task.Run(async () =>
            {
                _first = new(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(FirstRecorded)], ModelBoundProjections = [typeof(FirstModel)] });
                await _first.RegisterAll();
                await _first.EventLog.Append(new EventSourceId("same-source"), new FirstRecorded("first"));
            }),
            Task.Run(async () =>
            {
                _second = new(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(SecondRecorded)], ModelBoundProjections = [typeof(SecondModel)] });
                await _second.RegisterAll();
                await _second.EventLog.Append(new EventSourceId("same-source"), new SecondRecorded("second"));
                await _second.EventLog.Append(new EventSourceId("same-source"), new SecondRecorded("third"));
            }));
        await Task.WhenAll(DeliverFirst(), DeliverSecond());
        _firstTail = await _first.EventLog.GetTailSequenceNumber();
        _secondTail = await _second.EventLog.GetTailSequenceNumber();
    }

    [Fact] void should_keep_sequence_numbers_independent() => (_secondTail.Value - _firstTail.Value).ShouldEqual(1UL);
    [Fact] void should_keep_event_registrations_independent() => _first.EventTypes.HasFor(typeof(SecondRecorded)).ShouldBeFalse();
    [Fact] void should_keep_the_other_event_registrations_independent() => _second.EventTypes.HasFor(typeof(FirstRecorded)).ShouldBeFalse();
    [Fact] void should_register_the_first_model() => _first.Projections.HasFor<FirstModel>().ShouldBeTrue();
    [Fact] void should_register_the_second_model() => _second.Projections.HasFor<SecondModel>().ShouldBeTrue();
    [Fact] void should_not_register_the_other_model() => _first.Projections.HasFor<SecondModel>().ShouldBeFalse();
    [Fact] void should_not_register_the_first_model_in_the_second_store() => _second.Projections.HasFor<FirstModel>().ShouldBeFalse();
    [Fact] void should_keep_observer_instances_independent() => _first.Observers.ShouldNotBeSame(_second.Observers);

    [Fact] void should_deliver_only_the_first_observers_input() => _firstDeliveries.Values.ShouldEqual(new[] { "first:first-model" });
    [Fact] void should_deliver_only_the_second_observers_input() => _secondDeliveries.Values.ShouldEqual(new[] { "second:second-model", "third:second-model" });
    [Fact] void should_allocate_the_first_delivery_locally() => _firstDeliveries.Sequences.ShouldEqual(new[] { 0UL });
    [Fact] void should_allocate_the_second_deliveries_locally() => _secondDeliveries.Sequences.ShouldEqual(new[] { 0UL, 1UL });

    async Task DeliverFirst()
    {
        using var scenario = new ReactorScenario<FirstObserver>(new Defaults(_first.ClientArtifactsProvider), eventStore: _first);
        scenario.Services.AddSingleton(_firstDeliveries);
        scenario.Given.ForEventSourceId("same-source").ReadModel(new FirstModel("same-source", "first-model"));
        await scenario.Given.ForEventSource("same-source").Events(new FirstRecorded("first"));
    }

    async Task DeliverSecond()
    {
        using var scenario = new ReactorScenario<SecondObserver>(new Defaults(_second.ClientArtifactsProvider), eventStore: _second);
        scenario.Services.AddSingleton(_secondDeliveries);
        scenario.Given.ForEventSourceId("same-source").ReadModel(new SecondModel("same-source", "second-model"));
        await scenario.Given.ForEventSource("same-source").Events(new SecondRecorded("second"), new SecondRecorded("third"));
    }

    void Destroy()
    {
        _first?.Dispose();
        _second?.Dispose();
    }

    [EventType("catalog-concurrent-first-recorded")] public record FirstRecorded(string Value);
    [EventType("catalog-concurrent-second-recorded")] public record SecondRecorded(string Value);
    [FromEvent<FirstRecorded>] public record FirstModel([Key] string Id, string Value);
    [FromEvent<SecondRecorded>] public record SecondModel([Key] string Id, string Value);

    public class DeliveryProbe
    {
        public List<string> Values { get; } = [];
        public List<ulong> Sequences { get; } = [];
    }

    public class FirstObserver(DeliveryProbe probe) : IReactor
    {
        public void Handle(FirstRecorded @event, FirstModel model, EventContext context)
        {
            probe.Values.Add($"{@event.Value}:{model.Value}");
            probe.Sequences.Add(context.SequenceNumber.Value);
        }
    }

    public class SecondObserver(DeliveryProbe probe) : IReactor
    {
        public void Handle(SecondRecorded @event, SecondModel model, EventContext context)
        {
            probe.Values.Add($"{@event.Value}:{model.Value}");
            probe.Sequences.Add(context.SequenceNumber.Value);
        }
    }
}
