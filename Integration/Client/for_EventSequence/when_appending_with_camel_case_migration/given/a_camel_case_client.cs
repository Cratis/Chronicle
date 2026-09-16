// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.EventSequences;
using Cratis.Serialization;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;

public class a_camel_case_client(ChronicleFixture fixture, string eventStoreName) : Specification(fixture)
{
#pragma warning disable CA2213 // The specification lifecycle invokes Destroy(), which disposes this client.
    ChronicleClient _client;
#pragma warning restore CA2213
    protected IEventStore _store;

    public IAppendResult Result;
    public JsonObject GenerationOne;
    public JsonObject GenerationTwo;
    public int StoredCount;
    public bool RegistrationSucceeded => _store.Registration.IsSuccess;
    public override IEnumerable<Type> EventTypes => [typeof(ContactRecordedV1), typeof(ContactRecorded)];
    public override IEnumerable<Type> EventTypeMigrators => [typeof(ContactRecordedMigration)];
    protected virtual INamingPolicy NamingPolicy => new CamelCaseNamingPolicy();

    async Task Establish()
    {
        _client = new ChronicleClient(
            new BorrowedConnection(EventStore.Connection),
            new ChronicleOptions { EnableEventTypeGenerationValidation = true },
            artifactsProvider: this,
            namingPolicy: NamingPolicy);
        _store = await _client.GetEventStore(eventStoreName, EventStore.Namespace);

        // The borrowed connection is already connected, so it need not emit another OnConnected event.
        await _store.RegisterAll();
    }

    protected async Task ReadStoredGenerations()
    {
        var services = ((IChronicleServicesAccessor)_store.Connection).Services;
        var stored = (await services.Sequences.AppendedEvents(new()
        {
            EventStore = _store.Name,
            Namespace = _store.Namespace,
            EventSequenceId = _store.EventLog.Id
        }).EnsureSuccess()).ToArray();
        StoredCount = stored.Length;
        var generations = stored.Single().GenerationalContent.ToDictionary(_ => _.Key, _ => _.Value);
        GenerationOne = JsonNode.Parse(generations[1])!.AsObject();
        GenerationTwo = JsonNode.Parse(generations[2])!.AsObject();
    }

    void Destroy()
    {
        _client?.EvictEventStores();
        _client?.Dispose();
    }
}
