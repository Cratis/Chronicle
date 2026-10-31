// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators.when_binding_to_a_store;

public class and_the_explicit_upgrade_is_unreachable : given.store_bindings
{
    Exception _error;

    void Establish() => _eventTypes.HasFor(typeof(Upgrade)).Returns(false);

    void Because() => _error = Catch.Exception(() => StoreEventTypeMigrators.Bind(new EventTypeMigrators(_discovered, _services), _catalog, _eventTypes, _services, new DefaultNamingPolicy()));

    [Fact] void should_fail_eager_validation() => _error.ShouldBeOfExactType<InvalidExplicitArtifact>();
    [Fact] void should_not_activate_an_unreachable_recipe() => _activation.DidNotReceive().Record(Arg.Any<IEventTypes>());
}
