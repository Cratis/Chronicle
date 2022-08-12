// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.when_getting_event_type_for_clr_type;

public class and_there_is_a_mapping : given.one_event_type
{
    EventType result;

    void Because() => result = event_types.GetEventTypeFor(typeof(MyEvent));

    [Fact] void should_return_correct_event_type() => result.Id.ToString().ShouldEqual(MyEvent.EventTypeIdentifier);
}
