// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents a versioned, immutable reserved coverage or operation payload.
/// </summary>
/// <param name="SchemaVersion">The non-negative schema version.</param>
/// <param name="Payload">The payload, copied at construction.</param>
public sealed record VersionedEnvelope(int SchemaVersion, ImmutableArray<byte> Payload)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VersionedEnvelope"/> class, copying source bytes.
    /// </summary>
    /// <param name="schemaVersion">The schema version.</param>
    /// <param name="payload">The source bytes.</param>
    public VersionedEnvelope(int schemaVersion, ReadOnlySpan<byte> payload) : this(schemaVersion, ImmutableArray.CreateRange(payload.ToArray()))
    {
    }

    /// <summary>
    /// Gets the immutable payload bytes.
    /// </summary>
    public ImmutableArray<byte> Payload { get; } = DeliveryCollections.Copy(Payload);

    /// <inheritdoc/>
    public bool Equals(VersionedEnvelope? other) => other is not null && SchemaVersion == other.SchemaVersion && Payload.SequenceEqual(other.Payload);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(SchemaVersion, DeliveryCollections.Hash(Payload));
}
