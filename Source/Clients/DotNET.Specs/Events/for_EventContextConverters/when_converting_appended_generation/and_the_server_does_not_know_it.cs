// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventContextConverters.when_converting_appended_generation;

public class and_the_server_does_not_know_it : Specification
{
    EventContext _result;
    void Because() => _result = EventContext.Empty.ToContract().ToClient();
    [Fact] void should_keep_the_appended_generation_unknown() => _result.AppendedGeneration.ShouldBeNull();
}
