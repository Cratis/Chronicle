// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Guards the closed payload matrices of provider-facing mutation registry results.
/// </summary>
static class EventSequenceMutationRegistryResultGuard
{
    /// <summary>
    /// Requires a reference payload.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="value">The payload value.</param>
    /// <param name="field">The payload field name.</param>
    /// <returns>The required payload.</returns>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when the payload is missing.</exception>
    public static T Require<T>(T? value, string field)
        where T : class
    {
        if (value is null)
        {
            throw new InvalidEventSequenceMutationRegistryResult(field);
        }

        return value;
    }

    /// <summary>
    /// Requires a payload matrix condition.
    /// </summary>
    /// <param name="condition">The condition to require.</param>
    /// <param name="field">The contradictory field name.</param>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when the condition is not satisfied.</exception>
    public static void Require(bool condition, string field)
    {
        if (!condition)
        {
            throw new InvalidEventSequenceMutationRegistryResult(field);
        }
    }

    /// <summary>
    /// Requires a payload to be absent.
    /// </summary>
    /// <param name="value">The payload value.</param>
    /// <param name="field">The payload field name.</param>
    public static void RequireAbsent(object? value, string field) => Require(value is null, field);

    /// <summary>
    /// Requires failed validation details.
    /// </summary>
    /// <param name="validation">The validation details.</param>
    /// <returns>The failed validation details.</returns>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when validation details are missing or successful.</exception>
    public static EventSequenceMutationValidationResult RequireInvalid(EventSequenceMutationValidationResult? validation)
    {
        if (validation?.IsValid != false)
        {
            throw new InvalidEventSequenceMutationRegistryResult(nameof(validation));
        }

        if (!Enum.IsDefined(validation.Error) ||
            validation.Error == EventSequenceMutationValidationError.None ||
            string.IsNullOrWhiteSpace(validation.Field))
        {
            throw new InvalidEventSequenceMutationRegistryResult(nameof(validation));
        }

        return validation;
    }

    /// <summary>
    /// Requires a complete, internally consistent active mutation payload.
    /// </summary>
    /// <param name="active">The active mutation.</param>
    /// <param name="token">The complete state token.</param>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when the active payload is incomplete or contradictory.</exception>
    public static void RequireActivePayload(EventSequenceMutation? active, EventSequenceMutationStateToken? token)
    {
        var requiredActive = Require(active, nameof(active));
        var requiredToken = Require(token, nameof(token));

        var tokenValidation = EventSequenceMutationValidator.ValidateToken(requiredToken);
        var activeValidation = EventSequenceMutationValidator.ValidateActive(requiredToken.Scope, requiredActive);
        if (!tokenValidation.IsValid ||
            !activeValidation.IsValid ||
            requiredActive.Id != requiredToken.Id ||
            requiredActive.TargetSequence.Key != requiredToken.TargetKey ||
            requiredActive.Ordinal != requiredToken.Ordinal ||
            requiredActive.Definition.DefinitionDigestV1 != requiredToken.DefinitionDigestV1 ||
            requiredActive.Phase != requiredToken.Phase ||
            requiredActive.BlockedFrom != requiredToken.BlockedFrom ||
            requiredActive.RepairState != requiredToken.RepairState ||
            requiredActive.StateVersion != requiredToken.StateVersion)
        {
            throw new InvalidEventSequenceMutationRegistryResult(nameof(token));
        }
    }

    /// <summary>
    /// Requires an exact permanent archived registration and verified history payload.
    /// </summary>
    /// <param name="scope">The exact event sequence scope.</param>
    /// <param name="registration">The permanent archived registration.</param>
    /// <param name="history">The exact history entry.</param>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when the archived payload is incomplete or contradictory.</exception>
    public static void RequireArchivedPayload(
        Concepts.EventSequences.EventSequenceKey scope,
        EventSequenceMutationRegistration? registration,
        EventSequenceMutationHistoryEntry? history)
    {
        var requiredRegistration = Require(registration, nameof(registration));
        var requiredHistory = Require(history, nameof(history));
        if (!EventSequenceMutationValidator.ValidateArchivedRegistration(scope, requiredRegistration, requiredHistory).IsValid)
        {
            throw new InvalidEventSequenceMutationRegistryResult(nameof(registration));
        }
    }

    /// <summary>
    /// Requires a set mutation identifier for a non-sensitive conflict result.
    /// </summary>
    /// <param name="mutationId">The mutation identifier.</param>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when the identifier is missing or not set.</exception>
    public static void RequireMutationId(Concepts.EventSequences.Mutations.EventSequenceMutationId? mutationId)
    {
        if (mutationId is null || mutationId == Concepts.EventSequences.Mutations.EventSequenceMutationId.NotSet)
        {
            throw new InvalidEventSequenceMutationRegistryResult(nameof(mutationId));
        }
    }

    /// <summary>
    /// Requires the unsealed head produced by beginning tracking.
    /// </summary>
    /// <param name="head">The tracked head.</param>
    /// <returns>The required head.</returns>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when the tracked head is missing or not unsealed.</exception>
    public static EventSequenceMutationHead RequireTrackedHead(EventSequenceMutationHead? head)
    {
        var requiredHead = Require(head, nameof(head));
        if (requiredHead.Coverage != EventSequenceMutationCoverage.Unsealed)
        {
            throw new InvalidEventSequenceMutationRegistryResult(nameof(EventSequenceMutationHead.Coverage));
        }

        return requiredHead;
    }

    /// <summary>
    /// Requires a head carrying another active mutation.
    /// </summary>
    /// <param name="head">The observed head.</param>
    /// <returns>The required busy head.</returns>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when the head is missing or has no active mutation.</exception>
    public static EventSequenceMutationHead RequireBusyHead(EventSequenceMutationHead? head)
    {
        var requiredHead = Require(head, nameof(head));
        if (requiredHead.Active is null)
        {
            throw new InvalidEventSequenceMutationRegistryResult(nameof(EventSequenceMutationHead.Active));
        }

        return requiredHead;
    }
}
