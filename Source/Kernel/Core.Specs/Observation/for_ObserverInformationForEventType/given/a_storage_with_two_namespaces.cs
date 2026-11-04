// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverInformationForEventType.given;

public class a_storage_with_two_namespaces : Specification
{
    protected const string EventStoreName = "some-store";
    protected const string EventTypeId = "some-event-type";

    protected IStorage _storage;
    protected int _activeSubscriptions;
    protected int _totalSubscriptions;
    protected Subject<IEnumerable<NamespaceState>> _namespaceChanges;
    protected Subject<IEnumerable<ObserverDefinition>> _definitionChanges;

    void Establish()
    {
        _namespaceChanges = new();
        _definitionChanges = new();
        _storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);

        eventStore.Namespaces.ObserveAll().Returns(_ => Tracked(
            _namespaceChanges,
            new NamespaceState("ns-a", DateTimeOffset.UtcNow),
            new NamespaceState("ns-b", DateTimeOffset.UtcNow)));
        eventStore.Observers.ObserveAll().Returns(_ => Tracked(_definitionChanges, Definition()));
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_ =>
        {
            var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
            @namespace.Observers.ObserveAll().Returns(__ => Tracked(new Subject<IEnumerable<ObserverState>>()));
            return @namespace;
        });
    }

    static ObserverDefinition Definition() => new(
        "some-observer",
        [new Concepts.Events.EventType(EventTypeId, 1)],
        Concepts.EventSequences.EventSequenceId.Log,
        Concepts.Observation.ObserverType.Reactor,
        Concepts.Observation.ObserverOwner.Client,
        false);

    ISubject<IEnumerable<T>> Tracked<T>(Subject<IEnumerable<T>> changes, params T[] initial) =>
        Subject.Create<IEnumerable<T>>(
            System.Reactive.Observer.Create<IEnumerable<T>>(_ => { }),
            Observable.Create<IEnumerable<T>>(observer =>
            {
                Interlocked.Increment(ref _activeSubscriptions);
                Interlocked.Increment(ref _totalSubscriptions);
                observer.OnNext(initial);
                var subscription = changes.Subscribe(observer);
                return Disposable.Create(() =>
                {
                    subscription.Dispose();
                    Interlocked.Decrement(ref _activeSubscriptions);
                });
            }));

    ISubject<IEnumerable<T>> Tracked<T>(Subject<IEnumerable<T>> changes) => Tracked(changes, []);
}
