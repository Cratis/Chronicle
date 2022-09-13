// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Integration.for_ImportOperations;

public class when_applying_external_model_with_one_property_changed_for_public_event : given.one_property_changed_for<SomePublicEvent>
{
    SomePublicEvent event_appended_to_event_log;
    SomePublicEvent event_appended_to_outbox;

    void Establish()
    {
        event_log
            .Setup(_ => _.Append(IsAny<EventSourceId>(), IsAny<object>(), null))
            .Callback((EventSourceId _, object @event, DateTimeOffset? __) => event_appended_to_event_log = (@event as SomePublicEvent)!);

        event_outbox
            .Setup(_ => _.Append(IsAny<EventSourceId>(), IsAny<object>(), null))
            .Callback((EventSourceId _, object @event) => event_appended_to_outbox = (@event as SomePublicEvent)!);
    }

    async Task Because() => await operations.Apply(incoming);

    [Fact] void should_have_one_event_in_event_log() => event_appended_to_event_log.ShouldNotBeNull();
    [Fact] void should_have_one_event_in_event_outbox() => event_appended_to_outbox.ShouldNotBeNull();
}
