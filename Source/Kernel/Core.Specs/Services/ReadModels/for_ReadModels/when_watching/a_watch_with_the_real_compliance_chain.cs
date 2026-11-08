// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

/// <summary>
/// A watch whose compliance chain is wired for real rather than substituted, because a substitute at that seam is
/// exactly what hides a hand-off that fails. The value handler decrypts the name property only for
/// <see cref="ReleasedSubject"/>.
/// </summary>
public class a_watch_with_the_real_compliance_chain : given.all_dependencies
{
    protected const string Key = "person-42";
    protected const string EncryptedName = "encrypted-name";
    protected const string DecryptedName = "decrypted-name";

    protected readonly List<ReadModelChangeset> _emitted = [];
    protected Exception _exception;
    TaskCompletionSource<ChangesetForwarder> _forwarderCaptured;

    protected virtual string ReleasedSubject => Key;

    protected virtual bool HasPersonalData => true;

    protected JsonObject Streamed => JsonNode.Parse(_emitted.Single(_ => !_.Subscribed).ReadModel).AsObject();

    protected static ReadModelChangeContext ChangeAt(ulong sequenceNumber) => new(
        Concepts.ReadModels.ReadModelChangeType.Added,
        sequenceNumber,
        DateTimeOffset.UtcNow,
        CorrelationId.NotSet);

    void Establish()
    {
        var schema = JsonSchema.FromType<ReadModelWithPersonalData>();
        if (HasPersonalData)
        {
            schema.Properties["name"].ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            };
        }

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, schema } }
        };
        _readModel.GetDefinition().Returns(Task.FromResult(_readModelDefinition));

        _forwarderCaptured = new();
        var notifier = Substitute.For<IProjectionChangesetNotifier>();
        var subscriber = Substitute.For<IReadModelChangesetSubscriber>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(notifier);
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(subscriber);
        _changesetMediator.When(m => m.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>()))
            .Do(ci => _forwarderCaptured.SetResult(ci.Arg<ChangesetForwarder>()));
        notifier.Subscribe(Arg.Any<IReadModelChangesetSubscriber>()).Returns(Task.CompletedTask);
        notifier.Unsubscribe(Arg.Any<IReadModelChangesetSubscriber>()).Returns(Task.CompletedTask);

        var valueHandler = Substitute.For<IJsonSchemaMetadataValueHandler>();
        valueHandler.Type.Returns((SchemaMetadataTypeName)"PII");
        valueHandler.Category.Returns(SchemaMetadataCategory.Compliance);
        valueHandler.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), ReleasedSubject, Arg.Any<JsonNode>())
            .Returns(Task.FromResult<JsonNode>(JsonValue.Create(DecryptedName)));

        _service = new ReadModels(
            _grainFactory,
            _storage,
            _readBindings,
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
            new JsonSerializerOptions(),
            NamespaceCasingContext.Create());
    }

    protected async Task Push(JsonObject document, ReadModelChangeContext change)
    {
        _service.Watch(
            new WatchRequest { EventStore = "test-store", Namespace = "test-namespace", ReadModelIdentifier = "test-read-model" },
            default).Subscribe(_emitted.Add);

        var forwarder = await _forwarderCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _exception = await Catch.Exception(async () => await forwarder("test-namespace", Key, document, change));
    }

    record ReadModelWithPersonalData(string Id, string Name);
}
