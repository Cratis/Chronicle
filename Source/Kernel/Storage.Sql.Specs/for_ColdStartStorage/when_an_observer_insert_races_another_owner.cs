// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_an_observer_insert_races_another_owner : given.a_cold_start_database
{
    readonly EventSequenceId _otherOwner = "another-sequence";
    KernelObserverState _saved;
    bool _accepted;
    bool _winnerAccepted;

    async Task Because()
    {
        var storage = new ObserverStateStorage(Store, Namespace, _database);
        await Race(
            async () => _accepted = await storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(42) }, EventSequenceId.Log),
            async () => _winnerAccepted = await storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(7) }, _otherOwner));
        _saved = await storage.Get("observer");
    }

    [Fact] void should_accept_exactly_one_call() => (_accepted ^ _winnerAccepted).ShouldBeTrue();
    [Fact] void should_keep_the_accepted_calls_owner() => _saved.Owner.ShouldEqual(_winnerAccepted ? _otherOwner : EventSequenceId.Log);
    [Fact] void should_keep_the_accepted_calls_progress() => _saved.NextEventSequenceNumber.Value.ShouldEqual(_winnerAccepted ? 7UL : 42UL);
}
