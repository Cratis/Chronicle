// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using EngineProjection = Cratis.Chronicle.Projections.Engine.Projection;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_a_deferred_child_update_resolves_on_creation : given.all_dependencies
{
    static readonly EventType _created = new("ChildCreated", 1);
    static readonly EventType _updated = new("ChildUpdated", 1);
    static readonly PropertyPath _children = new("[children]");
    static readonly string _secret = Convert.ToBase64String(new byte[256]);
    readonly TaskCompletionSource<ChangesetForwarder> _captured = new();
    readonly List<ProjectionFuture> _futures = [];
    readonly List<JsonObject> _received = [];
    IProjectionPipeline _pipeline;
    EngineProjection _root;
    EngineProjection _child;
    ExpandoObjectConverter _converter;
    ExpandoObject _stored;
    ExpandoObject _savedChild;
    ProjectionEventContext _resolved;
    bool _wasDeferred;
    IDisposable _subscription;
    IDisposable _childSubscription;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "id":{"type":"string"},
              "secret":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
              "children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"label":{"type":"string"}}}}
            }}
            """);
        _readModelDefinition = _readModelDefinition with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [1] = schema } };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new(new TypeFormats());
        var compliance = new ReadModelsCompliance(manager, _converter);
        var initial = _converter.ToExpandoObject(new JsonObject { ["id"] = "root", ["secret"] = _secret, ["children"] = new JsonArray() }, schema);
        _stored = await compliance.Apply("test-store", "test-namespace", schema, "owner", initial);
        ((IDictionary<string, object?>)_stored)[WellKnownProperties.ReadModelInstanceInitialized] = true;
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(_ => Task.FromResult<ExpandoObject?>(_stored.Clone()));
        _sink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>()).Returns(call =>
        {
            var changes = call.ArgAt<IChangeset<AppendedEvent, ExpandoObject>>(1).Changes;
            foreach (var property in changes.OfType<PropertiesChanged<ExpandoObject>>().SelectMany(change => change.Differences))
            {
                property.PropertyPath.SetValue(_stored, property.Changed!, property.ArrayIndexers);
            }
            _savedChild = ((ExpandoObject)changes.OfType<ChildAdded>().Single().Child).Clone();
            ((IDictionary<string, object?>)_stored)["children"] = new List<ExpandoObject> { _savedChild };
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        });
        _namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(Substitute.For<IEventSequenceStorage>());
        var futures = Substitute.For<IProjectionFutures>();
        futures.AddFuture(Arg.Any<ProjectionFuture>()).Returns(call =>
        {
            _futures.Add(call.Arg<ProjectionFuture>());
            return Task.FromResult(_futures.Count);
        });
        futures.GetFutures().Returns(_ => Task.FromResult<IEnumerable<ProjectionFuture>>(_futures.ToArray()));
        futures.ResolveFuture(Arg.Any<ProjectionFutureId>()).Returns(call =>
        {
            _futures.RemoveAll(future => future.Id == call.Arg<ProjectionFutureId>());
            return Task.CompletedTask;
        });
        _grainFactory.GetGrain<IProjectionFutures>(Arg.Any<string>()).Returns(futures);
        _child = new(EventSequenceId.Log, "projection", new ExpandoObject(), new ProjectionPath("children"), _children, "id", _readModelDefinition, schema, true, AutoMap.Disabled, new HashSet<string>(), []);
        _root = new(EventSequenceId.Log, "projection", new ExpandoObject(), ProjectionPath.GetRootFor("projection"), PropertyPath.Root, "id", _readModelDefinition, schema, true, AutoMap.Disabled, new HashSet<string>(), [_child]);
        _child.SetParent(_root);
        Task<KeyResolverResult> Resolve(IEventSequenceStorage sequence, ISink sink, AppendedEvent @event)
        {
            var indexers = new ArrayIndexers([new ArrayIndexer(_children, "id", "child")]);
            if (@event.Context.EventType == _updated && !((IEnumerable<object>)((IDictionary<string, object?>)_stored)["children"]!).Any())
            {
                return Task.FromResult(KeyResolverResult.Deferred(new ProjectionFuture(ProjectionFutureId.New(), _root.Identifier, @event, PropertyPath.Root, _children, "id", "id", new Key("child", ArrayIndexers.NoIndexers), DateTimeOffset.UtcNow)));
            }
            return Task.FromResult(KeyResolverResult.Resolved(new Key("root", indexers)));
        }
        EventTypeWithKeyResolver[] registrations = [new(_created, Resolve), new(_updated, Resolve)];
        _root.SetEventTypesWithKeyResolvers(registrations, [], new Dictionary<EventType, ProjectionOperationType>());
        _child.SetEventTypesWithKeyResolvers(registrations, [_created, _updated], new Dictionary<EventType, ProjectionOperationType> { [_created] = ProjectionOperationType.From, [_updated] = ProjectionOperationType.From });
        var mapper = PropertyMappers.FromEventValueProvider("[children].label", @event => ((IDictionary<string, object?>)@event.Content)["label"]!);
        _childSubscription = _child.Event.Subscribe(context =>
        {
            if (context.Event.Context.EventType == _created)
            {
                context.Changeset.AddChild<ExpandoObject>(_children, "id", "child", [mapper], context.Key.ArrayIndexers);
            }
            else
            {
                context.Changeset.SetProperties([mapper], context.Key.ArrayIndexers);
            }
        });
        var pipelines = new ProjectionPipelineManager(_storage, _grainFactory, new ObjectComparer(), new TypeFormats(), compliance, Options.Create(new Configuration.ChronicleOptions()), NullLoggerFactory.Instance);
        _pipeline = await pipelines.GetFor("test-store", "test-namespace", _root);
        _service = new ReadModels(_grainFactory, _storage, _converter, _reducerMediator, _changesetMediator, _localSiloDetails, compliance, _materializedReadModels, new JsonSerializerOptions());
        _grainFactory.GetGrain<IProjectionChangesetNotifier>(Arg.Any<string>()).Returns(Substitute.For<IProjectionChangesetNotifier>());
        _grainFactory.GetGrain<IReadModelChangesetSubscriber>(Arg.Any<string>()).Returns(Substitute.For<IReadModelChangesetSubscriber>());
        _changesetMediator.When(mediator => mediator.Subscribe(Arg.Any<Guid>(), Arg.Any<ChangesetForwarder>())).Do(call => _captured.SetResult(call.Arg<ChangesetForwarder>()));
    }

    async Task Because()
    {
        _subscription = _service.Watch(new Contracts.ReadModels.WatchRequest { EventStore = "test-store", ReadModelIdentifier = "test-read-model" }).Subscribe(change =>
        {
            if (!change.Subscribed) _received.Add(JsonNode.Parse(change.ReadModel)!.AsObject());
        });
        var forwarder = await _captured.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _wasDeferred = (await _pipeline.Handle(Event(_updated, 2, "updated before creation"))).IsDeferred;
        _resolved = await _pipeline.Handle(Event(_created, 3, "creation value"));
        await forwarder("test-namespace", "root", _converter.ToJsonObject(_resolved.ReleasedReadModel!, _root.TargetReadModelSchema), new ReadModelChangeContext(ReadModelChangeType.Modified, 3UL, DateTimeOffset.UtcNow, Cratis.Execution.CorrelationId.NotSet));
    }

    void Destroy()
    {
        _subscription.Dispose();
        _childSubscription.Dispose();
        _root.Dispose();
        _child.Dispose();
    }

    AppendedEvent Event(EventType type, ulong sequence, string label) => new(EventContext.From("test-store", "test-namespace", type, EventSourceType.Default, "child", EventStreamType.All, EventStreamId.Default, sequence, CorrelationId.NotSet, subject: "owner"), _converter.ToExpandoObject(new JsonObject { ["id"] = "child", ["label"] = label }, _root.TargetReadModelSchema.Properties["children"].Item!));

    [Fact] void should_defer_the_update_until_creation() => _wasDeferred.ShouldBeTrue();
    [Fact] void should_resolve_the_future() => _futures.ShouldBeEmpty();
    [Fact] void should_save_the_resolved_update() => ((IDictionary<string, object?>)_savedChild)["label"].ShouldEqual("updated before creation");
    [Fact] void should_publish_the_same_child_that_was_saved() => _received.Single()["children"]![0]!["label"]!.GetValue<string>().ShouldEqual((string)((IDictionary<string, object?>)_savedChild)["label"]!);
    [Fact] void should_preserve_protected_root_plaintext_without_releasing_again() => _received.Single()["secret"]!.GetValue<string>().ShouldEqual(_secret);
}
