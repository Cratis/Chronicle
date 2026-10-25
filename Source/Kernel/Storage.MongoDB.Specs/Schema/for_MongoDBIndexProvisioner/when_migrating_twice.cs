// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_migrating_twice(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    MongoDBIndexReport _first;
    MongoDBIndexReport _second;
    MongoDBIndexReport _verified;

    async Task Establish() => _first = await _provisioner.Migrate([Store], [Namespace]);

    async Task Because()
    {
        _commands.Clear();
        _second = await _provisioner.Migrate([Store], [Namespace]);
        _verified = await _provisioner.Verify([Store], [Namespace]);
    }

    [Fact] void should_provision_every_scope() => _first.IsCurrent.ShouldBeTrue();
    [Fact] void should_remain_current() => _second.IsCurrent.ShouldBeTrue();
    [Fact] void should_verify_after_migration() => _verified.IsCurrent.ShouldBeTrue();
    [Fact] void should_not_repeat_ddl() => DdlCommands.ShouldEqual(0);
    [Fact] void should_cover_event_store_occurrences() => _verified.Scopes.Single(scope => scope.Scope == $"event-store:{Store}").Missing.Count.ShouldEqual(0);
}
