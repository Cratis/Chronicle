// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

/// <summary>
/// A child's unresolved parent source differs from the root selected by its later parent event.
/// </summary>
public class and_a_child_precedes_a_content_keyed_parent : Cratis.Chronicle.Projections.Engine.for_KeyResolvers.given.a_parent_key_history
{
    IProjectionFutures _futures;
    ResolveFutures _step;
    ProjectionFuturesTracker _tracker;
    ProjectionFuture _future;
    AppendedEvent _parentEvent;
    ProjectionEventContext _result;
    ProjectionEventContext _uninitialized;
    ProjectionEventContext _differentRoot;
    ProjectionEventContext? _applied;
    KeyResolverResult _afterRelease;

    async Task Establish()
    {
        _parent.ChildrenPropertyPath.Returns(PropertyPath.Root);
        _parent.ChildProjections.Returns([_child]);
        _parent.TargetReadModelSchema.Returns(new JsonSchema());
        _child.GetKeyResolverFor(_childEventType).Returns(_resolveChild);
        _child.OnNext(Arg.Do<ProjectionEventContext>(context =>
        {
            _applied = context;
            context.Changeset.AddChild<ExpandoObject>(
                new PropertyPath("children"),
                new PropertyPath("id"),
                context.Key.ArrayIndexers.All.Single().Identifier,
                [],
                context.Key.ArrayIndexers);
        }));

        await Append(0, _childEventType, ChildSource, new { parentId = ParentSource });
        var childEvent = await _storage.GetEventAt(0);
        var deferred = await _resolveChild(_storage, _sink, childEvent);
        deferred.ShouldBeOfExactType<DeferredKey>();
        _future = ((DeferredKey)deferred).Future;
        await Append(1, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        _parentEvent = await _storage.GetEventAt(1);

        // Replay can already see a later key change, but it must not route this release there.
        await Append(2, _parentEventType, ParentSource, new { rootId = LaterRoot });

        _futures = Substitute.For<IProjectionFutures>();
        _futures.GetFutures().Returns(Task.FromResult<IEnumerable<ProjectionFuture>>([_future]));
        _tracker = new ProjectionFuturesTracker { HasPending = true };
        _step = new ResolveFutures(
            _futures,
            _tracker,
            Substitute.For<ITypeFormats>(),
            new ObjectComparer(),
            NullLogger<ResolveFutures>.Instance,
            _storage,
            _sink);
    }

    async Task Because()
    {
        _uninitialized = await _step.Perform(_parent, ContextFor(OriginalRoot, initialized: false));
        _differentRoot = await _step.Perform(_parent, ContextFor(LaterRoot, initialized: true));
        _result = await _step.Perform(_parent, ContextFor(OriginalRoot, initialized: true));
        _afterRelease = await _resolveChild(_storage, _sink, _future.Event);
    }

    ProjectionEventContext ContextFor(string key, bool initialized)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)[WellKnownProperties.ReadModelInstanceInitialized] = initialized;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), _parentEvent, state);
        var context = new ProjectionEventContext(
            new Key(key, ArrayIndexers.NoIndexers),
            _parentEvent,
            changeset,
            ProjectionOperationType.None,
            NeedsInitialState: false);
        context.MemoizeResolvedKey(_resolveChild, KeyResolverResult.Resolved(new Key(LaterRoot, ArrayIndexers.NoIndexers)));
        return context;
    }

    [Fact] void should_store_the_unresolved_parent_source() => _future.ParentKey.Value.ShouldEqual(ParentSource);
    [Fact] void should_not_release_into_an_uninitialized_root() => _uninitialized.PendingFutureSaves.ShouldBeEmpty();
    [Fact] void should_not_release_into_a_different_root() => _differentRoot.PendingFutureSaves.ShouldBeEmpty();
    [Fact] void should_queue_the_child_when_its_content_keyed_parent_arrives() => _result.PendingFutureSaves.Single().FutureId.ShouldEqual(_future.Id);
    [Fact] void should_save_under_the_releasing_parent_key_not_the_source_or_later_key() => _result.PendingFutureSaves.Single().Key.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_preserve_the_child_indexer() => _result.PendingFutureSaves.Single().Key.ArrayIndexers.All.Single().Identifier.ShouldEqual(ChildSource);
    [Fact] void should_apply_the_original_child_event() => _applied!.Event.ShouldEqual(_future.Event);
    [Fact]
    void should_project_the_child_into_the_root_state()
    {
        var root = (IDictionary<string, object?>)_result.PendingFutureSaves.Single().Changeset.CurrentState;
        var child = ((IEnumerable<object>)root["children"]!).OfType<ExpandoObject>().Single();
        ((IDictionary<string, object?>)child)["id"].ShouldEqual(ChildSource);
    }

    [Fact]
    void should_replace_the_releasing_events_key_memo_for_the_child()
    {
        _applied!.TryGetResolvedKey(_resolveChild, out var result).ShouldBeTrue();
        ((ResolvedKey)result!).Key.Value.ShouldEqual(OriginalRoot);
    }

    [Fact] void should_keep_the_future_pending_until_its_write_is_saved() => _tracker.HasPending.ShouldBeTrue();
    [Fact] void should_not_leak_the_inclusive_release_boundary_to_an_ordinary_retry() => _afterRelease.ShouldBeOfExactType<DeferredKey>();
}
