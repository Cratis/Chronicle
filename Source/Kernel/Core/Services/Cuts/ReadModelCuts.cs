// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Cuts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;
using ConceptProjectionDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;
using ConceptReadModelDefinition = Cratis.Chronicle.Concepts.ReadModels.ReadModelDefinition;
using ContractCuts = Cratis.Chronicle.Contracts.Cuts;
using StorageCuts = Cratis.Chronicle.Storage.Cuts;

namespace Cratis.Chronicle.Services.Cuts;

/// <summary>
/// Represents an implementation of <see cref="ContractCuts.IReadModelCuts"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting projected state to JSON.</param>
public class ReadModelCuts(IGrainFactory grainFactory, IStorage storage, IExpandoObjectConverter expandoObjectConverter) : ContractCuts.IReadModelCuts
{
    /// <inheritdoc/>
    public async Task<ContractCuts.ReadModelCutResponse> Capture(ContractCuts.ReadModelCutRequest request, CallContext context = default)
    {
        var eventStore = (EventStoreName)request.EventStore;
        var namespaceName = (EventStoreNamespaceName)request.Namespace;
        var cuts = request.Cuts.Select(_ => new EventSequenceCut((EventSequenceId)_.EventSequenceId, (EventSequenceNumber)_.Position)).ToArray();
        var selection = request.Selection.Select(_ => (ReadModelIdentifier)_).ToArray();

        var storageRequest = new StorageCuts.ReadModelCutRequest(eventStore, namespaceName, cuts, selection);
        var id = StorageCuts.ReadModelCutIdCalculator.Calculate(storageRequest);

        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(namespaceName);
        var cutStorage = namespaceStorage.ReadModelCuts;

        var existing = await cutStorage.GetManifest(id);
        if (existing is not null)
        {
            return ToResponse(existing);
        }

        var manifest = await CaptureManifest(id, eventStore, namespaceName, cuts, selection, namespaceStorage, cutStorage);
        return ToResponse(manifest);
    }

    static async Task<IReadOnlyList<AppendedEvent>> ReadEventsUpTo(IEventStoreNamespaceStorage namespaceStorage, EventSequenceId eventSequenceId, EventSequenceNumber position)
    {
        var events = new List<AppendedEvent>();
        var eventSequenceStorage = namespaceStorage.GetEventSequence(eventSequenceId);
        using var cursor = await eventSequenceStorage.GetRange(EventSequenceNumber.First, position);
        while (await cursor.MoveNext())
        {
            events.AddRange(cursor.Current);
        }

        return events;
    }

    static string ToCanonicalPayload(IEnumerable<ExpandoObject> instances, JsonSchema schema, IExpandoObjectConverter expandoObjectConverter)
    {
        var array = new JsonArray();
        foreach (var instance in instances)
        {
            array.Add(expandoObjectConverter.ToJsonObject(instance, schema));
        }

        return array.ToJsonString();
    }

