// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_delivering_header_events : given.an_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Concepts.Events.AppendedEvent[] _events;

    void Establish() => _events = header_events.All();

    void Because() => _deliver("partition", _events, _completion, null, Concepts.Observation.ObservationOrigin.Live);

    [Fact] void should_redact_every_content_variant() => header_events.ContainsSentinel(JsonSerializer.Serialize(_deliveries)).ShouldBeFalse();
    [Fact] void should_keep_events_and_headers() => _deliveries.Single().Events.All(@event => header_events.HasEmptyHeaders(@event.Content)).ShouldBeTrue();
    [Fact] void should_preserve_internal_events() => _events.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
