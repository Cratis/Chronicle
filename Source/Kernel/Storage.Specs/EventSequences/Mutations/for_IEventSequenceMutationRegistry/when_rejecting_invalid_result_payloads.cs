// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_IEventSequenceMutationRegistry;

public class when_rejecting_invalid_result_payloads : given.a_registry_contract
{
    [Fact]
    void should_reject_null_mandatory_payloads_from_every_named_factory()
    {
        var errors = new[]
        {
            Catch.Exception(() => EventSequenceMutationBeginResult.Reserved(null!, _token)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Reserved(_active, null!)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Resumed(null!, _token)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Resumed(_active, null!)),
            Catch.Exception(() => EventSequenceMutationBeginResult.RecoveredReservation(null!, _token)),
            Catch.Exception(() => EventSequenceMutationBeginResult.RecoveredReservation(_active, null!)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Archived(null!, _archivedRegistration, _history)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Archived(_scope, null!, _history)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Archived(_scope, _archivedRegistration, null!)),
            Catch.Exception(() => EventSequenceMutationBeginResult.MutationAlreadyInProgress(null!)),
            Catch.Exception(() => EventSequenceMutationBeginResult.DefinitionConflict(null!)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Invalid(null!)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.Applied(null!, _token)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.Applied(_active, null!)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyApplied(null!, _token)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyApplied(_active, null!)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyArchived(null!, _archivedRegistration, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyArchived(_scope, null!, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyArchived(_scope, _archivedRegistration, null!)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.Invalid(null!)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.Archived(null!, _archivedRegistration, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.Archived(_scope, null!, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.Archived(_scope, _archivedRegistration, null!)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.AlreadyArchived(null!, _archivedRegistration, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.AlreadyArchived(_scope, null!, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.AlreadyArchived(_scope, _archivedRegistration, null!)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.Invalid(null!)),
            Catch.Exception(() => EventSequenceMutationTrackingResult.Invalid(null!))
        };

        errors.All(_ => _.GetType() == typeof(InvalidEventSequenceMutationRegistryResult)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_not_set_or_undefined_conflict_payloads()
    {
        var errors = new[]
        {
            Catch.Exception(() => EventSequenceMutationBeginResult.MutationAlreadyInProgress(EventSequenceMutationId.NotSet)),
            Catch.Exception(() => EventSequenceMutationBeginResult.DefinitionConflict(EventSequenceMutationId.NotSet)),
            Catch.Exception(() => EventSequenceMutationTrackingResult.Conflict((EventSequenceMutationCoverage)int.MaxValue))
        };

        errors.All(_ => _.GetType() == typeof(InvalidEventSequenceMutationRegistryResult)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_active_payloads_with_a_contradictory_token()
    {
        var contradictoryActive = _active with { StateVersion = 2L };
        var errors = new[]
        {
            Catch.Exception(() => EventSequenceMutationBeginResult.Reserved(contradictoryActive, _token)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Resumed(contradictoryActive, _token)),
            Catch.Exception(() => EventSequenceMutationBeginResult.RecoveredReservation(contradictoryActive, _token)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.Applied(contradictoryActive, _token)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyApplied(contradictoryActive, _token))
        };

        errors.All(_ => _.GetType() == typeof(InvalidEventSequenceMutationRegistryResult)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_active_payloads_with_a_tampered_definition_digest_even_when_the_token_matches()
    {
        var digestBytes = _definition.DefinitionDigestV1.Snapshot();
        digestBytes[0] ^= byte.MaxValue;
        var tamperedDefinition = _definition with { DefinitionDigestV1 = new(digestBytes) };
        var tamperedActive = _active with { Definition = tamperedDefinition };
        var matchingToken = CreateUncheckedToken(tamperedActive);
        var errors = new[]
        {
            Catch.Exception(() => EventSequenceMutationBeginResult.Reserved(tamperedActive, matchingToken)),
            Catch.Exception(() => EventSequenceMutationBeginResult.Resumed(tamperedActive, matchingToken)),
            Catch.Exception(() => EventSequenceMutationBeginResult.RecoveredReservation(tamperedActive, matchingToken)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.Applied(tamperedActive, matchingToken)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyApplied(tamperedActive, matchingToken))
        };

        errors.All(_ => _.GetType() == typeof(InvalidEventSequenceMutationRegistryResult)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_contradictory_archived_payloads()
    {
        var errors = new[]
        {
            Catch.Exception(() => EventSequenceMutationBeginResult.Archived(_scope, _activeRegistration, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyArchived(_scope, _activeRegistration, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.Archived(_scope, _activeRegistration, _history)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.AlreadyArchived(_scope, _activeRegistration, _history))
        };

        errors.All(_ => _.GetType() == typeof(InvalidEventSequenceMutationRegistryResult)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_archived_payloads_with_a_malformed_witness_even_when_registration_and_history_match()
    {
        var receiptBytes = _history.TerminalWitness.ReceiptDigestV1.Snapshot();
        receiptBytes[0] ^= byte.MaxValue;
        var malformedWitness = _history.TerminalWitness with { ReceiptDigestV1 = new(receiptBytes) };
        var malformedHistory = _history with { TerminalWitness = malformedWitness };
        var malformedRegistration = _archivedRegistration with { TerminalWitness = malformedWitness };
        var errors = new[]
        {
            Catch.Exception(() => EventSequenceMutationBeginResult.Archived(_scope, malformedRegistration, malformedHistory)),
            Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.AlreadyArchived(_scope, malformedRegistration, malformedHistory)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.Archived(_scope, malformedRegistration, malformedHistory)),
            Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.AlreadyArchived(_scope, malformedRegistration, malformedHistory))
        };

        errors.All(_ => _.GetType() == typeof(InvalidEventSequenceMutationRegistryResult)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_successful_validation_as_invalid_details()
    {
        var errors = InvalidFactoryErrors(EventSequenceMutationValidationResult.Valid);

        errors.All(_ => _.GetType() == typeof(InvalidEventSequenceMutationRegistryResult)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_every_contradictory_validation_detail_from_all_invalid_factories()
    {
        var validations = new[]
        {
            CreateUncheckedValidation(EventSequenceMutationValidationError.None, "request"),
            CreateUncheckedValidation(EventSequenceMutationValidationError.MissingValue, string.Empty),
            CreateUncheckedValidation(EventSequenceMutationValidationError.MissingValue, " "),
            CreateUncheckedValidation((EventSequenceMutationValidationError)int.MaxValue, "request")
        };

        var errors = validations.SelectMany(InvalidFactoryErrors).ToArray();

        errors.Length.ShouldEqual(validations.Length * 4);
        errors.All(_ => _.GetType() == typeof(InvalidEventSequenceMutationRegistryResult)).ShouldBeTrue();
    }

    Exception[] InvalidFactoryErrors(EventSequenceMutationValidationResult validation) =>
    [
        Catch.Exception(() => EventSequenceMutationBeginResult.Invalid(validation)),
        Catch.Exception(() => EventSequenceMutationRegistryTransitionResult.Invalid(validation)),
        Catch.Exception(() => EventSequenceMutationRegistryArchiveResult.Invalid(validation)),
        Catch.Exception(() => EventSequenceMutationTrackingResult.Invalid(validation))
    ];

    EventSequenceMutationValidationResult CreateUncheckedValidation(EventSequenceMutationValidationError error, string field) =>
        (EventSequenceMutationValidationResult)typeof(EventSequenceMutationValidationResult)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(_ => _.GetParameters().Length == 2)
            .Invoke([error, field]);

    EventSequenceMutationStateToken CreateUncheckedToken(EventSequenceMutation active) =>
        (EventSequenceMutationStateToken)typeof(EventSequenceMutationStateToken)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(_ => _.GetParameters().Length == 2)
            .Invoke([_scope, active]);
}
