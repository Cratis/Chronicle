// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Chronicle.Storage.Observation.Registrations;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Registrations;

/// <summary>
/// Stores isolated snapshots of registrations with atomic compare-and-set semantics.
/// </summary>
/// <param name="serializerOptions">Options for serializing complete definitions.</param>
public class ObserverRegistrationsStorage(JsonSerializerOptions? serializerOptions = null) : IObserverRegistrationsStorage
{
    readonly object _gate = new();
    readonly JsonSerializerOptions _options = serializerOptions ?? new() { Converters = { new ConceptAsJsonConverterFactory() } };
    readonly Dictionary<(ObserverId, EventSequenceId), (ulong Revision, bool IsDeleted, string Data)> _registrations = [];
    readonly Dictionary<ProgramIdentifier, ObserverOwnerSettings> _settings = [];
    ObserverGenerationFloor? _floor;

    /// <inheritdoc/>
    public Task<ObserverRegistration?> Get(ObserverId observerId, EventSequenceId eventSequenceId)
    {
        lock (_gate)
        {
            return Task.FromResult(_registrations.TryGetValue((observerId, eventSequenceId), out var entry) && !entry.IsDeleted ? Read(entry.Data) : null);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverRegistration>> GetActive() => Find(registration => registration.IsActive);

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverRegistration>> GetByOwner(ProgramIdentifier owner) => Find(registration => registration.Owner == owner);

    /// <inheritdoc/>
    public Task<bool> TrySave(ObserverRegistration registration, ObserverRegistrationRevision expectedRevision)
    {
        lock (_gate)
        {
            var key = (registration.ObserverId, registration.EventSequenceId);
            var revision = _registrations.TryGetValue(key, out var entry) ? entry.Revision : 0;
            if (!IsNext(registration.Revision.Value, expectedRevision.Value) || revision != expectedRevision.Value)
            {
                return Task.FromResult(false);
            }

            _registrations[key] = (registration.Revision.Value, false, JsonSerializer.Serialize(registration, _options));
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryDelete(ObserverId observerId, EventSequenceId eventSequenceId, ObserverRegistrationRevision expectedRevision)
    {
        lock (_gate)
        {
            var key = (observerId, eventSequenceId);
            if (!_registrations.TryGetValue(key, out var entry) || entry.IsDeleted)
            {
                return Task.FromResult(false);
            }

            if (entry.Revision != expectedRevision.Value || entry.Revision == ulong.MaxValue)
            {
                return Task.FromResult(false);
            }

            _registrations[key] = (entry.Revision + 1, true, string.Empty);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<ObserverGenerationFloor?> GetGenerationFloor()
    {
        lock (_gate)
        {
            return Task.FromResult(_floor);
        }
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveGenerationFloor(ObserverGenerationFloor floor, ulong expectedVersion)
    {
        lock (_gate)
        {
            var current = _floor ?? new(0, 0);
            var matchesVersion = current.Version == expectedVersion && IsNext(floor.Version, expectedVersion);
            if (!matchesVersion || floor.Generation < current.Generation)
            {
                return Task.FromResult(false);
            }

            _floor = floor;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<ObserverOwnerSettings?> GetOwnerSettings(ProgramIdentifier owner)
    {
        lock (_gate)
        {
            return Task.FromResult(_settings.GetValueOrDefault(owner));
        }
    }

    /// <inheritdoc/>
    public Task<bool> TrySaveOwnerSettings(ObserverOwnerSettings settings, ulong expectedRevision)
    {
        lock (_gate)
        {
            var revision = _settings.GetValueOrDefault(settings.Owner)?.Revision ?? 0;
            var matchesRevision = revision == expectedRevision && IsNext(settings.Revision, expectedRevision);
            if (!settings.HasValidTimeout || !matchesRevision)
            {
                return Task.FromResult(false);
            }

            _settings[settings.Owner] = settings;
            return Task.FromResult(true);
        }
    }

    static bool IsNext(ulong revision, ulong expected) => expected < ulong.MaxValue && revision == expected + 1;

    Task<IEnumerable<ObserverRegistration>> Find(Func<ObserverRegistration, bool> predicate)
    {
        lock (_gate)
        {
            return Task.FromResult<IEnumerable<ObserverRegistration>>(_registrations.Values.Where(entry => !entry.IsDeleted).Select(entry => Read(entry.Data)).Where(predicate).ToArray());
        }
    }

    ObserverRegistration Read(string data) => JsonSerializer.Deserialize<ObserverRegistration>(data, _options)!;
}
