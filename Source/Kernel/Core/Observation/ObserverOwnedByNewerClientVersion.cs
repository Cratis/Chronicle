// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Exception that defers a connected registration while a newer client serves another sequence.
/// </summary>
/// <param name="key">The requested observer key.</param>
/// <param name="ownerSequence">The active owner's sequence.</param>
/// <param name="ownerVersion">The active owner's version.</param>
/// <param name="incomingVersion">The registering client's version.</param>
[GenerateSerializer]
public class ObserverOwnedByNewerClientVersion(ObserverKey key, EventSequenceId ownerSequence, string ownerVersion, string incomingVersion)
    : Exception($"Observer '{key}' is served on '{ownerSequence}' by client '{ownerVersion}', newer than '{incomingVersion}'.")
{
    /// <summary>
    /// Gets the active owner's sequence.
    /// </summary>
    [Id(0)]
    public EventSequenceId OwnerSequence { get; } = ownerSequence;

    /// <summary>
    /// Gets the active owner's version.
    /// </summary>
    [Id(1)]
    public string OwnerVersion { get; } = ownerVersion;

    /// <summary>
    /// Gets the registering client's version.
    /// </summary>
    [Id(2)]
    public string IncomingVersion { get; } = incomingVersion;
}
