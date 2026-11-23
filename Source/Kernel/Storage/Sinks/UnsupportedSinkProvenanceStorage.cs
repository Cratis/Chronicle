// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Preserves ordinary custom sinks while refusing protected producers without durable provenance support.
/// </summary>
internal sealed class UnsupportedSinkProvenanceStorage : ISinkProvenanceStorage
{
    /// <summary>
    /// Gets the stateless instance.
    /// </summary>
    internal static readonly UnsupportedSinkProvenanceStorage Instance = new();

    /// <inheritdoc/>
    public Task<bool> IsQuarantined() => Task.FromResult(false);

    /// <inheritdoc/>
    public Task MarkQuarantined() => throw new NotSupportedException("Read-model storage is not available.");
}
