// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
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

/// <summary>
/// The projection publishes the document as the sink stores it - protected values encrypted, lineage attached - and
/// Watch is the single place that releases it. Plaintext shaped exactly like ciphertext (base64 of 256 bytes) in a
/// [PII] and a namespace-encrypted property must reach the client unchanged. Real encryption, real handlers and
/// in-memory keys: a substitute at this seam would hide a second release.
/// </summary>
public class and_protected_plaintext_has_cipher_shape : given.all_dependencies
{
    const string Key = "person-42";
    const string Owner = "subject-7";
    static readonly string _cipherShaped = Convert.ToBase64String(new byte[256]);

    readonly List<ReadModelChangeset> _emitted = [];
    TaskCompletionSource<ChangesetForwarder> _forwarderCaptured;
    ReadModelsCompliance _compliance;
    JsonSchema _schema;
    JsonObject _published;
    Exception _exception;

    JsonObject Streamed => JsonNode.Parse(_emitted.Single(_ => !_.Subscribed).ReadModel).AsObject();

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {"type":"object","properties":{
              "id":{"type":"string"},
              "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
              "secret":{"type":"string","security":[{"metadataType":"EncryptedNamespace","details":""}]}
            }}
            """);
        _readModelDefinition = _readModelDefinition with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, _schema } } };
        _readModel.GetDefinition().Returns(Task.FromResult(_readModelDefinition));

        var converter = new ExpandoObjectConverter(new TypeFormats());
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        _compliance = new ReadModelsCompliance(
            new JsonSchemaMetadataManager(
                new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                    new PIICompliancePropertyValueHandler(provisioner, keys, encryption),
                    new EncryptedNamespaceValueHandler(provisioner, keys, encryption)),
                NullLogger<JsonSchemaMetadataManager>.Instance),
            converter);

        var stored = await _compliance.Apply("test-store", "test-namespace", _schema, Owner, converter.ToExpandoObject(new JsonObject { ["id"] = Key, ["name"] = _cipherShaped, ["secret"] = _cipherShaped }, _schema));
        _published = converter.ToJsonObject(stored, _schema);
        _published[WellKnownProperties.Subject] = Owner;

        _forwarderCaptured = new();
        var notifier = Substitute.For<IProjectionChangesetNotifier>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(notifier);
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(Substitute.For<IReadModelChangesetSubscriber>());
        _changesetMediator.When(m => m.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>()))
            .Do(ci => _forwarderCaptured.SetResult(ci.Arg<ChangesetForwarder>()));
        notifier.Subscribe(Arg.Any<IReadModelChangesetSubscriber>()).Returns(Task.CompletedTask);
        notifier.Unsubscribe(Arg.Any<IReadModelChangesetSubscriber>()).Returns(Task.CompletedTask);

        _service = new ReadModels(
            _grainFactory,
            _storage,
            _readBindings,
            converter,
            _reducerMediator,
            _changesetMediator,
            _localSiloDetails,
            _compliance,
            _materializedReadModels,
            new JsonSerializerOptions(),
            NamespaceCasingContext.Create(),
            Substitute.For<Cratis.Chronicle.ReadModels.IReadModelSinkProvenance>());
    }

    async Task Because()
    {
        _service.Watch(
            new WatchRequest { EventStore = "test-store", Namespace = "test-namespace", ReadModelIdentifier = "test-read-model" },
            default).Subscribe(_emitted.Add);

        var forwarder = await _forwarderCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var change = new ReadModelChangeContext(Concepts.ReadModels.ReadModelChangeType.Added, 42, DateTimeOffset.UtcNow, CorrelationId.NotSet) with { Subject = Owner };
        _exception = await Catch.Exception(async () => await forwarder("test-namespace", Key, _published, change));
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_publish_protected_values_encrypted() => _published["name"]!.GetValue<string>().ShouldNotEqual(_cipherShaped);
    [Fact] void should_stream_the_personal_data_unchanged() => Streamed["name"]!.GetValue<string>().ShouldEqual(_cipherShaped);
    [Fact] void should_stream_the_namespace_confidential_value_unchanged() => Streamed["secret"]!.GetValue<string>().ShouldEqual(_cipherShaped);
}
