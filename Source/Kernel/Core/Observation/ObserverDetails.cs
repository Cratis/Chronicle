// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents observer details to be used when performing observer service operations.
/// </summary>
/// <param name="Key">The <see cref="ObserverKey"/> for a specific instance.</param>
/// <param name="Type">The <see cref="ObserverType"/> for the observer.</param>
public record ObserverDetails(ObserverKey Key, ObserverType Type)
{
    /// <summary>
    /// Gets whether replay initialization failed and the rebuild must be abandoned rather than promoted.
    /// </summary>
    public bool ReplayAborted { get; init; }

    /// <summary>
    /// Gets whether a successful replay handled events, proving that an empty result is an intentional rebuild.
    /// </summary>
    public bool ReplaySucceededWithEvents { get; init; }

    /// <summary>
    /// Gets whether another silo has already finalized the replay and this silo only needs to leave replay mode.
    /// </summary>
    public bool ReplayAlreadyFinalized { get; init; }
}
