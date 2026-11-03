// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_opening_before_the_names_are_checked : given.a_gate
{
    Exception _store;
    Exception _system;
    Exception _hostile;

    void Establish() => _gate = new(ResourceNameSnapshot.NotReady(), _reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);

    void Because()
    {
        _store = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("orders")));
        _system = Catch.Exception(() => _gate.EnsureCanOpen(EventStoreName.System));
        _hostile = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("a#b")));
    }

    [Fact] void should_refuse_the_store() => _store.ShouldBeOfExactType<ResourceNamesNotReady>();
    [Fact] void should_allow_the_system_store() => _system.ShouldBeNull();
    [Fact] void should_apply_the_rule_first() => _hostile.ShouldBeOfExactType<InvalidResourceName>();
}
