// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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

        if (IsUnsetId(request.Id))
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

        if (!IsValidCommand(request.Command))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidCommand, nameof(request.Command));
        }

        return EventSequenceMutationValidationResult.Valid;
    }

    /// <summary>
    /// Validates that a mutation request carries the deterministic identifier for its target, origin and kind.
    /// </summary>
    /// <param name="request">The mutation request.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateDeterministicId(EventSequenceMutationRequest? request)
    {
        var validation = ValidateRequest(request);
        if (!validation.IsValid)
        {
            return validation;
        }

        var expected = EventSequenceMutationDigestCalculator.CalculateId(
            request!.TargetSequence,
            request.Origin.Sequence,
            request.Origin.SequenceNumber,
            request.Kind);
        return request.Id == expected
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidId, nameof(request.Id));
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

        if (!EventSequenceMutationDigestCalculator.IsSupportedDigestVersion(definition.DefinitionDigestVersion))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidDigest, nameof(definition.DefinitionDigestVersion));
        }

        var digest = EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(scope!, definition.Request, definition.Target, definition.DefinitionDigestVersion);
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

        if (mutation.Ordinal is not { Value: > 0 })
        {
            return Invalid(EventSequenceMutationValidationError.InvalidOrdinal, nameof(mutation.Ordinal));
        }

        if (mutation.StateVersion is not { Value: > 0 })
        {
            return Invalid(EventSequenceMutationValidationError.InvalidStateVersion, nameof(mutation.StateVersion));
        }

        if (!AreDefined(mutation.Phase, mutation.BlockedFrom, mutation.RepairState))
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

        return IsLifecycleConsistent(registration)
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

        if (history is null || IsUnsetId(history.Id))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidTerminal, nameof(history));
        }

        if (history.Ordinal is not { Value: > 0 } || !IsKnownKind(history.Kind))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidTerminal, nameof(history.Ordinal));
        }

        validation = ValidateTarget(history.Target);
        if (!validation.IsValid)
        {
            return validation;
        }

        if (!TryGetTerminalWitness(history, out var witness))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidTerminal, nameof(history.TerminalWitness));
        }

        return ValidateReceiptDigest(scope!, history, witness);
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

        return IsExactLineage(registration, history!)
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
        if (!scopeValidation.IsValid || !HasValidTokenState(token))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidId, nameof(token));
        }

        if (!AreDefined(token.Phase, token.BlockedFrom, token.RepairState) || !IsValidComposite(token.Phase, token.BlockedFrom, token.RepairState))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidComposite, nameof(token.Phase));
        }

        var identity = EventSequenceMutationIdentity.TryCreate(token.Scope.EventSequenceId.Value);
        return identity.IsSuccess && identity.Identity!.Key == token.TargetKey
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidIdentity, nameof(token.TargetKey));
    }

    /// <summary>
    /// Validates an event sequence mutation identity.
    /// </summary>
    /// <param name="identity">The identity to validate.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateIdentity(EventSequenceMutationIdentity? identity) =>
        ValidateIdentity(identity, nameof(identity));

    /// <summary>
    /// Validates that tracking may begin from the expected coverage.
    /// </summary>
    /// <param name="expected">The expected current coverage.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateTrackingCoverage(EventSequenceMutationCoverage expected) =>
        Enum.IsDefined(expected) && expected == EventSequenceMutationCoverage.Untracked
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidEnum, nameof(expected));

    /// <summary>
    /// Validates a frozen mutation target.
    /// </summary>
    /// <param name="target">The target to validate.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateTarget(EventSequenceMutationTarget? target)
    {
        if (!IsConsistentTarget(target))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidTarget, nameof(target));
        }

        return EventSequenceMutationValidationResult.Valid;
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

    /// <summary>
    /// Validates the complete event sequence mutation registry scope.
    /// </summary>
    /// <param name="scope">The scope to validate.</param>
    /// <returns>The validation result.</returns>
    public static EventSequenceMutationValidationResult ValidateScope(EventSequenceKey? scope)
    {
        if (!HasScopeParts(scope) || !HasValidScopeValues(scope!))
        {
            return Invalid(EventSequenceMutationValidationError.InvalidScope, nameof(scope));
        }

        var identity = EventSequenceMutationIdentity.TryCreate(scope!.EventSequenceId.Value);
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

    static EventSequenceMutationValidationResult ValidateReceiptDigest(EventSequenceKey scope, EventSequenceMutationHistoryEntry history, EventSequenceMutationTerminalWitness witness)
    {
        var expected = EventSequenceMutationDigestCalculator.CalculateReceiptDigest(
            scope,
            history,
            witness.FinalStateVersion,
            witness.DefinitionDigestV1,
            witness.DigestVersion);

        return expected == witness.ReceiptDigestV1
            ? EventSequenceMutationValidationResult.Valid
            : Invalid(EventSequenceMutationValidationError.InvalidDigest, nameof(history.TerminalWitness.ReceiptDigestV1));
    }

    static bool IsUnsetId(EventSequenceMutationId? id) => id is null || id == EventSequenceMutationId.NotSet;

    static bool IsKnownKind(EventSequenceMutationKind kind) => Enum.IsDefined(kind) && kind != EventSequenceMutationKind.Unknown;

    static bool AreDefined(EventSequenceMutationPhase phase, EventSequenceMutationPhase blockedFrom, EventSequenceMutationRepairState repairState) =>
        Enum.IsDefined(phase) && Enum.IsDefined(blockedFrom) && Enum.IsDefined(repairState);

    static bool IsValidCommand(EventSequenceMutationCommandEnvelope? command) =>
        command is { Payload: not null } &&
        IsStrictText(command.Payload) &&
        command.Hash is { } hash &&
        !string.IsNullOrEmpty(hash.Value) &&
        IsStrictText(hash.Value);

    static bool HasValidTokenState(EventSequenceMutationStateToken token) =>
        token.TargetKey.IsInitialized &&
        !IsUnsetId(token.Id) &&
        token.Ordinal is { Value: > 0 } &&
        token.DefinitionDigestV1 is not null &&
        token.StateVersion is { Value: > 0 };

    static bool IsLifecycleConsistent(EventSequenceMutationRegistration registration)
    {
        var positiveOrdinal = registration.Ordinal is { Value: > 0 };
        return registration.Lifecycle switch
        {
            EventSequenceMutationRegistryLifecycle.Claimed => registration.Ordinal is null && registration.TerminalWitness is null,
            EventSequenceMutationRegistryLifecycle.Bound => positiveOrdinal && registration.TerminalWitness is null,
            EventSequenceMutationRegistryLifecycle.Archived => positiveOrdinal && IsValidWitness(registration.TerminalWitness, registration.Definition.DefinitionDigestV1),
            _ => false
        };
    }

    static bool TryGetTerminalWitness(EventSequenceMutationHistoryEntry history, [NotNullWhen(true)] out EventSequenceMutationTerminalWitness? witness)
    {
        witness = history.TerminalWitness;
        return history.TerminalPhase == EventSequenceMutationPhase.SourceCommitted &&
               EventSequenceMutationArchiveTime.IsNormalized(history.ArchivedAt) &&
               history.RepairState is EventSequenceMutationRepairState.NotRequired or EventSequenceMutationRepairState.Accepted or EventSequenceMutationRepairState.Unknown &&
               witness is not null &&
               IsValidWitness(witness, witness.DefinitionDigestV1);
    }

    static bool IsExactLineage(EventSequenceMutationRegistration registration, EventSequenceMutationHistoryEntry history)
    {
        var definition = registration.Definition;
        var request = definition.Request;
        return ValidateDeterministicId(request).IsValid &&
               registration.Ordinal == history.Ordinal &&
               registration.TerminalWitness == history.TerminalWitness &&
               request.Id == history.Id &&
               request.Kind == history.Kind &&
               definition.Target == history.Target &&
               definition.DefinitionDigestV1 == history.TerminalWitness.DefinitionDigestV1 &&
               definition.DefinitionDigestVersion == history.TerminalWitness.DigestVersion;
    }

    static bool IsConsistentTarget(EventSequenceMutationTarget? target) =>
        target is { Start: { } start, EndExclusive: { } endExclusive, ExpectedCount: { } expectedCount } &&
        start.IsActualValue &&
        endExclusive.IsActualValue &&
        expectedCount != Concepts.Events.EventCount.NotSet &&
        IsConsistentRange(start.Value, endExclusive.Value, expectedCount.Value);

    static bool IsConsistentRange(ulong start, ulong endExclusive, ulong expectedCount) =>
        endExclusive >= start &&
        expectedCount <= ulong.MaxValue - start &&
        start + expectedCount == endExclusive;

    static bool HasScopeParts(EventSequenceKey? scope) =>
        scope is { EventSequenceId: not null, EventStore: not null, Namespace: not null };

    static bool HasValidScopeValues(EventSequenceKey scope) =>
        !HasUnsetScopeValues(scope) && HasStrictScopeText(scope);

    static bool HasUnsetScopeValues(EventSequenceKey scope) =>
        scope.EventSequenceId == EventSequenceId.Unspecified ||
        scope.EventStore.Value is null ||
        scope.EventStore == Concepts.EventStoreName.NotSet ||
        scope.Namespace.Value is null ||
        scope.Namespace == Concepts.EventStoreNamespaceName.NotSet;

    static bool HasStrictScopeText(EventSequenceKey scope) =>
        IsStrictText(scope.EventStore.Value) && IsStrictText(scope.Namespace.Value);

    static bool IsValidWitness(EventSequenceMutationTerminalWitness? witness, EventSequenceMutationDefinitionDigestV1? expectedDefinitionDigest) =>
        witness is
        {
            FinalStateVersion.Value: > 0,
            DefinitionDigestV1: not null,
            ReceiptDigestV1: not null
        } &&
        expectedDefinitionDigest is not null &&
        EventSequenceMutationDigestCalculator.IsSupportedDigestVersion(witness.DigestVersion) &&
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

    static EventSequenceMutationValidationResult Invalid(EventSequenceMutationValidationError error, string field) => EventSequenceMutationValidationResult.Failed(error, field);
}
