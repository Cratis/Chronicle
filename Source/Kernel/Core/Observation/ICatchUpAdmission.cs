// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the admission of catch-up jobs: how many may run at once in one event store namespace.
/// </summary>
/// <remarks>
/// When a client application returns, every observer it owns needs a catch-up job at the same moment. Admitting them all
/// turns a reconnect into a burst of kernel work that the registrations of the same clients then queue behind.
/// </remarks>
public interface ICatchUpAdmission
{
    /// <summary>
    /// Asks to start a catch-up job for an observer.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer that needs a catch-up job.</param>
    /// <param name="limit">The most catch-up jobs that may run at once in the namespace; zero or less lifts the limit.</param>
    /// <param name="eventsBehind">How many events the observer is behind the tail of its event sequence; the observer furthest behind is admitted first.</param>
    /// <returns>
    /// A lease to dispose once the job has been started (or not), or null when the namespace is at its limit, or the free
    /// places are for observers further behind, and the observer should try again later. An observer that already has an
    /// outstanding catch-up job is always admitted.
    /// </returns>
    Task<IDisposable?> TryAdmit(ObserverKey observerKey, int limit, ulong eventsBehind);
}
