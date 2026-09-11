// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_request;

public class with_an_invalid_target_identity : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateRequest(_request with { TargetSequence = null! }),
        EventSequenceMutationValidator.ValidateRequest(_request with { TargetSequence = IdentityWithKey("event-log", default) }),
        EventSequenceMutationValidator.ValidateRequest(_request with { TargetSequence = IdentityWithKeyFrom("event-log", "other") }),
        EventSequenceMutationValidator.ValidateRequest(_request with { TargetSequence = IdentityWithKeyFrom("\ud800", "event-log") })
    ];

    [Fact] void should_reject_missing_default_mismatched_and_malformed_identities() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidIdentity).ShouldBeTrue();
}
