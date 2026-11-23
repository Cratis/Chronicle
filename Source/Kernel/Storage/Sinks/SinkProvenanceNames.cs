// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Holds reserved database-local provenance names and validates ordinary sink destinations.
/// </summary>
public static class SinkProvenanceNames
{
    /// <summary>
    /// The Chronicle-owned singleton marker collection or table.
    /// </summary>
    public const string Metadata = "__cratis_sink_provenance";

    /// <summary>
    /// Rejects metadata names, including names a provider compares without case sensitivity.
    /// </summary>
    /// <param name="destination">The ordinary destination.</param>
    /// <exception cref="InvalidOperationException">The destination is reserved for provenance metadata.</exception>
    public static void EnsureNotReserved(string destination)
    {
        if (string.Equals(destination, Metadata, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Read-model storage is not available.");
        }
    }
}
