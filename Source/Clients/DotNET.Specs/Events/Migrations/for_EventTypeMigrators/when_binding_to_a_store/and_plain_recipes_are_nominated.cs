// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators.when_binding_to_a_store;

public class and_plain_recipes_are_nominated : given.store_bindings
{
    EventTypeMigrators _original;
    IEventTypeMigration[] _migrations;
    IEventTypeMigration[] _again;

    void Establish() => _original = new(_discovered, _services);

    void Because()
    {
        var bound = _original.ForStore(_catalog, _eventTypes);
        bound.ValidateExplicitMigrations(new DefaultNamingPolicy());
        _bound = bound;
        _migrations = bound.GetMigratorsFor(typeof(Upgrade)).ToArray();
        _again = bound.GetMigratorsFor(typeof(Upgrade)).ToArray();
    }

    [Fact] void should_coalesce_repeated_nominations() => _migrations.Length.ShouldEqual(1);
    [Fact] void should_activate_once_for_this_revision() => _activation.Received(1).Record(_eventTypes);
    [Fact] void should_keep_original_recipe_types_in_the_catalog() => _bound.AllMigrators.ShouldContainOnly(typeof(PlainMigration));
    [Fact] void should_reuse_the_store_local_adapter() => ReferenceEquals(_migrations[0], _again[0]).ShouldBeTrue();
    [Fact] void should_leave_client_shared_discovery_unchanged() => _original.AllMigrators.ShouldBeEmpty();
    [Fact] void should_not_extend_the_client_shared_cache() => _original.GetMigratorsFor(typeof(Upgrade)).ShouldBeEmpty();
}
