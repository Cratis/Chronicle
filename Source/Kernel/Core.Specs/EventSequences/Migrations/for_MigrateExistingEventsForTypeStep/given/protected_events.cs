// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.given;

public class protected_events : Specification
{
    protected InMemoryEncryptionKeyStorage _keys;
    protected IEncryptionKeyStorage _keyAccess;
    protected IEventSequenceStorage _sequence;
    protected JsonSchemaMetadataManager _manager;
    protected ExpandoObjectConverter _converter;
    protected JsonSchema _sourceSchema;
    protected JsonSchema _targetSchema;
    protected MigrateExistingEventsForTypeStep _step;
    protected MigrateExistingEventsForTypeStepState _state;
    protected Catch<JobStepResult> _result;
    protected readonly Dictionary<ulong, IDictionary<EventTypeGeneration, ExpandoObject>> _stored = [];
    protected readonly List<string> _migrationInputs = [];
    protected readonly List<ExpandoObject> _plaintextEvents = [];
    protected readonly List<AppendedEvent> _events = [];
    protected const string Store = "test-event-store";
    protected const string Namespace = "default";
    protected virtual bool UsesObjectArray => false;
    protected virtual string MigrationSuffix => " migrated";
    protected virtual string SourceSchemaJson => UsesObjectArray
        ? """{"type":"object","properties":{"contacts":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}}}}"""
        : """{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""";
    protected virtual string SourceContentJson => UsesObjectArray ? """{"contacts":[{"name":"Jane"}]}""" : """{"name":"Jane"}""";

    async Task Establish()
    {
        _keys = new InMemoryEncryptionKeyStorage();
        _keyAccess = Substitute.For<IEncryptionKeyStorage>();
        _keyAccess.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>())
            .Returns(call => _keys.TryGetFor(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<EncryptionKeyIdentifier>(2), call.ArgAt<EncryptionKeyRevision?>(3)));
        _keyAccess.GetErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>())
            .Returns(call => _keys.GetErasureFor(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<EncryptionKeyIdentifier>(2)));
        var encryption = new Encryption();
        _manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(_keys, encryption), _keyAccess, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new ExpandoObjectConverter(new TypeFormats());
        _sourceSchema = await JsonSchema.FromJsonAsync(SourceSchemaJson);
        _targetSchema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"renamed":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        EventTypeId typeId = "protected-event";
        var eventTypes = Substitute.For<IEventTypesStorage>();
        eventTypes.GetFor(typeId, 1).Returns(new EventTypeSchema(new EventType(typeId, 1), EventTypeOwner.Server, EventTypeSource.Code, _sourceSchema));
        eventTypes.GetFor(typeId, 2).Returns(new EventTypeSchema(new EventType(typeId, 2), EventTypeOwner.Server, EventTypeSource.Code, _targetSchema));
        var migrations = Substitute.For<IEventTypeMigrations>();
        migrations.MigrateToAllGenerations(Arg.Any<EventStoreName>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>()).Returns(call =>
        {
            var plaintext = call.ArgAt<JsonObject>(2);
            var name = (UsesObjectArray ? plaintext["contacts"]![0]!["name"] : plaintext["name"])!.GetValue<string>();
            _migrationInputs.Add(name);
            _plaintextEvents.Add(call.ArgAt<ExpandoObject>(3));
            return Task.FromResult<IDictionary<EventTypeGeneration, ExpandoObject>>(new Dictionary<EventTypeGeneration, ExpandoObject>
            {
                [1] = call.ArgAt<ExpandoObject>(3),
                [2] = _converter.ToExpandoObject(new JsonObject { ["renamed"] = name + MigrationSuffix }, _targetSchema)
            });
        });
        foreach (var (number, subject) in new[] { (1UL, "erased-owner"), (2UL, "active-owner") })
        {
            var content = await _manager.Apply(Store, Namespace, _sourceSchema, subject, JsonNode.Parse(SourceContentJson)!.AsObject());
            var storedContent = _converter.ToExpandoObject(content, _sourceSchema);
            _stored[number] = new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = storedContent };
            _events.Add(new AppendedEvent(EventContext.From(Store, Namespace, new EventType(typeId, 1), EventSourceType.Default, "not-the-subject", EventStreamType.All, EventStreamId.Default, number, CorrelationId.NotSet, subject: subject), storedContent));
        }
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(_events);
        var page = 0;
        cursor.MoveNext().Returns(_ => Task.FromResult(page++ == 0));
        _sequence = Substitute.For<IEventSequenceStorage>();
        _sequence.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>()).Returns(cursor);
        _sequence.ReplaceGenerationContent(Arg.Any<EventSequenceNumber>(), Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>()).Returns(call =>
        {
            _stored[call.ArgAt<EventSequenceNumber>(0).Value] = call.ArgAt<IDictionary<EventTypeGeneration, ExpandoObject>>(1);
            return Task.CompletedTask;
        });
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        var ns = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore((EventStoreName)Store).Returns(store);
        store.EventTypes.Returns(eventTypes);
        store.GetNamespace((EventStoreNamespaceName)Namespace).Returns(ns);
        ns.GetEventSequence(WellKnownEventSequences.EventLog).Returns(_sequence);
        var silo = new TestKitSilo();
        silo.AddService(storage);
        silo.AddService(migrations);
        silo.AddService<IJsonSchemaMetadataManager>(_manager);
        silo.AddService<IExpandoObjectConverter>(_converter);
        silo.AddService(new JsonSerializerOptions());
        silo.AddService(Substitute.For<IJobStepThrottle>());
        silo.AddService(NullLogger<MigrateExistingEventsForTypeStep>.Instance);
        silo.AddPersistentStateStorage<MigrateExistingEventsForTypeStepState>(nameof(MigrateExistingEventsForTypeStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps);
        _step = await silo.CreateGrainAsync<MigrateExistingEventsForTypeStep>(JobStepId.New(), new JobStepKey(JobId.New(), Store, Namespace));
        _state = new MigrateExistingEventsForTypeStepState { EventTypeId = typeId };
    }

    protected async Task Perform() => _result = await (Task<Catch<JobStepResult>>)typeof(MigrateExistingEventsForTypeStep)
        .GetMethod("PerformStep", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_step, [_state, CancellationToken.None])!;

    protected JsonObject Target(ulong number) => _converter.ToJsonObject(_stored[number][2], _targetSchema);
}
