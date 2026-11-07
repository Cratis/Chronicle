// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Builds the two requests <see cref="ArchiveRetryConformance"/> archives and reuses the head for.
/// </summary>
public static class ArchiveRetryRequests
{
    /// <summary>
    /// Gets the target range proposed for both mutations.
    /// </summary>
    public static EventSequenceMutationTarget ProposedTarget { get; } = new(10UL, 13UL, 3UL);

    /// <summary>
    /// Creates a conformance for a request pair that targets the same sequence.
    /// </summary>
    /// <returns>The conformance.</returns>
    public static ArchiveRetryConformance Create()
    {
        var target = EventSequenceMutationIdentity.TryCreate("target-sequence").Identity!;
        return new(Request(target, 42UL), Request(target, 43UL), ProposedTarget);
    }

    static EventSequenceMutationRequest Request(EventSequenceMutationIdentity target, ulong originSequenceNumber)
    {
        var origin = EventSequenceMutationIdentity.TryCreate("origin-sequence").Identity!;
        const EventSequenceMutationKind kind = EventSequenceMutationKind.Revision;
        var id = EventSequenceMutationDigestCalculator.CalculateId(target, origin, originSequenceNumber, kind);
        return new(id, target, new(origin, originSequenceNumber), kind, new("{\"name\":\"Ada\"}", "command-hash"));
    }
}
