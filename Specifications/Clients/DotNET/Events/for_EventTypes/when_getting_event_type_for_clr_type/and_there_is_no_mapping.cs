// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.when_getting_event_type_for_clr_type;

public class and_there_is_no_mapping : given.no_event_types
{
    Exception result;

    void Because() => result = Catch.Exception(() => event_types.GetEventTypeFor(typeof(MyEvent)));

    [Fact] void should_throw_missing_event_type_for_clr_type() => result.ShouldBeOfExactType<MissingEventTypeForClrType>();
}
