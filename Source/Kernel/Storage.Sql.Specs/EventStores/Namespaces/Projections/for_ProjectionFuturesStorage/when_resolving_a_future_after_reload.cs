// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Disposables;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections.for_ProjectionFuturesStorage;

public class when_resolving_a_future_after_reload : given.a_futures_storage
{
    readonly System.Reactive.Subjects.Subject<ProjectionEventContext> _childEvents = new();
    readonly CompositeDisposable _subscriptions = new();
    Chronicle.Projections.IProjectionFutures _futures;
    IProjection _projection;
    ResolveFutures _step;
    ProjectionEventContext _context;
    ProjectionEventContext _result;

    async Task Establish()
    {
        var children = new PropertyPath("[children]");
        _future = _future with { ChildPath = children };
        await _storage.Save(_future.ProjectionId, _future);
        var reloadedStorage = new ProjectionFuturesStorage(_eventStore, _namespace, _database, _options);
        _futures = Substitute.For<Chronicle.Projections.IProjectionFutures>();
        _futures.GetFutures().Returns(_ => reloadedStorage.GetForProjection(_future.ProjectionId));
        _futures.ResolveFuture(Arg.Any<ProjectionFutureId>()).Returns(call => reloadedStorage.Remove(_future.ProjectionId, call.Arg<ProjectionFutureId>()));

        _projection = Substitute.For<IProjection>();
        _projection.Identifier.Returns(_future.ProjectionId);
        _projection.TargetReadModelSchema.Returns(new JsonSchema());
        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);
        var child = Substitute.For<IProjection>();
        child.Parent.Returns(_projection);
        child.Path.Returns(new ProjectionPath("children"));
        child.ChildrenPropertyPath.Returns(children);
        child.OnNext(Arg.Do<ProjectionEventContext>(_childEvents.OnNext));
        _projection.ChildProjections.Returns([child]);

        // These are the engine's context value provider and child mapping used by SetFromContext.
        _childEvents.Project(
            children,
            new PropertyPath("id"),
            [
                PropertyMappers.FromEventValueProvider(new PropertyPath("[children].occurred"), EventValueProviders.EventContext("occurred")),
                PropertyMappers.FromEventValueProvider(new PropertyPath("[children].correlationId"), EventValueProviders.EventContext("correlationId"))
            ],
            subscriptions: _subscriptions);

        var root = new ExpandoObject();
        ((IDictionary<string, object?>)root)[WellKnownProperties.ReadModelInstanceInitialized] = true;
        var rootEvent = new AppendedEvent(
            EventContext.Empty with
            {
                SequenceNumber = 43,
                Occurred = DateTimeOffset.Parse("2026-05-13T00:00:00Z")
            },
            new ExpandoObject());
        var comparer = new ObjectComparer();
        _context = new(
            new Key("parent", ArrayIndexers.NoIndexers),
            rootEvent,
            new Changeset<AppendedEvent, ExpandoObject>(comparer, rootEvent, root),
            ProjectionOperationType.None,
            NeedsInitialState: false);
        _step = new(_futures, new ProjectionFuturesTracker(), Substitute.For<ITypeFormats>(), comparer, Substitute.For<ILogger<ResolveFutures>>());
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_project_the_original_occurred_time() => Child["occurred"].ShouldEqual(DateTimeOffset.Parse("2026-05-12T13:14:15.1234567+02:00"));
    [Fact] void should_project_the_original_correlation_id() => Child["correlationId"].ShouldEqual(new CorrelationId(Guid.Parse("de001d50-d624-4d65-a550-314241d40e07")));
    [Fact] async Task should_keep_the_future_until_it_is_saved() => (await _storage.GetForProjection(_future.ProjectionId)).Select(future => future.Id).ShouldContainOnly(_future.Id);
    [Fact] void should_carry_the_future_identity_on_the_pending_save() => _result.PendingFutureSaves.Single().FutureId.ShouldEqual(_future.Id);
    [Fact] void should_save_the_child_under_the_parent_key() => _result.PendingFutureSaves.Single().Key.Value.ShouldEqual("parent");

    IDictionary<string, object?> Child => ((IEnumerable<object>)((IDictionary<string, object?>)_result.PendingFutureSaves.Single().Changeset.CurrentState)["children"]!).OfType<ExpandoObject>().Single();

    void Destroy()
    {
        _subscriptions.Dispose();
        _childEvents.Dispose();
    }
}
