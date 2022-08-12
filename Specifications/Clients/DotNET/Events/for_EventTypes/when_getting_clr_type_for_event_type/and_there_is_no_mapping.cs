// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.when_getting_clr_type_for_event_type;

public class and_there_is_no_mapping : given.no_event_types
{
    Exception result;

    void Because() => result = Catch.Exception(() => event_types.GetClrTypeFor(MyEvent.EventTypeIdentifier));

    [Fact] void should_throw_missing_clr_type_for_event_type() => result.ShouldBeOfExactType<MissingClrTypeForEventType>();
}
