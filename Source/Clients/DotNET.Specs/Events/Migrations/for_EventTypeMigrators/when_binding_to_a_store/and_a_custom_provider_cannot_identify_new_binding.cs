// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators.when_binding_to_a_store;

public class and_a_custom_provider_cannot_identify_new_binding : given.store_bindings
{
    IEventTypeMigrators _custom;
    Exception _error;

    void Establish()
    {
        _custom = Substitute.For<IEventTypeMigrators>();
        _custom.AllMigrators.Returns([typeof(PlainMigration)]);
        _custom.GetMigratorsFor(typeof(Upgrade)).Returns([Substitute.For<IEventTypeMigration>()]);
    }

    void Because()
    {
        _bound = StoreEventTypeMigrators.Bind(_custom, _catalog, _eventTypes, _services, new DefaultNamingPolicy());
        _error = Catch.Exception(() => _bound.GetMigratorsFor(typeof(Upgrade)));
    }

    [Fact] void should_refuse_only_the_opaque_new_binding() => _error.ShouldBeOfExactType<ExplicitMigrationBindingNotSupported>();
}
