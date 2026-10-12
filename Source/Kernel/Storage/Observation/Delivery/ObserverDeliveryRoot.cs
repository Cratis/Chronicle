// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents the compare-and-swap root guarding all delivery rows for one observer.
/// </summary>
/// <param name="Observer">The observer.</param>
/// <param name="Format">The root format.</param>
/// <param name="Lifecycle">The delivery owner lifecycle.</param>
/// <param name="Generation">The row generation.</param>
/// <param name="KeyDefinitionVersion">The key definition version.</param>
/// <param name="MapVersion">The lane map version.</param>
/// <param name="Lanes">Lane progress, copied at construction.</param>
/// <param name="ActivationEpoch">The activation ownership epoch.</param>
/// <param name="ScheduleClock">The scheduling clock in UTC ticks.</param>
/// <param name="ScheduleTicket">The scheduling ticket counter.</param>
/// <param name="CoverageHead">Reserved coverage metadata.</param>
/// <param name="Operation">Reserved operation metadata.</param>
/// <param name="Revision">The root revision.</param>
/// <param name="LastCommit">A new token for this commit.</param>
public sealed record ObserverDeliveryRoot(
    ObserverId Observer,
    DeliveryRootFormat Format,
    DeliveryLifecycle Lifecycle,
    DeliveryGeneration Generation,
    int KeyDefinitionVersion,
    int MapVersion,
    ImmutableArray<DeliveryLane> Lanes,
    DeliveryEpoch ActivationEpoch,
    long ScheduleClock,
    DeliveryTicket ScheduleTicket,
    VersionedEnvelope? CoverageHead,
    VersionedEnvelope? Operation,
    DeliveryRevision Revision,
    DeliveryCommitId LastCommit)
{
    /// <summary>
    /// Gets the immutable lane progress.
    /// </summary>
    public ImmutableArray<DeliveryLane> Lanes { get; } = DeliveryCollections.Copy(Lanes);

    /// <inheritdoc/>
    public bool Equals(ObserverDeliveryRoot? other) => other is not null &&
        Observer == other.Observer && Format == other.Format && Lifecycle == other.Lifecycle &&
        Generation == other.Generation && KeyDefinitionVersion == other.KeyDefinitionVersion && MapVersion == other.MapVersion &&
        Lanes.SequenceEqual(other.Lanes) && ActivationEpoch == other.ActivationEpoch && ScheduleClock == other.ScheduleClock &&
        ScheduleTicket == other.ScheduleTicket && CoverageHead == other.CoverageHead && Operation == other.Operation &&
        Revision == other.Revision && LastCommit == other.LastCommit;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Observer);
        hash.Add(Format);
        hash.Add(Lifecycle);
        hash.Add(Generation);
        hash.Add(KeyDefinitionVersion);
        hash.Add(MapVersion);
        hash.Add(DeliveryCollections.Hash(Lanes));
        hash.Add(ActivationEpoch);
        hash.Add(ScheduleClock);
        hash.Add(ScheduleTicket);
        hash.Add(CoverageHead);
        hash.Add(Operation);
        hash.Add(Revision);
        hash.Add(LastCommit);

        return hash.ToHashCode();
    }
}
