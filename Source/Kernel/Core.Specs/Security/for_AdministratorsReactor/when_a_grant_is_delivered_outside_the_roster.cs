// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_a_grant_is_delivered_outside_the_roster : given.an_administrators_reactor
{
    EventContext[] _contexts;
    IEnumerable<Administrator> _privileges;

    void Establish()
    {
        var roster = SeedGrant();
        _contexts =
        [
            roster with { EventStore = "tenant-store" },
            Seed(_userId, _grant),
            roster with { Namespace = "tenant" },
            roster with { EventStreamType = "another-stream-type" },
            roster with { EventStreamId = "another-stream" }
        ];
    }

    async Task Because()
    {
        foreach (var context in _contexts) await _reactor.Granted(_grant, context);
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_not_change_administrator_storage() => _privileges.ShouldBeEmpty();
    [Fact] void should_not_read_the_authoritative_roster() => _events.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_subscribe_only_to_the_system_store() => typeof(AdministratorsReactor).IsSystemEventStoreOnly().ShouldBeTrue();
    [Fact] void should_subscribe_only_to_the_default_namespace() => typeof(AdministratorsReactor).IsDefaultNamespaceOnly().ShouldBeTrue();
}
