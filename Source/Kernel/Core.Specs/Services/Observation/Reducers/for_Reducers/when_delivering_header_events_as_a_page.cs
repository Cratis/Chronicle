// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_delivering_header_events_as_a_page : given.a_page_observation_stream
{
    readonly TaskCompletionSource<ReducerSubscriberResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Because() => _deliver(new("partition", header_events.All(), null), _completion);

    [Fact] void should_redact_every_content_variant() => header_events.ContainsSentinel(JsonSerializer.Serialize(_deliveries)).ShouldBeFalse();
    [Fact] void should_keep_events_and_headers() => _deliveries.Single().Partitions.Single().Events.All(@event => header_events.HasEmptyHeaders(@event.Content)).ShouldBeTrue();
}
