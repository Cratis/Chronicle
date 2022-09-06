// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.when_getting_clr_type_for_event_type;

public class and_there_is_a_mapping : given.one_event_type
{
    Type result;

    void Because() => result = event_types.GetClrTypeFor(MyEvent.EventTypeIdentifier);

    [Fact] void should_return_correct_clr_type() => result.ShouldEqual(typeof(MyEvent));
}
