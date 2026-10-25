// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Defines the per-store claims on observer identities, so a generation and a definition can never both own one.
/// </summary>
public interface IObserverIdentityClaims
{
    /// <summary>
    /// Atomically claims an observer identity when it is unclaimed.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="claim">The claim to make.</param>
    /// <returns>The stored claim, which is the given claim when it won.</returns>
    Task<ObserverIdentityClaim> Claim(EventStoreName store, ObserverIdentityClaim claim);

    /// <summary>
    /// Gets the claim on an observer identity.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="observerId">The observer identity.</param>
    /// <returns>The claim, or null when unclaimed.</returns>
    Task<ObserverIdentityClaim?> Get(EventStoreName store, ObserverId observerId);

    /// <summary>
    /// Releases a claim when the stored claim equals the given claim.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="claim">The claim to release.</param>
    /// <returns>True when the claim was released.</returns>
    Task<bool> Release(EventStoreName store, ObserverIdentityClaim claim);

    /// <summary>
    /// Gets whether a per-store observer definition exists.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="observerId">The observer identity.</param>
    /// <returns>True when a definition exists.</returns>
    Task<bool> HasDefinition(EventStoreName store, ObserverId observerId);
}
