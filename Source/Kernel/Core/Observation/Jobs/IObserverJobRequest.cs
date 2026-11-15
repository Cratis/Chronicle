// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Defines the basis for a request to an observer.
/// </summary>
public interface IObserverJobRequest : IJobRequest
{
    /// <summary>
    /// Gets the <see cref="ObserverKey"/> for the request.
    /// </summary>
    ObserverKey ObserverKey { get; }

    /// <summary>
    /// Gets the <see cref="ObserverKey"/> for the request.
    /// </summary>
    ObserverType ObserverType { get; }

    /// <summary>
    /// Gets the <see cref="ObserverReaderScope"/> the request reads with. Null on a request persisted before requests carried a scope.
    /// </summary>
    ObserverReaderScope? ReaderScope { get; }

    /// <summary>
    /// Gets the revision of the observer's membership the request was made for. Reserved by decision 0011: nothing sets or reads it yet.
    /// </summary>
    ulong? Revision { get; }

    /// <summary>
    /// Gets the epoch of the observer's ownership claim the job was started under. Null on a request that is not checked
    /// against a claim, including every request persisted before requests carried an epoch; such a request is admitted.
    /// </summary>
    /// <remarks>
    /// The observer grants and persists its claim before it starts the job, and a job whose epoch is no longer the
    /// persisted claim's for its scope is refused by <see cref="ObserverOwnershipEpochRule"/> when it starts or resumes.
    /// Kept apart from <see cref="Revision"/>, which decision 0011 reserves and refuses when missing.
    /// </remarks>
    ulong? OwnershipEpoch { get; }
}
