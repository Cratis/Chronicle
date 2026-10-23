// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverEventSequenceResolver;

public class when_the_sequence_is_omitted : given.observer_storage
{
    async Task Because() => _result = await ObserverEventSequenceResolver.Resolve(_storage, "some-event-store", "some-observer", string.Empty);

    [Fact] void should_use_the_stored_sequence() => _result.Value.ShouldEqual("inbox-source");
}
