// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_a_projection_produces_pii_after_erasure : given.all_dependencies
{
    readonly TaskCompletionSource<ChangesetForwarder> _captured = new();
    readonly List<JsonObject> _received = [];
    ProjectionEventContext _context;
    EncryptChangeset _step;
    Cratis.Chronicle.Projections.Engine.IProjection _projection;
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _child;
    IDisposable _subscription;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"contacts":{"type":"array","items":{"type":"object","properties":{
              "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
              "secret":{"type":"string","security":[{"metadataType":"EncryptedNamespace","details":""}]}
            }}}}}
            """);
        _readModelDefinition = _readModelDefinition with { Schemas = new Dictionary<Concepts.ReadModels.ReadModelGeneration, JsonSchema> { [1] = _schema } };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        var keys = new InMemoryEncryptionKeyStorage();
        await keys.RecordErasureFor("test-store", "test-namespace", "owner");
        await keys.DeleteFor("test-store", "test-namespace", "owner");
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, keys, encryption),
                new EncryptedNamespaceValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new ExpandoObjectConverter(new TypeFormats());
        var compliance = new ReadModelsCompliance(manager, _converter);
        var comparer = new ObjectComparer();
        _step = new EncryptChangeset(compliance, comparer, "test-store", "test-namespace");
        _projection = Substitute.For<Cratis.Chronicle.Projections.Engine.IProjection>();
        _projection.TargetReadModelSchema.Returns(_schema);
        var @event = new AppendedEvent(EventContext.From("test-store", "test-namespace", EventType.Unknown, EventSourceType.Default, "not-owner", EventStreamType.All, EventStreamId.Default, 1UL, CorrelationId.NotSet, subject: "owner"), new ExpandoObject());
        _child = new ExpandoObject();
        ((IDictionary<string, object?>)_child)["name"] = "new personal value";
        ((IDictionary<string, object?>)_child)["secret"] = "namespace secret";
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(comparer, @event, new ExpandoObject());
        changeset.AddChild("contacts", _child);
        _context = new ProjectionEventContext(new Key("not-owner", ArrayIndexers.NoIndexers), @event, changeset, ProjectionOperationType.None, false);
        _service = new ReadModels(_grainFactory, _storage, _expandoObjectConverter, _reducerMediator, _changesetMediator, _localSiloDetails, compliance, _materializedReadModels, new JsonSerializerOptions());
        var notifier = Substitute.For<IProjectionChangesetNotifier>();
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(notifier);
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(Substitute.For<IReadModelChangesetSubscriber>());
        _changesetMediator.When(m => m.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>())).Do(call => _captured.SetResult(call.Arg<ChangesetForwarder>()));
    }

    async Task Because()
    {
        _subscription = _service.Watch(new WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" }).Subscribe(change =>
        {
            if (!change.Subscribed) _received.Add(JsonNode.Parse(change.ReadModel)!.AsObject());
        });
        var forwarder = await _captured.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _step.Perform(_projection, _context);
        var snapshot = _converter.ToJsonObject(_context.ReleasedReadModel!, _schema);
        snapshot[WellKnownProperties.LastHandledEventSequenceNumber] = 1UL;
        await forwarder("test-namespace", "not-owner", snapshot, new Concepts.ReadModels.ReadModelChangeContext(Concepts.ReadModels.ReadModelChangeType.Modified, 1UL, DateTimeOffset.UtcNow, Cratis.Execution.CorrelationId.NotSet));
    }

    void Destroy() => _subscription.Dispose();

    [Fact] void should_blank_the_persisted_child() => ((IDictionary<string, object?>)_child)["name"].ShouldEqual(string.Empty);
    [Fact] void should_blank_the_watch_payload() => _received.Single()["contacts"]![0]!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_namespace_plaintext_without_releasing_again() => _received.Single()["contacts"]![0]!["secret"]!.GetValue<string>().ShouldEqual("namespace secret");
    [Fact] void should_keep_the_watermark() => _received.Single()[WellKnownProperties.LastHandledEventSequenceNumber]!.GetValue<ulong>().ShouldEqual(1UL);
}
