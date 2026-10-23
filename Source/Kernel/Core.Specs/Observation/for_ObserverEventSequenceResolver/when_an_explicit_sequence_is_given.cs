// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.for_ObserverEventSequenceResolver;

public class when_an_explicit_sequence_is_given : given.observer_storage
{
    async Task Because() => _result = await ObserverEventSequenceResolver.Resolve(_storage, "some-event-store", "some-observer", "another-sequence");

    [Fact] void should_keep_the_explicit_sequence() => _result.Value.ShouldEqual("another-sequence");
    [Fact] void should_not_read_storage() => _storage.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>());
}
