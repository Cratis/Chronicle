// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

/// <summary>
/// The projection subscriber supplies a released snapshot with a watermark that the schema does not declare.
/// Watch strips internal bookkeeping without sending that snapshot through the compliance manager again.
/// </summary>
public class and_document_carries_kernel_bookkeeping : given.all_dependencies
{
    const string Key = "person-42";
    const string DecryptedName = "decrypted-name";

    readonly List<ReadModelChangeset> _emitted = [];
    IProjectionChangesetNotifier _notifier;
    IReadModelChangesetSubscriber _subscriber;
    TaskCompletionSource<ChangesetForwarder> _forwarderCaptured;
    Exception _exception;

    void Establish()
    {
        var schema = JsonSchema.FromType<ReadModelWithPersonalData>();
        schema.Properties["name"].ExtensionData = new Dictionary<string, object?>
        {
            { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
        };

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<Concepts.ReadModels.ReadModelGeneration, JsonSchema> { { (Concepts.ReadModels.ReadModelGeneration)1, schema } }
        };
        _readModel.GetDefinition().Returns(Task.FromResult(_readModelDefinition));

        _forwarderCaptured = new();
        _notifier = Substitute.For<IProjectionChangesetNotifier>();
        _subscriber = Substitute.For<IReadModelChangesetSubscriber>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(_notifier);
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(_subscriber);

        _changesetMediator.When(m => m.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>()))
            .Do(ci => _forwarderCaptured.SetResult(ci.Arg<ChangesetForwarder>()));

        _notifier.Subscribe(Arg.Any<IReadModelChangesetSubscriber>()).Returns(Task.CompletedTask);
        _notifier.Unsubscribe(Arg.Any<IReadModelChangesetSubscriber>()).Returns(Task.CompletedTask);

        var valueHandler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        valueHandler.Type.Returns((SchemaMetadataTypeName)"PII");
        valueHandler.Category.Returns(SchemaMetadataCategory.Compliance);
        valueHandler.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Key, Arg.Any<JsonNode>())
            .Returns(Task.FromResult<JsonNode>(JsonValue.Create(DecryptedName)));

        _service = new ReadModels(
            _grainFactory,
            _storage,
            _expandoObjectConverter,
            _reducerMediator,
            _changesetMediator,
            _localSiloDetails,
            new ReadModelsCompliance(
                new JsonSchemaMetadataManager(
                    new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(valueHandler),
                    NullLogger<JsonSchemaMetadataManager>.Instance),
                _expandoObjectConverter),
            _materializedReadModels,
            new JsonSerializerOptions());
    }

    async Task Because()
    {
        _service.Watch(
            new WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" },
            default).Subscribe(_emitted.Add);

        var forwarder = await _forwarderCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Exactly the document ProjectionObserverSubscriber pushes: the schema round trip, plus the watermark
        // stamped on top of it.
        var document = new JsonObject
        {
            ["id"] = Key,
            ["name"] = DecryptedName,
            [WellKnownProperties.LastHandledEventSequenceNumber] = JsonValue.Create(42UL)
        };

        _exception = await Catch.Exception(async () => await forwarder(
            "test-namespace",
            Key,
            document,
            new Concepts.ReadModels.ReadModelChangeContext(
                Concepts.ReadModels.ReadModelChangeType.Added,
                Concepts.Events.EventSequenceNumber.First,
                DateTimeOffset.UtcNow,
                Cratis.Execution.CorrelationId.NotSet)));
    }

    [Fact] void should_keep_the_last_handled_watermark() => JsonSerializer.Deserialize<JsonElement>(_emitted.Single(_ => !_.Subscribed).ReadModel).GetProperty(WellKnownProperties.LastHandledEventSequenceNumber).GetUInt64().ShouldEqual(42UL);
    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_stream_the_changeset() => _emitted.Count(_ => !_.Subscribed).ShouldEqual(1);
    [Fact] void should_stream_the_decrypted_read_model() => JsonSerializer.Deserialize<JsonElement>(_emitted.Single(_ => !_.Subscribed).ReadModel).GetProperty("name").GetString().ShouldEqual(DecryptedName);

    record ReadModelWithPersonalData(string Id, string Name);
}
