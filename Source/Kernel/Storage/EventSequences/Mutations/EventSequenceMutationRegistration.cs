// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the permanent registration of a mutation identifier.
/// </summary>
/// <param name="Definition">The immutable winning definition.</param>
/// <param name="Lifecycle">The registration lifecycle.</param>
/// <param name="Ordinal">The assigned ordinal, when bound.</param>
/// <param name="TerminalWitness">The terminal witness, when archived.</param>
public sealed record EventSequenceMutationRegistration(
    EventSequenceMutationDefinition Definition,
    EventSequenceMutationRegistryLifecycle Lifecycle,
    EventSequenceMutationOrdinal? Ordinal,
    EventSequenceMutationTerminalWitness? TerminalWitness)
{
    /// <summary>
    /// Determines whether a request exactly matches the permanently registered request.
    /// </summary>
    /// <param name="request">The request to compare. Any newly proposed target is intentionally ignored.</param>
    /// <returns><see langword="true"/> when the request is exactly equal.</returns>
    public bool IsExactRequest(EventSequenceMutationRequest? request) => Definition?.IsExactRequest(request) == true;
}
