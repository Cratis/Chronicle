// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents an in-process, in-memory implementation of <see cref="IReadModels"/> (contracts) for testing scenarios.
/// </summary>
/// <remarks>
/// Stores pre-seeded read model instances in a dictionary keyed by read model identifier and key.
/// <see cref="GetInstanceByKey"/> returns the pre-seeded JSON for a given identifier and key.
/// All other contract methods throw <see cref="NotSupportedException"/> because they require server infrastructure.
/// </remarks>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> used to serialize pre-seeded instances.</param>
internal sealed class InProcessReadModelsService(JsonSerializerOptions jsonSerializerOptions) : IReadModels
{
    readonly Dictionary<(string Identifier, string Key), string> _instances = [];

    /// <summary>
    /// Registers a pre-seeded read model instance so that a subsequent <see cref="GetInstanceByKey"/> call can return it.
    /// </summary>
    /// <typeparam name="TReadModel">The type of read model to register.</typeparam>
    /// <param name="eventSourceId">The event source identifier to associate with the read model.</param>
    /// <param name="instance">The read model instance to pre-seed.</param>
    internal void Register<TReadModel>(EventSourceId eventSourceId, TReadModel instance)
        where TReadModel : class
    {
        var identifier = typeof(TReadModel).GetReadModelIdentifier();
        var json = JsonSerializer.Serialize(instance, jsonSerializerOptions);
        _instances[(identifier, eventSourceId.Value)] = json;
    }

    /// <inheritdoc/>
    public Task<GetInstanceByKeyResponse> GetInstanceByKey(GetInstanceByKeyRequest request, CallContext context = default)
    {
        var key = (request.ReadModelIdentifier, request.ReadModelKey);
        if (_instances.TryGetValue(key, out var json))
        {
            return Task.FromResult(new GetInstanceByKeyResponse { ReadModel = json });
        }

        throw new InvalidOperationException(
            $"No read model '{request.ReadModelIdentifier}' has been registered for key '{request.ReadModelKey}'. " +
            "Use ReadModel() on the Given builder to register an instance before exercising production code that calls GetInstanceById.");
    }

    /// <inheritdoc/>
    public Task RegisterMany(RegisterManyRequest request, CallContext context = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task RegisterSingle(RegisterSingleRequest request, CallContext context = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task UpdateDefinition(UpdateDefinitionRequest request, CallContext context = default) =>
        throw new NotSupportedException("UpdateDefinition is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<GetDefinitionsResponse> GetDefinitions(GetDefinitionsRequest request, CallContext context = default) =>
        throw new NotSupportedException("GetDefinitions is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<GetOccurrencesResponse> GetOccurrences(GetOccurrencesRequest request, CallContext context = default) =>
        throw new NotSupportedException("GetOccurrences is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, CallContext context = default) =>
        throw new NotSupportedException("GetInstances is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<GetSnapshotsByKeyResponse> GetSnapshotsByKey(GetSnapshotsByKeyRequest request, CallContext context = default) =>
        throw new NotSupportedException("GetSnapshotsByKey is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task<GetAllInstancesResponse> GetAllInstances(GetAllInstancesRequest request, CallContext context = default) =>
        throw new NotSupportedException("GetAllInstances is not supported in test scenarios.");

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset> Watch(WatchRequest request, CallContext context = default) =>
        throw new NotSupportedException("Watch is not supported in test scenarios.");

    /// <inheritdoc/>
    public Task DehydrateSession(DehydrateSessionRequest request, CallContext context = default) =>
        throw new NotSupportedException("DehydrateSession is not supported in test scenarios.");
}
