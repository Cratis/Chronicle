// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

public partial class ReadModelScenario<TReadModel>
{
    readonly Dictionary<EventSourceId, NamedReadModelSeed<TReadModel>> _namedReadModelSeeds = [];
    ReadModelSeedKey? _seedKey;

    bool HasSeeds => _readModelSeeds.Count > 0 || _namedReadModelSeeds.Count > 0;

    /// <summary>
    /// Registers starting state under canonical mapped composite key part names.
    /// </summary>
    /// <param name="keyParts">Exactly the scalar parts mapped by the selected root projection.</param>
    /// <param name="instance">The starting state for this key.</param>
    /// <exception cref="InvalidReadModelSeedKey">The projection or supplied parts are invalid.</exception>
    /// <exception cref="ReadModelAlreadySeeded">The canonical key was already registered.</exception>
    public void CollectReadModelFor(IReadOnlyDictionary<string, object?> keyParts, TReadModel instance)
    {
        var contract = _seedKey ?? ReadModelSeedKey.Create(
            typeof(TReadModel),
            !_inlineProjection && FindReducerType(typeof(TReadModel)) is not null ? null : ProjectionDefinition(),
            _jsonSchemaGenerator);
        var (key, identity) = contract.Normalize(keyParts);
        if (_namedReadModelSeeds.ContainsKey(identity)) throw new ReadModelAlreadySeeded(typeof(TReadModel), identity);
        _namedReadModelSeeds.Add(identity, new(key, identity, instance));
        _seedKey = contract;
        EventStore().RegisterReadModelInstance(identity, instance);
        _processed = false;
    }

    /// <summary>
    /// Gets the folded result of a registered named seed, not an arbitrary composite-key query.
    /// </summary>
    /// <param name="keyParts">The registered seed's canonical mapped parts.</param>
    /// <returns>The seeded key's current state, or null when unregistered or removed.</returns>
    public TReadModel? InstanceForKey(IReadOnlyDictionary<string, object?> keyParts)
    {
        if (_seedKey is null) return null;
        var (_, identity) = _seedKey.Normalize(keyParts);
        if (!_namedReadModelSeeds.ContainsKey(identity)) return null;
        EnsureProcessed();
        return _instances.GetValueOrDefault(identity);
    }
}
