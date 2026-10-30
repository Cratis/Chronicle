// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_redacting;

public class and_it_succeeds_it_advances_the_mutation_epoch : given.a_content_mutation_with_observers
{
    Task Because() => _eventSequence.Redact(42UL, "reason", CorrelationId.New(), [], Identity.System);

    [Fact] void should_settle_on_an_even_epoch() => _stateStorage.State.MutationEpoch.IsMutating.ShouldBeFalse();
    [Fact] void should_advance_the_epoch() => _stateStorage.State.MutationEpoch.Value.ShouldEqual(2UL);
}
