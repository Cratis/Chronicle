// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Shares structural schema generation for equivalent metadata provider catalogs.
/// </summary>
internal static class SchemaGeneratorsForTesting
{
    static readonly ConcurrentDictionary<CacheKey, Lazy<JsonSchemaGenerator>> _generators = new();

    /// <summary>
    /// Gets or creates a generator for the naming policy and metadata provider catalogs.
    /// </summary>
    /// <param name="namingPolicy">The schema naming policy.</param>
    /// <param name="artifacts">The metadata provider catalogs.</param>
    /// <param name="create">The generator factory.</param>
    /// <returns>The shared schema generator.</returns>
    internal static JsonSchemaGenerator GetOrCreate(INamingPolicy namingPolicy, IClientArtifactsProvider artifacts, Func<JsonSchemaGenerator> create)
    {
        var key = new CacheKey(namingPolicy.GetType(), artifacts);
        var generator = _generators.GetOrAdd(key, static (_, factory) => new Lazy<JsonSchemaGenerator>(factory), create);
        try
        {
            return generator.Value;
        }
        catch
        {
            _generators.TryRemove(new KeyValuePair<CacheKey, Lazy<JsonSchemaGenerator>>(key, generator));
            throw;
        }
    }

    sealed class CacheKey(Type namingPolicy, IClientArtifactsProvider artifacts) : IEquatable<CacheKey>
    {
        readonly Type _namingPolicy = namingPolicy;
        readonly Type[][] _providers =
        [
            artifacts.ComplianceForTypesProviders.ToArray(),
            artifacts.ComplianceForPropertiesProviders.ToArray(),
            artifacts.SecurityForTypesProviders.ToArray(),
            artifacts.SecurityForPropertiesProviders.ToArray()
        ];

        public bool Equals(CacheKey? other) => other is not null && _namingPolicy == other._namingPolicy &&
            _providers.Zip(other._providers).All(pair => pair.First.SequenceEqual(pair.Second));

        public override bool Equals(object? obj) => obj is CacheKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = default(HashCode);
            hash.Add(_namingPolicy);
            foreach (var providers in _providers)
            {
                hash.Add(providers.Length);
                foreach (var provider in providers)
                {
                    hash.Add(provider);
                }
            }
            return hash.ToHashCode();
        }
    }
}
