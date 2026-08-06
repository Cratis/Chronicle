// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

/// <summary>
/// The changeset document on the observable path belongs to the notifier that pushed it, not to the release
/// call — so releasing it must hand it back untouched. The compliance chain is wired for real here rather than
/// substituted, because the real manager releases onto a clone: a substitute that returns the very instance it
/// was given makes the release path look non-mutating when it is not.
/// </summary>
public class and_document_is_owned_by_the_caller : given.all_dependencies
{
    const string Key = "person-42";
    const string EncryptedName = "encrypted-name";
    const string DecryptedName = "decrypted-name";

    readonly List<ReadModelChangeset> _emitted = [];
    IProjectionChangesetNotifier _notifier;
    IReadModelChangesetSubscriber _subscriber;
    TaskCompletionSource<ChangesetForwarder> _forwarderCaptured;
    JsonObject _document;
    string _documentBefore;

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

        var valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        valueHandler.Type.Returns((ComplianceMetadataType)"PII");
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
                new JsonComplianceManager(
                    new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(valueHandler),
                    NullLogger<JsonComplianceManager>.Instance),
                _expandoObjectConverter),
            _eventCompliance,
            new JsonSerializerOptions());
    }

    async Task Because()
    {
        _service.Watch(
            new WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" },
            default).Subscribe(_emitted.Add);

        var forwarder = await _forwarderCaptured.Task.WaitAsync(TimeSpan.FromSeconds(5));

        _document = new JsonObject { ["id"] = Key, ["name"] = EncryptedName };
        _documentBefore = _document.ToJsonString();

        await forwarder(
            "test-namespace",
            Key,
            _document,
            new Concepts.ReadModels.ReadModelChangeContext(
                Concepts.ReadModels.ReadModelChangeType.Added,
                Concepts.Events.EventSequenceNumber.First,
                DateTimeOffset.UtcNow,
                Cratis.Execution.CorrelationId.NotSet));
    }

    [Fact] void should_stream_the_decrypted_read_model() => JsonSerializer.Deserialize<JsonElement>(_emitted.Single(_ => !_.Subscribed).ReadModel).GetProperty("name").GetString().ShouldEqual(DecryptedName);
    [Fact] void should_not_stamp_the_subject_marker_on_the_document_it_was_given() => _document.ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
    [Fact] void should_leave_the_document_it_was_given_unchanged() => _document.ToJsonString().ShouldEqual(_documentBefore);

    record ReadModelWithPersonalData(string Id, string Name);
}
