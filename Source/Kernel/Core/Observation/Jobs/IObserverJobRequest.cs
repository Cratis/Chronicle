// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

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
}
