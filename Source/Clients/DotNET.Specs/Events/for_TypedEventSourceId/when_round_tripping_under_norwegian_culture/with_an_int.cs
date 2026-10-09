// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_an_int : given.a_norwegian_culture
{
    EventSourceId<int> _result;

    void Because() => WithNorwegianCulture(() => _result = EventSourceId<int>.From(new EventSourceId<int>(12345)));

    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(12345);
}
