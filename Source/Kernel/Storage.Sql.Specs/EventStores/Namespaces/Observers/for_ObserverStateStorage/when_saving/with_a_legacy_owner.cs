// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving;

public class with_a_legacy_owner : given.an_observer_state_storage
{
    bool _accepted;
    KernelObserverState _stored;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.Observers.Add(new KernelObserverState { Identifier = "legacy" }.ToSql());
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        _accepted = await _storage.Save(new() { Identifier = "legacy", LastHandledEventSequenceNumber = 42 }, EventSequenceId.Inbox);
        _stored = await _storage.Get("legacy");
    }

    [Fact] void should_accept_the_legacy_record() => _accepted.ShouldBeTrue();
    [Fact] void should_stamp_the_inbox_owner() => _stored.Owner.ShouldEqual(EventSequenceId.Inbox);
    [Fact] void should_save_the_progress() => _stored.LastHandledEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)42);
}
