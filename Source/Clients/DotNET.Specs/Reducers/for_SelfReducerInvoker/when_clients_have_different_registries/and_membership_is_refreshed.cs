// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_clients_have_different_registries;

public class and_membership_is_refreshed : given.emitted_methods
{
    SelfReducerInvoker _before;
    SelfReducerInvoker _after;

    void Because()
    {
        var registry = Registry(typeof(First));
        _before = new(registry, typeof(State), "state");
        registry.AllClrTypes.Returns(ImmutableList.Create(typeof(Second), typeof(First)));
        registry.GetEventTypeFor(typeof(First)).Returns(new EventType("new-alias", 3));
        _after = new(registry, typeof(State), "state");
    }

    [Fact] void should_preserve_the_existing_invoker_snapshot() => _before.EventTypes.Select(type => type.Id.Value).ShouldContainOnly(nameof(First));
    [Fact] void should_expand_the_new_registry_membership() => _after.EventTypes.Select(type => type.Id.Value).ShouldContainOnly("new-alias", nameof(Second));
    [Fact] void should_resolve_new_generation_identities() => _after.EventTypes.Single(type => type.Id.Value == "new-alias").Generation.Value.ShouldEqual(3U);

    public record First : IRoot;
    public record Second : IRoot;
    public record State
    {
        public static State Reduce(IRoot @event, State? current) => new();
    }
}
