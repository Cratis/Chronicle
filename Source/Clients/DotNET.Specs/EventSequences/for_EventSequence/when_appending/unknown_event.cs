// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class unknown_event : given.an_event_sequence
{
    Exception result;

    void Establish() => _eventTypes.HasFor(typeof(object)).Returns(false);

    async Task Because() => result = await Catch.Exception(async () => await _eventSequence.Append(Guid.NewGuid().ToString(), new object()));

    [Fact] void should_throw_unknown_event_type() => result.ShouldBeOfExactType<UnknownEventType>();
}
