// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page.given;

public class a_page_pipeline : Specification
{
    protected readonly ReplayContext _replayContext = new(new ReadModelType("roots", ReadModelGeneration.First), "roots", "roots-revert", DateTimeOffset.UtcNow);
    protected ProjectionPipeline _pipeline;
    protected IEventSequenceStorage _storage;
    protected ISink _sink;
    protected ISinkPagePrefetch _prefetch;
    protected IProjection _projection;
    protected AppendedEvent[] _events;
    protected Dictionary<EventSourceId, AppendedEvent> _parents;
    protected Dictionary<string, ExpandoObject> _documents;
    protected Dictionary<string, ExpandoObject> _prepared;
    protected int _sinkReads;
    protected int _sinkBatches;
    protected readonly EventType _parentType = new("parent", 1);
    protected readonly EventType _childType = new("child", 1);

    void Establish()
    {
        _storage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink, ISinkPagePrefetch>();
        _prefetch = (ISinkPagePrefetch)_sink;
        _documents = new();
        _prepared = new();
        _parents = new();
        _events = Enumerable.Range(0, 4).Select(index => EventFor($"child-{index}", _childType, (ulong)index, $"parent-{index % 2}")).ToArray();
        for (var index = 0; index < 2; index++)
        {
            _parents[$"parent-{index}"] = EventFor($"parent-{index}", _parentType, (ulong)(100 + index), $"root-{index}");
            _documents[$"root-{index}"] = new ExpandoObject();
            ((IDictionary<string, object?>)_documents[$"root-{index}"])["count"] = 0;
        }
        _storage.GetLastInstancesOfAny(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(call => Task.FromResult<IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>>>(call.ArgAt<IEnumerable<EventSourceId>>(0).ToDictionary(source => source, source => new Option<AppendedEvent>(_parents[source]))));
        _storage.TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(call => Task.FromResult(new Option<AppendedEvent>(_parents[call.Arg<EventSourceId>()])));
        ConfigureSink();
        ConfigureProjection();
        _pipeline = CreatePipeline();
    }

    protected ProjectionPipeline CreatePipeline()
    {
        var replayStorage = new ReplayScopedEventSequenceStorage(_storage);
        return new(
            _projection,
            _sink,
            Substitute.For<IChangesetStorage>(),
            Substitute.For<IObjectComparer>(),
            [new ResolveAndCount(replayStorage, _sink)],
            new ProjectionHandleLock(),
            replayStorage,
            NullLogger<ProjectionPipeline>.Instance);
    }

    protected async Task<string> HandlePage(ProjectionPipeline pipeline)
    {
        foreach (var @event in _events) await pipeline.Handle(@event);
        return string.Join(',', _documents.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{((IDictionary<string, object?>)pair.Value)["count"]}"));
    }

    protected void ResetCounts()
    {
        foreach (var document in _documents.Values) ((IDictionary<string, object?>)document)["count"] = 0;
        _sinkReads = 0;
        _sinkBatches = 0;
        _storage.ClearReceivedCalls();
    }

    protected AppendedEvent EventFor(string source, EventType type, ulong sequence, string parent)
    {
        dynamic content = new ExpandoObject();
        content.parent = parent;
        return new(EventContext.Empty with { EventSourceId = source, EventType = type, SequenceNumber = sequence }, content);
    }

    void ConfigureProjection()
    {
        var resolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        _projection = Substitute.For<IProjection>();
        _projection.Identifier.Returns(new ProjectionId("root-projection"));
        _projection.Path.Returns(new ProjectionPath("root"));
        _projection.OwnEventTypes.Returns([_parentType]);
        _projection.EventTypes.Returns([_parentType, _childType]);
        _projection.HasKeyResolverFor(Arg.Any<EventType>()).Returns(true);
        var parentKey = resolvers.FromEventValueProvider(@event => ((IDictionary<string, object?>)@event.Content)["parent"]!);
        _projection.GetKeyResolverFor(_parentType).Returns(parentKey);
        var child = Substitute.For<IProjection>();
        child.Identifier.Returns(new ProjectionId("child-projection"));
        child.Path.Returns(new ProjectionPath("children"));
        child.HasParent.Returns(true);
        child.Parent.Returns(_projection);
        child.ChildrenPropertyPath.Returns(new PropertyPath("children"));
        child.IdentifiedByProperty.Returns(new PropertyPath("id"));
        child.ChildProjections.Returns([]);
        var resolver = resolvers.FromParentHierarchy(child, resolvers.FromEventSourceId, parentKey, new PropertyPath("id"));
        _projection.GetKeyResolverFor(_childType).Returns(resolver);
    }

    void ConfigureSink()
    {
        _prefetch.When(prefetch => prefetch.ClearPrefetchPage()).Do(_ => _prepared.Clear());
        _prefetch.PrefetchPage(Arg.Any<IEnumerable<Key>>()).Returns(call =>
        {
            _sinkBatches++;
            foreach (var key in call.Arg<IEnumerable<Key>>())
            {
                var id = key.Value.ToString()!;
                if (_documents.TryGetValue(id, out var document)) _prepared[id] = document;
            }
            return Task.CompletedTask;
        });
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(call =>
        {
            var id = call.Arg<Key>().Value.ToString()!;
            if (_prepared.TryGetValue(id, out var prepared)) return Task.FromResult<ExpandoObject?>(prepared);
            _sinkReads++;
            return Task.FromResult<ExpandoObject?>(_documents.GetValueOrDefault(id));
        });
    }

    protected sealed class ResolveAndCount(IEventSequenceStorage storage, ISink sink) : ICanPerformProjectionPipelineStep
    {
        public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
        {
            var result = (ResolvedKey)await projection.GetKeyResolverFor(context.Event.Context.EventType)(storage, sink, context.Event);
            var state = (IDictionary<string, object?>)(await sink.FindOrDefault(result.Key))!;
            state["count"] = (int)state["count"]! + 1;
            return context with { Key = result.Key };
        }
    }
}
