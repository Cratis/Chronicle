// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when a digest is requested for a digest version that is not supported.
/// </summary>
/// <param name="version">The unsupported version.</param>
public class UnsupportedEventSequenceMutationDigestVersion(EventSequenceMutationDigestVersion version) : Exception($"The event sequence mutation digest version '{(int)version}' is not supported.")
{
    /// <summary>
    /// Gets the unsupported version.
    /// </summary>
    public EventSequenceMutationDigestVersion Version { get; } = version;
}
