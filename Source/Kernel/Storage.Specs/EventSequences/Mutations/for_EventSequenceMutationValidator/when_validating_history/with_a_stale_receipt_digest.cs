// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_a_stale_receipt_digest : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateHistory(
            _scope,
            _history with { TerminalWitness = _witness with { ReceiptDigestV1 = new EventSequenceMutationReceiptDigestV1(new byte[32]) } }),
        EventSequenceMutationValidator.ValidateHistory(
            _scope,
            _history with { TerminalWitness = _witness with { DefinitionDigestV1 = new EventSequenceMutationDefinitionDigestV1(new byte[32]) } })
    ];

    [Fact] void should_reject_every_digest_that_fails_recomputation() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidDigest).ShouldBeTrue();
}
