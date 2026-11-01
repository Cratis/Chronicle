// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Caches only serialized wire definitions, never projection instances or mutable builders.
/// </summary>
internal sealed class CachedFluentProjectionDefinitionsSource : IFluentProjectionDefinitionsSource
{
    readonly object _sync = new();
    readonly Dictionary<EventGenerationDelivery, Definitions> _byDelivery = [];

    /// <inheritdoc/>
    public (Dictionary<Type, ProjectionDefinition> Definitions, Dictionary<Type, Exception> Failures, Dictionary<Type, string> ContainerNames) Get(
        EventGenerationDelivery delivery,
        Func<(Dictionary<Type, ProjectionDefinition> Definitions, Dictionary<Type, Exception> Failures, Dictionary<Type, string> ContainerNames)> discover)
    {
        lock (_sync)
        {
            if (!_byDelivery.TryGetValue(delivery, out var cached))
            {
                var result = discover();
                if (result.Failures.Count > 0) return result;
                cached = new(
                    result.Definitions.ToFrozenDictionary(pair => pair.Key, pair => Serialize(pair.Value)),
                    result.ContainerNames.ToFrozenDictionary());
                _byDelivery.Add(delivery, cached);
            }

            return (
                cached.Wire.ToDictionary(pair => pair.Key, pair => Deserialize(pair.Value)),
                [],
                cached.Names.ToDictionary());
        }
    }

    static ImmutableArray<byte> Serialize(ProjectionDefinition definition)
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, definition);
        return stream.ToArray().ToImmutableArray();
    }

    static ProjectionDefinition Deserialize(ImmutableArray<byte> bytes)
    {
        using var stream = new MemoryStream(bytes.ToArray());
        return ProtoBuf.Serializer.Deserialize<ProjectionDefinition>(stream);
    }

    sealed record Definitions(FrozenDictionary<Type, ImmutableArray<byte>> Wire, FrozenDictionary<Type, string> Names);
}
