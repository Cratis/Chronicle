// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Integration.for_ImportOperations;

public class when_applying_external_model_with_one_property_changed : given.one_property_changed_for<SomeEvent>
{
    DateTimeOffset? valid_from;
    SomeEvent event_appended_to_event_log;
    SomeEvent event_appended_to_outbox;
    DateTimeOffset? event_log_valid_from;
    protected override DateTimeOffset? valid_from_to_append_with => valid_from ??= DateTimeOffset.UtcNow.AddDays(Random.Shared.Next(7));

    void Establish()
    {
        event_log
            .Setup(_ => _.Append(IsAny<EventSourceId>(), IsAny<object>(), IsAny<DateTimeOffset>()))
            .Callback((EventSourceId _, object @event, DateTimeOffset? validFrom) =>
            {
                event_appended_to_event_log = (@event as SomeEvent)!;
                event_log_valid_from = validFrom;
            });

        event_outbox
            .Setup(_ => _.Append(IsAny<EventSourceId>(), IsAny<object>(), null))
            .Callback((EventSourceId _, object @event, DateTimeOffset? __) => event_appended_to_outbox = (@event as SomeEvent)!);
    }

    async Task Because() => await operations.Apply(incoming);

    [Fact] void should_have_one_event_in_event_log() => event_appended_to_event_log.ShouldNotBeNull();
    [Fact] void should_append_to_event_log_with_correct_valid_from() => event_log_valid_from.ShouldEqual(valid_from_to_append_with);
    [Fact] void should_not_have_one_event_in_event_outbox() => event_appended_to_outbox.ShouldBeNull();
}
