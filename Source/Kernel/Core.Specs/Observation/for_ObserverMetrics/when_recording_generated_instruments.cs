// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.EventSequences;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

public class when_recording_generated_instruments : Specification
{
    readonly System.Diagnostics.Metrics.Meter _meter = new($"generated-instruments-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly Dictionary<string, Instrument> _instruments = [];

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, _) =>
        {
            if (ReferenceEquals(instrument.Meter, _meter)) _instruments.Add(instrument.Name, instrument);
        };
        _listener.Start();
    }

    void Because()
    {
        new Meter<Observer>(_meter).BeginObserverScope("observer", new("observer", "store", "namespace", "sequence"), MetricNames.Convention).SuccessfulObservation();
        new Meter<EventSequence>(_meter).BeginEventSequenceScope("store", "namespace", MetricNames.Convention).AppendedEvent("Event");
        new AppendedEventsQueueMetrics(new MetricSet(_meter, MetricNames.Convention)).EventsEnqueued(1);
        new ConnectedClientsMetrics(new Meter<ConnectedClients>(_meter), MetricNames.Convention).ConnectedClients(1);
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_publish_the_observation_description_as_help_metadata() => _instruments["cratis.chronicle.observer.successful_observations"].Description.ShouldEqual("Number of successful observations per observer in a given event store and namespace");
    [Fact] void should_publish_the_event_sequence_description_as_help_metadata() => _instruments["cratis.chronicle.event_sequence.appended_events"].Description.ShouldEqual("Number of events appended to the event sequence");
    [Fact] void should_publish_the_queue_description_as_help_metadata() => _instruments["cratis.chronicle.appended_events_queue.enqueued_events"].Description.ShouldEqual("Number of events enqueued");
    [Fact] void should_publish_the_clients_description_as_help_metadata() => _instruments["cratis.chronicle.connected_client.count"].Description.ShouldEqual("Number of connected clients");
    [Fact] void should_use_the_observation_unit() => _instruments["cratis.chronicle.observer.successful_observations"].Unit.ShouldEqual("{observation}");
    [Fact] void should_use_the_append_unit() => _instruments["cratis.chronicle.event_sequence.appended_events"].Unit.ShouldEqual("{event}");
    [Fact] void should_use_the_queue_unit() => _instruments["cratis.chronicle.appended_events_queue.enqueued_events"].Unit.ShouldEqual("{event}");
    [Fact] void should_use_the_client_unit() => _instruments["cratis.chronicle.connected_client.count"].Unit.ShouldEqual("{client}");
}
