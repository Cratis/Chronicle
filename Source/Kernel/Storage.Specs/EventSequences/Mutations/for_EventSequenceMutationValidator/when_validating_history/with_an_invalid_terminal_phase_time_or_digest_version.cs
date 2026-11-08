// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_an_invalid_terminal_phase_time_or_digest_version : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalPhase = EventSequenceMutationPhase.Reserved }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalPhase = EventSequenceMutationPhase.None }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { ArchivedAt = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123).AddTicks(1) }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { ArchivedAt = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123).ToOffset(TimeSpan.FromHours(2)) }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { ArchivedAt = DateTimeOffset.FromUnixTimeMilliseconds(-1) }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalWitness = _witness with { DigestVersion = EventSequenceMutationDigestVersion.Unspecified } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalWitness = _witness with { DigestVersion = (EventSequenceMutationDigestVersion)byte.MaxValue } })
    ];

    [Fact] void should_reject_every_invalid_field() => _results.All(_ => !_.IsValid).ShouldBeTrue();
}
