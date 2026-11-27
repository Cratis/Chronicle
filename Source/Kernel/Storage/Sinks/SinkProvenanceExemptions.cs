// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Enumerates sinks that cannot expose stored read-model database payloads.
/// </summary>
public static class SinkProvenanceExemptions
{
    static readonly Dictionary<SinkTypeId, bool> _types = new()
    {
        // The null/preview sink discards writes; public reads can only fold events, never read database payloads.
        [SinkTypeId.None] = true,

        // Event publication stores events, not read-model rows, and public read-model reads are refused.
        [WellKnownSinkTypes.EventSequence] = false
    };

    /// <summary>
    /// Gets whether the type has no physical read-model database to mark.
    /// </summary>
    /// <param name="type">The sink type.</param>
    /// <returns>Whether the type is an explicitly enumerated non-database sink.</returns>
    public static bool IsNonDatabase(SinkTypeId type) => _types.ContainsKey(type);

    /// <summary>
    /// Gets whether public reads are meaningful for the sink type.
    /// </summary>
    /// <param name="type">The sink type.</param>
    /// <returns>Whether public read admission can continue.</returns>
    public static bool AllowsPublicReads(SinkTypeId type) => !_types.TryGetValue(type, out var allowed) || allowed;

    /// <summary>
    /// Resolves non-database exemptions while refusing unsupported database providers.
    /// </summary>
    /// <param name="type">The sink type.</param>
    /// <returns>The applicable provenance storage.</returns>
    internal static ISinkProvenanceStorage For(SinkTypeId type) =>
        IsNonDatabase(type) ? NonDatabaseProvenance.Instance : UnsupportedSinkProvenanceStorage.Instance;

    sealed class NonDatabaseProvenance : ISinkProvenanceStorage
    {
        /// <summary>
        /// The stateless non-database provenance instance.
        /// </summary>
        internal static readonly NonDatabaseProvenance Instance = new();

        /// <inheritdoc/>
        public Task<SinkProvenanceLease> GetLease() => Task.FromResult(SinkProvenanceLease.Exempt);

        /// <inheritdoc/>
        public Task<bool> IsQuarantined() => Task.FromResult(false);

        /// <inheritdoc/>
        public Task MarkQuarantined() => throw new NotSupportedException("Read-model storage is not available.");
    }
}
