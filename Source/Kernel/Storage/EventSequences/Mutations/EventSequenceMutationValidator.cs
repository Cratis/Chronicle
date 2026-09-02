// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Validates the provider-neutral event sequence mutation state model.
/// </summary>
public static class EventSequenceMutationValidator
{
    static readonly UTF8Encoding _strictUtf8 = new(false, true);

    /// <summary>
    /// Validates the inputs from which a definition is created.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="request">The mutation request.</param>
    /// <param name="target">The winning target.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateDefinitionInputs(EventSequenceKey? scope, EventSequenceMutationRequest? request, EventSequenceMutationTarget? target)
    {
        var validation = ValidateScope(scope);
        if (!validation.IsValid)
        {
            return validation;
        }

        validation = ValidateRequest(request);
        if (!validation.IsValid)
        {
            return validation;
        }

        validation = ValidateTarget(target);
        if (!validation.IsValid)
        {
            return validation;
        }

        var scopeIdentity = EventSequenceMutationIdentity.TryCreate(scope!.EventSequenceId.Value);
        if (!scopeIdentity.IsSuccess || !IdentityEquals(scopeIdentity.Identity, request!.TargetSequence))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidScope, nameof(request.TargetSequence));
        }

        return EventSequenceMutationValidationResult.Valid;
    }

    /// <summary>
    /// Validates a mutation request.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateRequest(EventSequenceMutationRequest? request)
    {
        if (request is null)
        {
            return Invalid(EventSequenceMutationValidationError.MissingValue, nameof(request));
        }

        if (request.Id is null || request.Id == EventSequenceMutationId.NotSet)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidId, nameof(request.Id));
        }

        var validation = ValidateIdentity(request.TargetSequence, nameof(request.TargetSequence));
        if (!validation.IsValid)
        {
            return validation;
        }

        validation = ValidateOrigin(request.Origin);
        if (!validation.IsValid)
        {
            return validation;
        }

        if (!Enum.IsDefined(request.Kind) || request.Kind == EventSequenceMutationKind.Unknown)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidEnum, nameof(request.Kind));
        }

        if (request.Command is null || request.Command.Payload is null || !IsStrictText(request.Command.Payload) || request.Command.Hash is null || string.IsNullOrEmpty(request.Command.Hash.Value) || !IsStrictText(request.Command.Hash.Value))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidCommand, nameof(request.Command));
        }

        return EventSequenceMutationValidationResult.Valid;
    }

    /// <summary>
    /// Validates a complete mutation definition and recomputes its digest.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="definition">The definition to validate.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateDefinition(EventSequenceKey? scope, EventSequenceMutationDefinition? definition)
    {
        if (definition is null)
        {
            return Invalid(EventSequenceMutationValidationError.MissingValue, nameof(definition));
        }

        var validation = ValidateDefinitionInputs(scope, definition.Request, definition.Target);
        if (!validation.IsValid)
        {
            return validation;
        }

        if (definition.DefinitionDigestV1 is null)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidDigest, nameof(definition.DefinitionDigestV1));
        }

        var digest = EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(scope!, definition.Request, definition.Target);
        return digest == definition.DefinitionDigestV1
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidDigest, nameof(definition.DefinitionDigestV1));
    }

    /// <summary>
    /// Validates an active mutation and its phase composite.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="mutation">The active mutation.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateActive(EventSequenceKey? scope, EventSequenceMutation? mutation)
    {
        if (mutation is null)
        {
            return Invalid(EventSequenceMutationValidationError.MissingValue, nameof(mutation));
        }

        var validation = ValidateDefinition(scope, mutation.Definition);
        if (!validation.IsValid)
        {
            return validation;
        }

        if (mutation.Ordinal is null || mutation.Ordinal.Value <= 0)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidOrdinal, nameof(mutation.Ordinal));
        }

        if (mutation.StateVersion is null || mutation.StateVersion.Value <= 0)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidStateVersion, nameof(mutation.StateVersion));
        }

        if (!Enum.IsDefined(mutation.Phase) || !Enum.IsDefined(mutation.BlockedFrom) || !Enum.IsDefined(mutation.RepairState))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidEnum, nameof(mutation.Phase));
        }

        return IsValidComposite(mutation.Phase, mutation.BlockedFrom, mutation.RepairState)
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidComposite, nameof(mutation.Phase));
    }

    /// <summary>
    /// Validates a permanent registration lifecycle matrix.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="registration">The registration to validate.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateRegistration(EventSequenceKey? scope, EventSequenceMutationRegistration? registration)
    {
        if (registration is null)
        {
            return Invalid(EventSequenceMutationValidationError.MissingValue, nameof(registration));
        }

        var validation = ValidateDefinition(scope, registration.Definition);
        if (!validation.IsValid)
        {
            return validation;
        }

        if (!Enum.IsDefined(registration.Lifecycle) || registration.Lifecycle == EventSequenceMutationRegistryLifecycle.Unknown)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidEnum, nameof(registration.Lifecycle));
        }

        var positiveOrdinal = registration.Ordinal is { Value: > 0 };
        var valid = registration.Lifecycle switch
        {
            EventSequenceMutationRegistryLifecycle.Claimed => registration.Ordinal is null && registration.TerminalWitness is null,
            EventSequenceMutationRegistryLifecycle.Bound => positiveOrdinal && registration.TerminalWitness is null,
            EventSequenceMutationRegistryLifecycle.Archived => positiveOrdinal && IsValidWitness(registration.TerminalWitness, registration.Definition.DefinitionDigestV1),
            _ => false
        };

        return valid
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidRegistration, nameof(registration.Lifecycle));
    }

    /// <summary>
    /// Validates a terminal history entry and recomputes its receipt digest.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="history">The history entry to validate.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateHistory(EventSequenceKey? scope, EventSequenceMutationHistoryEntry? history)
    {
        var validation = ValidateScope(scope);
        if (!validation.IsValid)
        {
            return validation;
        }

        if (history is null || history.Id is null || history.Id == EventSequenceMutationId.NotSet)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidTerminal, nameof(history));
        }

        if (history.Ordinal is null || history.Ordinal.Value <= 0 || !Enum.IsDefined(history.Kind) || history.Kind == EventSequenceMutationKind.Unknown)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidTerminal, nameof(history.Ordinal));
        }

        validation = ValidateOrigin(history.Origin);
        if (!validation.IsValid)
        {
            return validation;
        }

        validation = ValidateTarget(history.Target);
        if (!validation.IsValid)
        {
            return validation;
        }

        if (history.CommandHash is null || string.IsNullOrEmpty(history.CommandHash.Value) || !IsStrictText(history.CommandHash.Value) ||
            history.RepairState is not (EventSequenceMutationRepairState.NotRequired or EventSequenceMutationRepairState.Accepted or EventSequenceMutationRepairState.Unknown) ||
            history.TerminalWitness is not { } witness ||
            !IsValidWitness(witness, witness.DefinitionDigestV1))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidTerminal, nameof(history.TerminalWitness));
        }

        var expected = EventSequenceMutationDigestCalculator.CalculateReceiptDigest(
            scope!,
            history,
            witness.FinalStateVersion,
            witness.DefinitionDigestV1);

        return expected == witness.ReceiptDigestV1
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidDigest, nameof(history.TerminalWitness.ReceiptDigestV1));
    }

    /// <summary>
    /// Validates that an archived registration and terminal history entry describe the same mutation lineage.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="registration">The permanent archived registration.</param>
    /// <param name="history">The terminal history entry.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateArchivedRegistration(
        EventSequenceKey? scope,
        EventSequenceMutationRegistration? registration,
        EventSequenceMutationHistoryEntry? history)
    {
        var validation = ValidateRegistration(scope, registration);
        if (!validation.IsValid)
        {
            return validation;
        }

        if (registration!.Lifecycle != EventSequenceMutationRegistryLifecycle.Archived)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidRegistration, nameof(registration.Lifecycle));
        }

        validation = ValidateHistory(scope, history);
        if (!validation.IsValid)
        {
            return validation;
        }

        var definition = registration.Definition;
        var request = definition.Request;
        var exactLineage = registration.Ordinal == history!.Ordinal &&
                           registration.TerminalWitness == history.TerminalWitness &&
                           request.Id == history.Id &&
                           request.Origin == history.Origin &&
                           request.Kind == history.Kind &&
                           request.Command.Hash == history.CommandHash &&
                           definition.Target == history.Target &&
                           definition.DefinitionDigestV1 == history.TerminalWitness.DefinitionDigestV1;

        return exactLineage
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidTerminal, nameof(history));
    }

    /// <summary>
    /// Validates a state token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateToken(EventSequenceMutationStateToken? token)
    {
        if (token is null)
        {
            return Invalid(EventSequenceMutationValidationError.MissingValue, nameof(token));
        }

        var scopeValidation = ValidateScope(token.Scope);
        if (!scopeValidation.IsValid || !token.TargetKey.IsInitialized || token.Id is null || token.Id == EventSequenceMutationId.NotSet || token.Ordinal is null || token.Ordinal.Value <= 0 || token.DefinitionDigestV1 is null || token.StateVersion is null || token.StateVersion.Value <= 0)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidId, nameof(token));
        }

        if (!Enum.IsDefined(token.Phase) || !Enum.IsDefined(token.BlockedFrom) || !Enum.IsDefined(token.RepairState) || !IsValidComposite(token.Phase, token.BlockedFrom, token.RepairState))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidComposite, nameof(token.Phase));
        }

        var identity = EventSequenceMutationIdentity.TryCreate(token.Scope.EventSequenceId.Value);
        return identity.IsSuccess && identity.Identity!.Key == token.TargetKey
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidIdentity, nameof(token.TargetKey));
    }

    /// <summary>
    /// Determines whether a phase composite is a valid active state.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <param name="blockedFrom">The blocked source phase.</param>
    /// <param name="repairState">The repair state.</param>
    /// <returns><see langword="true"/> when the composite is valid.</returns>
    public static bool IsValidComposite(EventSequenceMutationPhase phase, EventSequenceMutationPhase blockedFrom, EventSequenceMutationRepairState repairState) =>
        (phase, blockedFrom, repairState) switch
        {
            (EventSequenceMutationPhase.Reserved or EventSequenceMutationPhase.Applying or EventSequenceMutationPhase.Verifying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified) => true,
            (EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying or EventSequenceMutationPhase.Verifying, EventSequenceMutationRepairState.Unspecified) => true,
            (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.NotRequired or EventSequenceMutationRepairState.Pending or EventSequenceMutationRepairState.Dispatching or EventSequenceMutationRepairState.Accepted or EventSequenceMutationRepairState.Unknown) => true,
            _ => false
        };

    static EventSequenceMutationValidationResult ValidateScope(EventSequenceKey? scope)
    {
        if (scope is null || scope.EventSequenceId is null || scope.EventStore is null || scope.Namespace is null ||
            scope.EventSequenceId == EventSequenceId.Unspecified || scope.EventStore.Value is null || scope.EventStore == Concepts.EventStoreName.NotSet ||
            scope.Namespace.Value is null || scope.Namespace == Concepts.EventStoreNamespaceName.NotSet ||
            !IsStrictText(scope.EventStore.Value) || !IsStrictText(scope.Namespace.Value))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidScope, nameof(scope));
        }

        var identity = EventSequenceMutationIdentity.TryCreate(scope.EventSequenceId.Value);
        return identity.IsSuccess
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidScope, nameof(scope.EventSequenceId));
    }

    static EventSequenceMutationValidationResult ValidateOrigin(EventSequenceMutationOrigin? origin)
    {
        if (origin?.SequenceNumber is null || !origin.SequenceNumber.IsActualValue)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidIdentity, nameof(origin));
        }

        return ValidateIdentity(origin.Sequence, nameof(origin.Sequence));
    }

    static EventSequenceMutationValidationResult ValidateIdentity(EventSequenceMutationIdentity? identity, string field)
    {
        if (identity?.Key.IsInitialized != true)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidIdentity, field);
        }

        var recreated = EventSequenceMutationIdentity.TryCreate(identity.Display);
        return recreated.IsSuccess && IdentityEquals(recreated.Identity, identity)
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidIdentity, field);
    }

    static EventSequenceMutationValidationResult ValidateTarget(EventSequenceMutationTarget? target)
    {
        if (target is not { Start: { } start, EndExclusive: { } endExclusive, ExpectedCount: { } expectedCount } ||
            !start.IsActualValue || !endExclusive.IsActualValue || expectedCount == Concepts.Events.EventCount.NotSet ||
            endExclusive.Value < start.Value || expectedCount.Value > ulong.MaxValue - start.Value ||
            start.Value + expectedCount.Value != endExclusive.Value)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidTarget, nameof(target));
        }

        return EventSequenceMutationValidationResult.Valid;
    }

    static bool IsValidWitness(EventSequenceMutationTerminalWitness? witness, EventSequenceMutationDefinitionDigestV1? expectedDefinitionDigest) =>
        witness is
        {
            FinalStateVersion.Value: > 0,
            DefinitionDigestV1: not null,
            ReceiptDigestV1: not null
        } &&
        expectedDefinitionDigest is not null &&
        witness.DefinitionDigestV1 == expectedDefinitionDigest;

    static bool IdentityEquals(EventSequenceMutationIdentity? left, EventSequenceMutationIdentity? right) =>
        left is not null &&
        right is not null &&
        string.Equals(left.Display, right.Display, StringComparison.Ordinal) &&
        left.Key == right.Key;

    static bool IsStrictText(string value)
    {
        try
        {
            var bytes = _strictUtf8.GetBytes(value);
            return string.Equals(value, _strictUtf8.GetString(bytes), StringComparison.Ordinal);
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    static EventSequenceMutationValidationResult Invalid(EventSequenceMutationValidationError error, string field) => new(error, field);
}
