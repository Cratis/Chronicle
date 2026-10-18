// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving;

public class with_existing_null_control : given.an_observer_state_storage
{
    KernelObserverState _saved;

    async Task Establish() => await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(10), LastHandledEventSequenceNumber = new(9), TailEventSequenceNumber = new(20) });

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(123), LastHandledEventSequenceNumber = new(122), TailEventSequenceNumber = new(200) });
        _saved = await _storage.Get("observer");
    }

    [Fact] void should_keep_control_absent() => _saved.TypeSetControl.ShouldBeNull();
    [Fact] void should_keep_initialization_absent() => _saved.Initialization.ShouldBeNull();
    [Fact] void should_update_the_next_position() => _saved.NextEventSequenceNumber.Value.ShouldEqual(123UL);
    [Fact] void should_update_the_last_handled_position() => _saved.LastHandledEventSequenceNumber.Value.ShouldEqual(122UL);
    [Fact] void should_update_the_tail() => _saved.TailEventSequenceNumber.Value.ShouldEqual(200UL);
}
