// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Source-owned durable coordination for one quarantined webhook's real-delivery probe.
/// </summary>
/// <param name="Id">The probe generation.</param>
/// <param name="QuarantineId">The quarantine episode that authorized probing.</param>
/// <param name="Due">The sampled deadline, never resampled on activation.</param>
/// <param name="Failures">The number of failed probes, independent of partition attempts.</param>
public record WebhookProbeState(Guid Id, Guid QuarantineId, DateTimeOffset Due, int Failures)
{
    /// <summary>Gets the authoritative definition fingerprint captured by a claim.</summary>
    public string? DefinitionFingerprint { get; init; }

    /// <summary>Gets the claimed failure episode, if delivery might be in flight.</summary>
    public FailedPartitionId? EpisodeId { get; init; }

    /// <summary>Gets the inclusive cursor captured by the claim.</summary>
    public EventSequenceNumber? Position { get; init; }

    /// <summary>Gets whether delivery succeeded and only local recovery remains.</summary>
    public bool ResumePending { get; init; }

    /// <summary>Gets the next undelivered event after successful delivery.</summary>
    public EventSequenceNumber? ResumePosition { get; init; }

    /// <summary>Gets the durable absolute partition counts after successful delivery.</summary>
    public IReadOnlyList<WebhookProbePartitionCount> PartitionCounts { get; init; } = [];
}
