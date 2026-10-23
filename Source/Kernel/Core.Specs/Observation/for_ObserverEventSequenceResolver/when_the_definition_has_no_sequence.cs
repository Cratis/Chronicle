// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverEventSequenceResolver;

public class when_the_definition_has_no_sequence : given.observer_storage
{
    void Establish() => _definitions.Get("some-observer").Returns(new ObserverDefinition(
        "some-observer", [], string.Empty, ObserverType.Reactor, ObserverOwner.Client, true));

    async Task Because() => _result = await ObserverEventSequenceResolver.Resolve(_storage, "some-event-store", "some-observer", string.Empty);

    [Fact] void should_fall_back_to_the_log() => _result.ShouldEqual(EventSequenceId.Log);
}
