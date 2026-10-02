// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents committed current observer state, not a queue of lifecycle transitions.
/// </summary>
/// <remarks>
/// Open incidents converge to this level. Unobserved intermediate episodes may be omitted. Ending hints are
/// activation-local: after a crash an unknown partition ending defaults to Recovered and quarantine to Cleared.
/// The Retired desired level always clears as Removed, whether or not resource cleanup has completed.
/// </remarks>
/// <param name="Observer">The observer key.</param>
/// <param name="FailedPartitions">Current partition episodes.</param>
/// <param name="IsQuarantined">Whether operational quarantine is desired.</param>
/// <param name="Disposition">The durable lifecycle disposition.</param>
/// <param name="MaxRetryAttempts">Maximum retries, or zero for unlimited retries.</param>
public record ObserverAlertSnapshot(
    ObserverKey Observer,
    IReadOnlyCollection<FailedPartitionSnapshot> FailedPartitions,
    bool IsQuarantined,
    AlertDisposition Disposition,
    int MaxRetryAttempts)
{
    /// <summary>
    /// Gets the lifecycle token persisted by the observer.
    /// </summary>
    public Guid LifecycleId { get; init; }

    /// <summary>
    /// Gets the committed alert revision in that lifecycle.
    /// </summary>
    public long Revision { get; init; }

    /// <summary>
    /// Gets the source-owned current quarantine episode identity.
    /// </summary>
    public Guid? QuarantineEpisodeId { get; init; }

    /// <summary>
    /// Gets the reasons for identified ended episodes that have not yet been acknowledged.
    /// </summary>
    /// <remarks>
    /// Keys carry the incident's GUID value because the Orleans JSON codec cannot use concept converters for
    /// dictionary property names. The observer keeps strongly typed incident keys in its local bookkeeping.
    /// </remarks>
    public IReadOnlyDictionary<Guid, AlertClearedReason> Endings { get; init; } = ImmutableDictionary<Guid, AlertClearedReason>.Empty;
}
