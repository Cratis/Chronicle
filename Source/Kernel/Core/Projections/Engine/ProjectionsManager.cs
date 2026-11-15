// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents the implementation of <see cref="IProjectionsManager"/>.
/// </summary>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="grainFactory">The grain factory for authoritative read model definitions.</param>
/// <param name="meter">The engine cache meter.</param>
/// <param name="options">The kernel configuration.</param>
[Singleton]
public class ProjectionsManager(
    IProjectionFactory projectionFactory,
    IStorage storage,
    IGrainFactory grainFactory,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ProjectionsManager>? meter,
    IOptions<ChronicleOptions> options) : IProjectionsManager
{
    /// <summary>The silo-wide limit on cached compiled root graphs.</summary>
    internal const int CacheCapacity = 256;

    readonly ConcurrentDictionary<(EventStoreName Store, ProjectionId Id), RegisteredProjection> _registrations = new();
    readonly Channel<bool> _compilation = CreateCompilationBoundary();
    readonly Lock _publication = new();
    readonly CompiledProjectionCache _cache = new(CacheCapacity, new ProjectionsManagerMetrics(meter, options.Value.Telemetry.MetricNames).CachedGraphs);
    long _evictionRevision;

    /// <summary>Initializes a manager without telemetry for existing direct callers.</summary>
    /// <param name="projectionFactory">The projection factory.</param>
    /// <param name="storage">The storage.</param>
    /// <param name="grainFactory">The grain factory.</param>
    public ProjectionsManager(IProjectionFactory projectionFactory, IStorage storage, IGrainFactory grainFactory)
        : this(projectionFactory, storage, grainFactory, null, Options.Create(new ChronicleOptions()))
    {
    }

    /// <summary>Gets the number of compiled root graphs retained by the cache.</summary>
    internal int CachedGraphCount => _cache.Count;

    /// <inheritdoc/>
    public async Task<Result<ProjectionRegistrationError>> Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions, IEnumerable<ReadModelDefinition> readModelDefinitions, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        await _compilation.Reader.ReadAsync();
        try
        {
            var readModels = readModelDefinitions.ToDictionary(readModel => readModel.Identifier);
            var validationNamespace = namespaces.FirstOrDefault();
            var failures = new Dictionary<ProjectionId, Exception>();
            foreach (var definition in definitions)
            {
                try
                {
                    await RegisterDefinition(eventStore, definition, readModels, validationNamespace);
                }
                catch (Exception exception)
                {
                    failures[definition.Identifier] = exception as ProjectionDefinitionRegistrationFailed ??
                        new ProjectionDefinitionRegistrationFailed(definition.Identifier, exception);
                }
            }

            return failures.Count == 0
                ? Result<ProjectionRegistrationError>.Success()
                : Result.Failed(new ProjectionRegistrationError(failures));
        }
        finally
        {
            _compilation.Writer.TryWrite(true);
        }
    }

    /// <inheritdoc/>
    public async Task AddNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace, IEnumerable<ReadModelDefinition> readModelDefinitions)
    {
        // Namespaces are an unbounded keyspace. Keep no per-namespace entry until a consumer needs a graph.
        await _compilation.Reader.ReadAsync();
        try
        {
            var readModels = readModelDefinitions.ToDictionary(readModel => readModel.Identifier);
            foreach (var registration in _registrations.Where(entry => entry.Key.Store == eventStore).Select(entry => entry.Value))
            {
                _ = readModels[registration.Definition.ReadModel];
            }
        }
        finally
        {
            _compilation.Writer.TryWrite(true);
        }
    }

    /// <inheritdoc/>
    public bool TryGet(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id, [NotNullWhen(true)] out IProjection? projection) =>
        _cache.TryGet(eventStore, @namespace, id, out projection);

    /// <inheritdoc/>
    public async Task<ProjectionDefinition?> GetDefinition(EventStoreName eventStore, ProjectionId id)
    {
        if (_registrations.TryGetValue((eventStore, id), out var registration)) return registration.Definition;
        var definitions = storage.GetEventStore(eventStore).Projections;
        return await definitions.Has(id) ? await definitions.Get(id) : null;
    }

    /// <inheritdoc/>
    public bool TryGetDefinitionFingerprint(EventStoreName eventStore, ProjectionId id, [NotNullWhen(true)] out string? fingerprint)
    {
        fingerprint = _registrations.TryGetValue((eventStore, id), out var registration) ? registration.Fingerprint : null;
        return fingerprint is not null;
    }

    /// <inheritdoc/>
    public async Task<IProjection?> GetCurrent(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id)
    {
        // A single compilation boundary prevents duplicate graphs and stale publication during registration.
        await _compilation.Reader.ReadAsync();
        try
        {
            return await GetOrCompile(eventStore, @namespace, id);
        }
        finally
        {
            _compilation.Writer.TryWrite(true);
        }
    }

    /// <inheritdoc/>
    public void Evict(EventStoreName eventStore, ProjectionId id)
    {
        lock (_publication)
        {
            _evictionRevision++;
            _registrations.TryRemove((eventStore, id), out _);
            _cache.Remove(eventStore, id);
        }
    }

    static Channel<bool> CreateCompilationBoundary()
    {
        // A single reusable token serializes async compilation without introducing disposal requirements
        // on existing callers that construct the manager directly.
        var boundary = Channel.CreateBounded<bool>(1);
        boundary.Writer.TryWrite(true);
        return boundary;
    }

    static string ReadModelFingerprintFor(ReadModelDefinition definition)
    {
        var content = JsonSerializer.Serialize(definition with { Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema>() }, ProjectionFactory.FingerprintOptions) +
            string.Join(';', definition.Schemas.OrderBy(entry => entry.Key.Value).Select(entry => $"{entry.Key.Value}:{entry.Value.ToJson()}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    async Task RegisterDefinition(EventStoreName eventStore, ProjectionDefinition definition, IDictionary<ReadModelIdentifier, ReadModelDefinition> readModels, EventStoreNamespaceName? validationNamespace)
    {
        ProjectionGenerationDeliveryValidation.Validate(definition);
        if (!readModels.TryGetValue(definition.ReadModel, out var readModel))
        {
            var available = string.Join(", ", readModels.Keys.Select(identifier => $"'{identifier.Value}'"));
            throw new InvalidOperationException($"ReadModelDefinition with Identifier '{definition.ReadModel.Value}' not found. Available: [{available}]");
        }

        var fingerprint = ProjectionFactory.DefinitionFingerprintFor(definition);
        var readModelFingerprint = ReadModelFingerprintFor(readModel);
        var revision = _evictionRevision;
        if (_registrations.TryGetValue((eventStore, definition.Identifier), out var previous) &&
            previous.Fingerprint == fingerprint && previous.ReadModelFingerprint == readModelFingerprint)
        {
            return;
        }

        // Registration reports invalid expressions/schemas to its caller. Validate once, not once per tenant.
        IProjection? validated = null;
        if (validationNamespace is not null)
        {
            var schemas = await storage.GetEventStore(eventStore).EventTypes.GetLatestForAllEventTypes();
            validated = await projectionFactory.Create(eventStore, validationNamespace, definition, readModel, schemas);
        }

        lock (_publication)
        {
            if (revision != _evictionRevision) return;
            _cache.Remove(eventStore, definition.Identifier);
            _registrations[(eventStore, definition.Identifier)] = new(definition, fingerprint, readModelFingerprint);
            if (validated is not null) _cache.Store(eventStore, validationNamespace!, definition.Identifier, validated);
        }
    }

    async Task<IProjection?> GetOrCompile(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var revision = _evictionRevision;
        if (!await eventStoreStorage.Projections.Has(id))
        {
            Evict(eventStore, id);
            return null;
        }

        var definition = await eventStoreStorage.Projections.Get(id);
        var fingerprint = ProjectionFactory.DefinitionFingerprintFor(definition);
        if (TryGet(eventStore, @namespace, id, out var current) && current.DefinitionFingerprint == fingerprint) return current;

        var readModel = await grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(definition.ReadModel, eventStore)).GetDefinition();
        var schemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
        var projection = await projectionFactory.Create(eventStore, @namespace, definition, readModel, schemas);
        lock (_publication)
        {
            if (revision != _evictionRevision) return projection;
            if (_registrations.TryGetValue((eventStore, id), out var previous) && previous.Fingerprint != fingerprint) _cache.Remove(eventStore, id);
            _registrations[(eventStore, id)] = new(definition, fingerprint, ReadModelFingerprintFor(readModel));
            _cache.Store(eventStore, @namespace, id, projection);
        }

        return projection;
    }

    sealed record RegisteredProjection(ProjectionDefinition Definition, string Fingerprint, string ReadModelFingerprint);
}
