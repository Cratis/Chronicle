// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Discovers model-bound definitions once and hands every store its own deserialized copy.
/// </summary>
/// <remarks>
/// A definition is mutable and has generation delivery applied to it afterwards, so it is cached in its wire form and
/// never shared. Failures are kept with the definitions: they are the outcome of the same discovery.
/// </remarks>
internal sealed class CachedModelBoundProjectionsSource : IModelBoundProjectionsSource
{
    readonly object _sync = new();
    Dictionary<Type, byte[]>? _definitions;
    Dictionary<Type, Exception>? _failures;

    /// <inheritdoc/>
    public (IDictionary<Type, ProjectionDefinition> Definitions, IDictionary<Type, Exception> Failures) Discover(
        IClientArtifactsProvider clientArtifactsProvider,
        INamingPolicy namingPolicy,
        IEventTypes eventTypes,
        ILogger<Projections.Projections> logger,
        string? currentEventStoreName)
    {
        Dictionary<Type, byte[]> definitions;
        Dictionary<Type, Exception> knownFailures;
        lock (_sync)
        {
            if (_definitions is null || _failures is null)
            {
                var (discovered, failures) = ModelBoundProjectionsSource.Default.Discover(clientArtifactsProvider, namingPolicy, eventTypes, logger, currentEventStoreName);
                _definitions = discovered.ToDictionary(_ => _.Key, _ => Serialize(_.Value));
                _failures = new Dictionary<Type, Exception>(failures);
            }

            definitions = _definitions;
            knownFailures = _failures;
        }

        // The cached dictionaries are never mutated once assigned, so every store copies them without the lock.
        return (definitions.ToDictionary(_ => _.Key, _ => Deserialize(_.Value)), new Dictionary<Type, Exception>(knownFailures));
    }

    static byte[] Serialize(ProjectionDefinition definition)
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, definition);
        return stream.ToArray();
    }

    static ProjectionDefinition Deserialize(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return ProtoBuf.Serializer.Deserialize<ProjectionDefinition>(stream);
    }
}
