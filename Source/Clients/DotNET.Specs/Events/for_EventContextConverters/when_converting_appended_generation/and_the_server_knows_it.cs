// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventContextConverters.when_converting_appended_generation;

public class and_the_server_knows_it : Specification
{
    EventContext _result;
    void Because() => _result = (EventContext.Empty with { EventType = new("person-registered", 2), AppendedGeneration = 1 }).ToContract().ToClient();
    [Fact] void should_keep_the_appended_generation_separate_from_delivery() => _result.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_keep_the_delivered_generation() => _result.EventType.Generation.Value.ShouldEqual(2U);
}
