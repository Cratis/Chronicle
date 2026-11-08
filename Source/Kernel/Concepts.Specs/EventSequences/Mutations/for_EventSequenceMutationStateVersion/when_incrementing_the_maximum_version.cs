// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceMutationStateVersion;

public class when_incrementing_the_maximum_version : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new EventSequenceMutationStateVersion(long.MaxValue).Next());

    [Fact] void should_throw_state_version_exhausted() => _error.ShouldBeOfExactType<EventSequenceMutationStateVersionExhausted>();
}
