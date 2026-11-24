// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents what comparing the cursors of observers with the tails of what they observe found.
/// </summary>
/// <param name="Behind">The observers with at least one event waiting for them.</param>
/// <param name="Unverified">The observers that could not be compared because their cursor is unknown; treat them as behind.</param>
/// <param name="UpToDate">The number of observers with nothing waiting.</param>
public record ObserverBehindCheckResult(IReadOnlyList<ObserverKey> Behind, IReadOnlyList<ObserverKey> Unverified, int UpToDate)
{
    /// <summary>
    /// Gets the observers that need to be woken: the ones that are behind and the ones that could not be compared.
    /// </summary>
    public IEnumerable<ObserverKey> ToWake => Behind.Concat(Unverified);
}
