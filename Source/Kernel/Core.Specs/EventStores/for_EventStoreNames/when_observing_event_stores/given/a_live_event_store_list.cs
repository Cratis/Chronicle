// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores.given;

public class a_live_event_store_list : Specification
{
    protected IStorage _storage;
    protected IEventStoreInitialization _initialization;
    protected Subject<IEnumerable<EventStoreName>> _storedSets;
    protected retry_scheduler _scheduler;
    protected List<IEnumerable<EventStoreNames>> _emissions;
    protected List<Exception> _errors;
    protected IDisposable _subscription;
    protected EventStoreName[] _stores = ["ready", "not-ready"];

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _initialization = Substitute.For<IEventStoreInitialization>();
        _initialization.EnsureAll(Arg.Any<IEnumerable<EventStoreName>>()).Returns(call => call.Arg<IEnumerable<EventStoreName>>());
        _storedSets = new();
        _storage.ObserveEventStores().Returns(_storedSets);
        _scheduler = new();
        _emissions = [];
        _errors = [];
    }

    protected virtual IScheduler RetryScheduler => _scheduler;

    protected void Observe() => _subscription = LiveEventStoreNames.Observe(_storage, _initialization, RetryScheduler).Subscribe(_emissions.Add, _errors.Add);

    void Destroy()
    {
        _subscription?.Dispose();
        _storedSets.Dispose();
    }
}
