// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Identifies the equivalent catalogs whose application metadata can be shared between testing event stores.
/// </summary>
/// <remarks>
/// The key holds the content of the catalogs rather than the identity of the provider, so a provider that changes
/// later gets a new entry instead of stale metadata.
/// </remarks>
internal sealed class ApplicationMetadataKey : IEquatable<ApplicationMetadataKey>
{
    readonly JsonSchemaGenerator _generator;
    readonly Type _namingPolicy;
    readonly string _eventStoreName;
    readonly Type[][] _catalogs;
    readonly int _hash;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMetadataKey"/> class.
    /// </summary>
    /// <param name="generator">The shared schema generator, compared by reference.</param>
    /// <param name="namingPolicy">The naming policy.</param>
    /// <param name="eventStoreName">The name of the event store the metadata is discovered for.</param>
    /// <param name="artifacts">The artifacts the metadata is discovered from.</param>
    internal ApplicationMetadataKey(JsonSchemaGenerator generator, INamingPolicy namingPolicy, string eventStoreName, IClientArtifactsProvider artifacts)
    {
        _generator = generator;
        _namingPolicy = namingPolicy.GetType();
        _eventStoreName = eventStoreName;
        _catalogs =
        [
            [.. artifacts.EventTypes],
            [.. artifacts.ModelBoundProjections],
            [.. artifacts.EventTypeMigrators]
        ];

        var hash = default(HashCode);
        hash.Add(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_generator));
        hash.Add(_namingPolicy);
        hash.Add(_eventStoreName);
        foreach (var catalog in _catalogs)
        {
            hash.Add(catalog.Length);
            foreach (var type in catalog)
            {
                hash.Add(type);
            }
        }

        _hash = hash.ToHashCode();
    }

    /// <inheritdoc/>
    public bool Equals(ApplicationMetadataKey? other) =>
        other is not null &&
        _hash == other._hash &&
        ReferenceEquals(_generator, other._generator) &&
        _namingPolicy == other._namingPolicy &&
        _eventStoreName == other._eventStoreName &&
        _catalogs.Zip(other._catalogs).All(pair => pair.First.SequenceEqual(pair.Second));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ApplicationMetadataKey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _hash;
}
