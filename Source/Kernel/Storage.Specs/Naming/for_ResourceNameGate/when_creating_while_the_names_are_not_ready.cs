// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_creating_while_the_names_are_not_ready : given.a_gate
{
    Exception _store;
    Exception _namespace;
    Exception _system;

    void Establish() => _gate = new(ResourceNameSnapshot.NotReady(), _reservations, ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);

    async Task Because()
    {
        _store = await Catch.Exception(() => _gate.EnsureCanCreate(Store("orders")));
        _namespace = await Catch.Exception(() => _gate.EnsureCanCreate(Namespace("orders", "tenant")));
        _system = await Catch.Exception(() => _gate.EnsureCanCreate(new ResourceCreation(EventStoreName.System, null, [])));
    }

    [Fact] void should_refuse_a_new_event_store() => _store.ShouldBeOfExactType<ResourceNamesNotReady>();
    [Fact] void should_refuse_a_new_namespace() => _namespace.ShouldBeOfExactType<ResourceNamesNotReady>();
    [Fact] void should_allow_the_system_event_store() => _system.ShouldBeNull();
    [Fact] void should_only_reserve_for_the_system_event_store() => _reservations.Received(1).Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
}
