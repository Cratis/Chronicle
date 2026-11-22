// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_ensuring_a_new_namespace : given.an_owner
{
    EventStoreNamespaceName _canonical;

    async Task Because() => _canonical = await _owner.Ensure(_name);

    [Fact] void should_return_the_display_casing() => _canonical.ShouldEqual(_name);
    [Fact] void should_commit_open_last() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] void should_persist_the_listing() => _listing.Count.ShouldEqual(1);
    [Fact] void should_clear_the_acceleration_timer_after_terminal_commit() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
