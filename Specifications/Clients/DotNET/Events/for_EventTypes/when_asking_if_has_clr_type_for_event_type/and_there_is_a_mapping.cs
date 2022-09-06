// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.when_asking_if_has_clr_type_for_event_type;

public class and_there_is_a_mapping : given.one_event_type
{
    bool result;

    void Because() => result = event_types.HasFor(MyEvent.EventTypeIdentifier);

    [Fact] void should_have_mapping() => result.ShouldBeTrue();
}
