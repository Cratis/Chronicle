// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages;

/// <summary>
/// Callers complete the subject they are handed when their connection goes away - see
/// <see cref="Reactive.ObservableExtensions.CompletedBy{TResult}"/>. Handing out a shared subject therefore let the
/// first caller to disconnect end event-store observation for every other caller in the process.
/// </summary>
public class when_one_observer_completes_its_subject : given.an_empty_registry
{
    static readonly EventStoreName _first = "FirstEventStore";
    static readonly EventStoreName _second = "SecondEventStore";
    readonly List<IEnumerable<EventStoreName>> _received = [];

    void Establish()
    {
        _storages.GetOrCreate(_first);

        var leaving = _storages.Observe();
        _storages.Observe().Subscribe(_received.Add);
        leaving.OnCompleted();
    }

    void Because() => _storages.GetOrCreate(_second);

    [Fact] void should_keep_notifying_the_remaining_observer() => _received.Count.ShouldEqual(2);

    [Fact] void should_still_report_every_event_store() => _received[^1].ShouldContainOnly(_first, _second);

    [Fact] void should_still_seed_an_observer_subscribing_afterwards()
    {
        var received = new List<IEnumerable<EventStoreName>>();
        _storages.Observe().Subscribe(received.Add);
        received[0].ShouldContainOnly(_first, _second);
    }
}
