// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.for_EventStore.when_computing_the_definitions_fingerprint;

public class and_native_migrators_are_bound_to_the_snapshot : when_preparing_for_registration.given.an_explicit_store
{
    Events.EventTypes _live;
    Events.EventTypes _detached;
    RegisterEventTypesRequest _before;
    RegisterEventTypesRequest _snapshot;
    RegisterEventTypesRequest _after;
    IEventTypeMigration _liveMigrator;
    Schemas.JsonSchema _liveSchema;
    bool _preserved;
    bool _fingerprintParity;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("bound").Generation(1))
            .RegisterEventType<AnotherEvent>(_ => _.WithId("bound").Generation(2)).RegisterMigration<Recipe>();
        _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>()).Returns(Contracts.Commands.CommandResult.Success(Guid.NewGuid()));
        _store = CreateStore();
        await _store.DiscoverAll();
        _live = (Events.EventTypes)_store.EventTypes;
        _before = await _live.PrepareRegistration();
        _liveMigrator = _live.PreparedMigrators.GetMigratorsFor(typeof(AnotherEvent)).Single();
        _liveSchema = _live.GetSchemaFor((EventTypeId)"bound");
    }

    async Task Because()
    {
        _generator.BeforeGenerate = (_, registry, _) =>
        {
            _detached = (Events.EventTypes)registry;
            registry.GetSchemaFor((EventTypeId)"bound").Node["snapshotOnly"] = true;
        };
        _snapshot = await _live.BuildDeploymentPayloadSnapshot();
        _preserved = ReferenceEquals(_liveSchema, _live.GetSchemaFor((EventTypeId)"bound"));
        _generator.BeforeGenerate = null;
        _after = await _live.PrepareRegistration();
        var fingerprint = await _store.ComputeDefinitionsFingerprint();
        _fingerprintParity = fingerprint == (await _store.RegisterDefinitions()).Fingerprint;
    }

    [Fact] void should_match_the_registered_fingerprint_with_native_migrations() => _fingerprintParity.ShouldBeTrue();
    [Fact] void should_leave_live_schemas_untouched() => _preserved.ShouldBeTrue();
    [Fact] void should_clone_the_schema_nodes() => _liveSchema.Node.ContainsKey("snapshotOnly").ShouldBeFalse();
    [Fact] void should_leave_the_live_migrator_instance_untouched() => _live.PreparedMigrators.GetMigratorsFor(typeof(AnotherEvent)).Single().ShouldBeSame(_liveMigrator);
    [Fact] void should_bind_an_independent_migrator() => ReferenceEquals(_detached.PreparedMigrators.GetMigratorsFor(typeof(AnotherEvent)).Single(), _liveMigrator).ShouldBeFalse();
    [Fact] void should_match_the_normal_migration() => _snapshot.Types.Single().Migrations.Single().UpcastJmesPath.ShouldEqual(_before.Types.Single().Migrations.Single().UpcastJmesPath);
    [Fact] void should_preserve_the_next_normal_payload() => _after.Types.Single().Schema.ShouldEqual(_before.Types.Single().Schema);
    [Fact] void should_keep_both_generation_schemas() => _snapshot.Types.Single().Generations.Count.ShouldEqual(2);

    public class Recipe(Events.IEventTypes registry) : EventTypeMigration<AnotherEvent, PlainEvent>(registry)
    {
        readonly Events.IEventTypes _registry = registry;

        public override void Upcast(IEventMigrationBuilder<AnotherEvent, PlainEvent> builder) => builder.Properties(_ => _.DefaultValue(upgrade => upgrade.Name, _registry.GetEventTypeFor(typeof(PlainEvent)).Id.Value));
        public override void Downcast(IEventMigrationBuilder<PlainEvent, AnotherEvent> builder) => builder.Properties(_ => _.DefaultValue(previous => previous.Name, _registry.GetEventTypeFor(typeof(PlainEvent)).Id.Value));
    }
}
