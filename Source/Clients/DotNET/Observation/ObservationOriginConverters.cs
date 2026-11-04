// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Converts between the contract and client representations of <see cref="ObservationOrigin"/>.
/// </summary>
internal static class ObservationOriginConverters
{
    /// <summary>
    /// Converts a contract origin to the client <see cref="ObservationOrigin"/>.
    /// </summary>
    /// <param name="origin">The contract origin.</param>
    /// <returns>The client <see cref="ObservationOrigin"/>.</returns>
    public static ObservationOrigin ToClient(this Contracts.Observation.ObservationOrigin origin) =>
        origin switch
        {
            Contracts.Observation.ObservationOrigin.Live => ObservationOrigin.Live,
            Contracts.Observation.ObservationOrigin.CatchingUp => ObservationOrigin.CatchUp,
            Contracts.Observation.ObservationOrigin.Replaying => ObservationOrigin.Replay,
            Contracts.Observation.ObservationOrigin.Retrying => ObservationOrigin.Retry,
            _ => ObservationOrigin.Unknown
        };
}
