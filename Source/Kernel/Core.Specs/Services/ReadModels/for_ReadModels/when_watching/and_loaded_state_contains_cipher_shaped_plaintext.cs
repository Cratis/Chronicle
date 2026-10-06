// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_loaded_state_contains_cipher_shaped_plaintext : given.all_dependencies
{
    const string Owner = "owner";
    const string Source = "source-not-owner";
    readonly string _plaintext = Convert.ToBase64String(new byte[256]);
    readonly List<JsonObject> _received = [];
    readonly TaskCompletionSource _subscribed = new();
    TaskCompletionSource<ChangesetForwarder> _forwarderCaptured;
    InMemoryEncryptionKeyStorage _keyStorage;
    JsonObject _loadedState;
    IDisposable _subscription;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync(
            """
            { "type": "object", "properties": {
              "id": { "type": "string" },
              "name": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
              "secret": { "type": "string", "security": [{ "metadataType": "EncryptedNamespace", "details": "" }] },
              "tick": { "type": "integer" }
            } }
            """);
        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, schema } }
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _keyStorage = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keyStorage, encryption);
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, _keyStorage, encryption),
                new EncryptedNamespaceValueHandler(provisioner, _keyStorage, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var encrypted = await manager.Apply("test-store", "test-namespace", schema, Owner, new JsonObject { ["id"] = Source, ["name"] = _plaintext, ["secret"] = _plaintext, ["tick"] = 0 });

        // DecryptInitialState releases stored values for projection processing; the subscriber forwards
        // changeset.CurrentState, not the encrypted sink payload. Supply that already-released boundary value.
        _loadedState = await manager.Release("test-store", "test-namespace", schema, Owner, encrypted);
        _loadedState[WellKnownProperties.Subject] = Owner;
        _loadedState["tick"] = 1;
        _complianceHelper = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        _service = new ReadModels(_grainFactory, _storage, _expandoObjectConverter, _reducerMediator, _changesetMediator, _localSiloDetails, _complianceHelper, _materializedReadModels, new JsonSerializerOptions());

        _forwarderCaptured = new();
        var notifier = Substitute.For<IProjectionChangesetNotifier>();
        var subscriber = Substitute.For<IReadModelChangesetSubscriber>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(notifier);
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(subscriber);
        _changesetMediator.When(m => m.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>()))
            .Do(ci => _forwarderCaptured.SetResult(ci.Arg<ChangesetForwarder>()));
        notifier.Subscribe(Arg.Any<IReadModelChangesetSubscriber>()).Returns(Task.CompletedTask);
        notifier.Unsubscribe(Arg.Any<IReadModelChangesetSubscriber>()).Returns(Task.CompletedTask);
    }

    async Task Because()
    {
        _subscription = _service.Watch(new WatchRequest
        {
            EventStore = "test-store",
            ReadModelIdentifier = "test-read-model"
        }).Subscribe(change =>
        {
            if (change.Subscribed)
            {
                _subscribed.SetResult();
            }
            else
            {
                _received.Add(JsonNode.Parse(change.ReadModel)!.AsObject());
            }
        });
        await _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var forwarder = await _forwarderCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var context = new ReadModelChangeContext(Concepts.ReadModels.ReadModelChangeType.Modified, EventSequenceNumber.First, DateTimeOffset.UtcNow, Cratis.Execution.CorrelationId.NotSet);
        await forwarder("test-namespace", Source, _loadedState, context);
        await _keyStorage.DeleteFor("test-store", "test-namespace", Owner);
        var afterErasure = _loadedState.DeepClone().AsObject();
        afterErasure["name"] = string.Empty;
        afterErasure["tick"] = 2;
        await forwarder("test-namespace", Source, afterErasure, context);
    }

    void Destroy() => _subscription.Dispose();

    [Fact] void should_preserve_the_already_released_personal_plaintext() => _received[0]["name"]!.GetValue<string>().ShouldEqual(_plaintext);
    [Fact] void should_preserve_the_already_released_namespace_plaintext() => _received[0]["secret"]!.GetValue<string>().ShouldEqual(_plaintext);
    [Fact] void should_keep_personal_data_erased() => _received[1]["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_namespace_confidentiality_after_personal_erasure() => _received[1]["secret"]!.GetValue<string>().ShouldEqual(_plaintext);
}
