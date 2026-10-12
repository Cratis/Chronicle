// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_clients_bind_plain_migrations_in_both_orders : given.an_explicit_store
{
    EventStore _first;
    EventStore _second;
    EventStore _firstInReverse;
    EventStore _secondInReverse;
    RegisterEventTypesRequest[] _payloads;

    void Establish()
    {
        var firstOptions = new ChronicleOptions { ArtifactIdentityDrift = ArtifactIdentityDriftPolicy.Ignore };
        firstOptions.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("first").Generation(1))
            .RegisterEventType<AnotherEvent>(_ => _.WithId("first").Generation(2)).RegisterMigration<PlainRecipe>();
        var secondOptions = new ChronicleOptions { ArtifactIdentityDrift = ArtifactIdentityDriftPolicy.Ignore };
        secondOptions.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("second").Generation(4))
            .RegisterEventType<AnotherEvent>(_ => _.WithId("second").Generation(5)).RegisterMigration<PlainRecipe>();
        _options = firstOptions;
        _first = CreateStore();
        _firstInReverse = CreateStore();
        _options = secondOptions;
        _second = CreateStore();
        _secondInReverse = CreateStore();
    }

    async Task Because()
    {
        _payloads = new RegisterEventTypesRequest[4];
        var stores = new[] { _first, _second, _secondInReverse, _firstInReverse };
        for (var index = 0; index < stores.Length; index++)
        {
            await stores[index].DiscoverAll();
            await stores[index].RegisterAll();
            _payloads[index] = _payload;
        }
    }

    [Fact] void should_use_the_first_owning_registry_for_constructor_injection() => ConstructorIdentity(_payloads[0]).ShouldEqual("first");
    [Fact] void should_use_the_second_owning_registry_for_constructor_injection() => ConstructorIdentity(_payloads[1]).ShouldEqual("second");
    [Fact] void should_use_the_second_registry_in_reverse_order() => ConstructorIdentity(_payloads[2]).ShouldEqual("second");
    [Fact] void should_use_the_first_registry_in_reverse_order() => ConstructorIdentity(_payloads[3]).ShouldEqual("first");
    [Fact] void should_bind_the_first_generation_pair() => _payloads[0].Types.Single().Migrations.Single().FromGeneration.ShouldEqual(1u);
    [Fact] void should_bind_the_second_generation_pair() => _payloads[1].Types.Single().Migrations.Single().FromGeneration.ShouldEqual(4u);
    [Fact] void should_keep_both_real_generation_schemas() => _payloads[0].Types.Single().Generations.All(_ => _.Schema != "{}").ShouldBeTrue();

    static string ConstructorIdentity(RegisterEventTypesRequest payload) =>
        JsonNode.Parse(payload.Types.Single().Migrations.Single().UpcastJmesPath)!["Name"]!["$defaultValue"]!.GetValue<string>();

    public class PlainRecipe(Events.IEventTypes registry)
    {
        public void Upcast(IEventMigrationBuilder<AnotherEvent, PlainEvent> builder) =>
            builder.Properties(_ => _.DefaultValue(upgrade => upgrade.Name, registry.GetEventTypeFor(typeof(PlainEvent)).Id.Value));

        public void Downcast(IEventMigrationBuilder<PlainEvent, AnotherEvent> builder) =>
            builder.Properties(_ => _.DefaultValue(previous => previous.Name, registry.GetEventTypeFor(typeof(PlainEvent)).Id.Value));
    }
}
