// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Clients;
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
        new Meter<Observer>(_meter).BeginScope(new Dictionary<string, object>()).SuccessfulObservation();
        new Meter<EventSequence>(_meter).BeginEventSequenceScope("store", "namespace").AppendedEvent("Event");
        new Meter<AppendedEventsQueue>(_meter).BeginScope(new Dictionary<string, object>()).EventsEnqueued(1);
        new Meter<ConnectedClients>(_meter).ConnectedClients(1);
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_publish_the_observation_description_as_help_metadata() => _instruments["chronicle-observer-successful-observations"].Description.ShouldEqual("Number of successful observations per observer in a given event store and namespace");
    [Fact] void should_publish_the_event_sequence_description_as_help_metadata() => _instruments["chronicle-event-sequences-appended-events"].Description.ShouldEqual("Number of events appended to the event sequence");
    [Fact] void should_publish_the_queue_description_as_help_metadata() => _instruments["chronicle-appended-events-queue-events-enqueued"].Description.ShouldEqual("Number of events enqueued");
    [Fact] void should_publish_the_clients_description_as_help_metadata() => _instruments["chronicle-connected-clients"].Description.ShouldEqual("Number of connected clients");
    [Fact] void should_not_embed_descriptions_in_units() => _instruments.Values.Select(instrument => instrument.Unit).ShouldEqual([null, null, null, null]);
}
