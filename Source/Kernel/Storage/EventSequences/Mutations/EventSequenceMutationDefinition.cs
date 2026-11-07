// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents a registered mutation request with its winning, frozen target and definition digest.
/// </summary>
/// <param name="Request">The exact registered request.</param>
/// <param name="Target">The target frozen when the request won registration.</param>
/// <param name="DefinitionDigestV1">The digest binding the scope, request, and winning target.</param>
/// <param name="DefinitionDigestVersion">The digest version that produced <paramref name="DefinitionDigestV1"/>; the digest is always recomputed with it.</param>
public sealed record EventSequenceMutationDefinition(
    EventSequenceMutationRequest Request,
    EventSequenceMutationTarget Target,
    EventSequenceMutationDefinitionDigestV1 DefinitionDigestV1,
    EventSequenceMutationDigestVersion DefinitionDigestVersion)
{
    /// <summary>
    /// Creates and validates a definition, calculating its digest from the supplied scope, request, and target.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="request">The request to register.</param>
    /// <param name="target">The winning target to freeze.</param>
    /// <returns>The validated definition.</returns>
    /// <exception cref="InvalidEventSequenceMutation">Thrown when any input is malformed.</exception>
    public static EventSequenceMutationDefinition Create(EventSequenceKey scope, EventSequenceMutationRequest request, EventSequenceMutationTarget target) =>
        Create(scope, request, target, EventSequenceMutationDigestCalculator.CurrentDigestVersion);

    /// <summary>
    /// Creates and validates a definition with a specific digest version, for example the one persisted with an archived receipt.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="request">The request to register.</param>
    /// <param name="target">The winning target to freeze.</param>
    /// <param name="version">The digest version to calculate the definition digest with.</param>
    /// <returns>The validated definition.</returns>
    /// <exception cref="InvalidEventSequenceMutation">Thrown when any input is malformed or the digest version is not supported.</exception>
    public static EventSequenceMutationDefinition Create(
        EventSequenceKey scope,
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget target,
        EventSequenceMutationDigestVersion version)
    {
        var validation = EventSequenceMutationValidator.ValidateDefinitionInputs(scope, request, target);
        validation.ThrowIfInvalid();
        if (!EventSequenceMutationDigestCalculator.IsSupportedDigestVersion(version))
        {
            throw new InvalidEventSequenceMutation(new(EventSequenceMutationValidationError.InvalidDigest, nameof(version)));
        }

        return new(request, target, EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(scope, request, target, version), version);
    }

    /// <summary>
    /// Determines whether this definition was registered from the exact request.
    /// </summary>
    /// <param name="request">The request to compare. A newly proposed target is intentionally not part of this comparison.</param>
    /// <returns><see langword="true"/> when every request field is exactly equal.</returns>
    public bool IsExactRequest(EventSequenceMutationRequest? request) => Request?.ExactlyEquals(request) == true;
}