    static ReadModelCutPayloadDigest ComputeDigest(string payloadJson) => new(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)));

    static ContractCuts.ReadModelCutResponse ToResponse(StorageCuts.ReadModelCutManifest manifest) => new()
    {
        Id = manifest.Id,
        Cuts = manifest.Cuts.Select(_ => new ContractCuts.EventSequenceCut
        {
            EventSequenceId = _.EventSequenceId,
            Position = _.Position
        }).ToArray(),
        Entries = manifest.Entries.Select(ToContractEntry).ToArray(),
        PublishedAt = manifest.PublishedAt
    };

    static ContractCuts.ReadModelCutEntry ToContractEntry(StorageCuts.ReadModelCutEntry entry) => new()
    {
        ReadModel = entry.ReadModel,
        Outcome = (ContractCuts.ReadModelCutOutcome)(int)entry.Outcome,
        Generation = entry.Generation?.Value,
        Digest = entry.Digest?.ToString(),
        FailureReason = entry.FailureReason
    };

    async Task<StorageCuts.ReadModelCutManifest> CaptureManifest(
        ReadModelCutId id,
        EventStoreName eventStore,
        EventStoreNamespaceName namespaceName,
        IReadOnlyCollection<EventSequenceCut> cuts,
        IReadOnlyCollection<ReadModelIdentifier> selection,
        IEventStoreNamespaceStorage namespaceStorage,
        StorageCuts.IReadModelCutStorage cutStorage)
    {
        var cutsByEventSequence = cuts.ToDictionary(_ => _.EventSequenceId);
        var readModelDefinitions = await storage.GetEventStore(eventStore).ReadModels.GetAll();
        var projectionDefinitions = await grainFactory.GetGrain<IProjectionsManager>(eventStore).GetProjectionDefinitions();

        var entries = new List<StorageCuts.ReadModelCutEntry>();
        foreach (var readModelId in selection)
        {
            var entry = await CaptureEntry(
                id,
                readModelId,
                cutsByEventSequence,
                readModelDefinitions,
                projectionDefinitions,
                eventStore,
                namespaceName,
                namespaceStorage,
                cutStorage);
            entries.Add(entry);
        }

        var manifest = new StorageCuts.ReadModelCutManifest(id, eventStore, namespaceName, cuts, entries, DateTimeOffset.UtcNow);
        await cutStorage.PublishManifest(manifest);
        return manifest;
    }

    async Task<StorageCuts.ReadModelCutEntry> CaptureEntry(
        ReadModelCutId id,
        ReadModelIdentifier readModelId,
        Dictionary<EventSequenceId, EventSequenceCut> cutsByEventSequence,
        IEnumerable<ConceptReadModelDefinition> readModelDefinitions,
        IEnumerable<ConceptProjectionDefinition> projectionDefinitions,
        EventStoreName eventStore,
        EventStoreNamespaceName namespaceName,
        IEventStoreNamespaceStorage namespaceStorage,
        StorageCuts.IReadModelCutStorage cutStorage)
    {
        var readModelDefinition = readModelDefinitions.FirstOrDefault(_ => _.Identifier == readModelId);
        if (readModelDefinition is null)
        {
            return new(readModelId, ReadModelCutOutcome.NotFound, null, null, "No read model definition was found for this identifier.");
        }

        if (readModelDefinition.ObserverType != ReadModelObserverType.Projection)
        {
            return new(
                readModelId,
                ReadModelCutOutcome.Unsupported,
                null,
                null,
                "The read model is not backed by a projection - reducer-backed read models run in the connected client process and cannot be recomputed by the kernel.");
        }

        var projectionDefinition = projectionDefinitions.FirstOrDefault(_ => _.ReadModel == readModelId);
        if (projectionDefinition is null)
        {
            return new(readModelId, ReadModelCutOutcome.NotFound, null, null, "No projection definition was found for this read model.");
        }

        if (!cutsByEventSequence.TryGetValue(projectionDefinition.EventSequenceId, out var cut))
        {
            return new(
                readModelId,
                ReadModelCutOutcome.Failed,
                null,
                null,
                $"The request did not include a cut for event sequence '{projectionDefinition.EventSequenceId}', which this read model's projection reads from.");
        }

        try
        {
            var events = await ReadEventsUpTo(namespaceStorage, projectionDefinition.EventSequenceId, cut.Position);
            var projection = grainFactory.GetGrain<IProjection>(new Concepts.Projections.ProjectionKey(projectionDefinition.Identifier, eventStore));
            var instances = await projection.Process(namespaceName, events);
            var schema = readModelDefinition.GetSchemaForLatestGeneration();
            var payloadJson = ToCanonicalPayload(instances, schema, expandoObjectConverter);
            var digest = ComputeDigest(payloadJson);

            await cutStorage.SavePayload(id, readModelId, payloadJson);
            return new(readModelId, ReadModelCutOutcome.Captured, readModelDefinition.LatestGeneration, digest, null);
        }
        catch (Exception ex)
        {
            return new(readModelId, ReadModelCutOutcome.Failed, null, null, ex.Message);
        }
    }
}
