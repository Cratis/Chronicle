// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_a_long : given.a_norwegian_culture
{
    EventSourceId<long> _result;

    void Because() => WithNorwegianCulture(() => _result = EventSourceId<long>.From(new EventSourceId<long>(123456789012345L)));

    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(123456789012345L);
}
