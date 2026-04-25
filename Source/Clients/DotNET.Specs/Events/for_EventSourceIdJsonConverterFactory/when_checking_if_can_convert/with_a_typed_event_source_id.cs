// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventSourceIdJsonConverterFactory.when_checking_if_can_convert;

public class with_a_typed_event_source_id : Specification
{
    EventSourceIdJsonConverterFactory _factory;
    bool _result;

    void Establish() => _factory = new();

    void Because() => _result = _factory.CanConvert(typeof(EventSourceId<Guid>));

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
