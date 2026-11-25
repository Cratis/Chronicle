// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Refuses admission when a database provider cannot determine durable provenance.
/// </summary>
internal sealed class UnsupportedSinkProvenanceStorage : ISinkProvenanceStorage
{
    /// <summary>
    /// Gets the stateless instance.
    /// </summary>
    internal static readonly UnsupportedSinkProvenanceStorage Instance = new();

    /// <inheritdoc/>
    public Task<bool> IsQuarantined() => Task.FromResult(true);

    /// <inheritdoc/>
    public Task MarkQuarantined() => throw new NotSupportedException("Read-model storage is not available.");
}
