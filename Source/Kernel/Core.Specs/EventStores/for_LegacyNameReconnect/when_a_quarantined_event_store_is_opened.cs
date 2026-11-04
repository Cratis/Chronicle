// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.EventStores.for_LegacyNameReconnect;

public class when_a_quarantined_event_store_is_opened : given.a_legacy_installation
{
    Exception _error;

    void Establish() => _snapshot.Publish([_plainStore], []);

    void Because() => _error = Catch.Exception(() => _storage.GetEventStore(_plainStore));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<ResourceQuarantined>();
}
