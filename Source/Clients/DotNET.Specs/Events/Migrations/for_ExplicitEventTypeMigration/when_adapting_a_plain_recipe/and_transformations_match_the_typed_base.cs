// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.Migrations.for_ExplicitEventTypeMigration.when_adapting_a_plain_recipe;

public class and_transformations_match_the_typed_base : Specification
{
    IEventTypes _registry;
    ServiceProvider _services;
    IEventTypeMigration _plain;
    IEventTypeMigration _typed;
    JsonObject[] _upcast;
    JsonObject[] _downcast;
    JsonObject[] _typedUpcast;
    JsonObject[] _typedDowncast;

    void Establish()
    {
        _registry = Substitute.For<IEventTypes>();
        _registry.GetEventTypeFor(typeof(Previous)).Returns(new EventType("matrix", 1));
        _registry.GetEventTypeFor(typeof(Current)).Returns(new EventType("matrix", 2));
        _services = new ServiceCollection().BuildServiceProvider();
        _plain = ExplicitMigrationMethods.For(typeof(Recipe)).Create(_services, _registry);
        _typed = (IEventTypeMigration)ActivatorUtilities.CreateInstance(_services, given.TypedMigrationCounterpart.For(typeof(Recipe), typeof(Current), typeof(Previous)), _registry);
    }

    void Because()
    {
        INamingPolicy[] policies = [new DefaultNamingPolicy(), new CamelCaseNamingPolicy(), new PrefixPolicy()];
        _upcast = policies.Select(policy => Build(_plain, policy, true)).ToArray();
        _downcast = policies.Select(policy => Build(_plain, policy, false)).ToArray();
        _typedUpcast = policies.Select(policy => Build(_typed, policy, true)).ToArray();
        _typedDowncast = policies.Select(policy => Build(_typed, policy, false)).ToArray();
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_match_typed_upcasts_under_every_policy() => _upcast.Zip(_typedUpcast).All(pair => JsonNode.DeepEquals(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_match_typed_downcasts_under_every_policy() => _downcast.Zip(_typedDowncast).All(pair => JsonNode.DeepEquals(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_split_the_attributed_nested_source() => _upcast[1]["WireCurrent.firstName"]!["$split"]!["source"]!.GetValue<string>().ShouldEqual("WirePrevious.FullWire");
    [Fact] void should_keep_both_split_parts() => _upcast[1]["WireCurrent.lastName"]!["$split"]!["part"]!.GetValue<int>().ShouldEqual(1);
    [Fact] void should_combine_the_nested_target_paths() => _downcast[1]["WirePrevious.FullWire"]!["$combine"]!["sources"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldContainOnly("WireCurrent.firstName", "WireCurrent.lastName");
    [Fact] void should_rename_between_explicit_json_names() => _upcast[2]["WireCurrent.NewAlias"]!["$rename"]!.GetValue<string>().ShouldEqual("WirePrevious.OldAlias");
    [Fact] void should_keep_default_literals_unchanged() => _upcast[2]["WireCurrent.mapped_Added"]!["$defaultValue"]!.GetValue<string>().ShouldEqual("literal_Value");
    [Fact] void should_apply_the_non_idempotent_policy_once_per_unattributed_segment() => _upcast[2].ContainsKey("WireCurrent.mapped_FirstName").ShouldBeTrue();
    [Fact] void should_apply_the_forward_bidirectional_map() => Mapping(_upcast[1], "WireCurrent.status")[0]!["to"]!.GetValue<string>().ShouldEqual("new");
    [Fact] void should_invert_the_bidirectional_map() => Mapping(_downcast[1], "WirePrevious.status")[0]!["to"]!.GetValue<string>().ShouldEqual("old");
    [Fact] void should_keep_many_to_one_forward_mappings() => Mapping(_upcast[1], "WireCurrent.collapsed").Count.ShouldEqual(2);
    [Fact] void should_choose_the_first_inverse_for_many_to_one() => Mapping(_downcast[1], "WirePrevious.collapsed")[0]!["to"]!.GetValue<string>().ShouldEqual("first");
    [Fact] void should_coalesce_many_to_one_inverse_mappings() => Mapping(_downcast[1], "WirePrevious.collapsed").Count.ShouldEqual(1);
    [Fact] void should_keep_the_explicit_upcast_override() => Mapping(_upcast[1], "WireCurrent.direction")[0]!["to"]!.GetValue<string>().ShouldEqual("up-override");
    [Fact] void should_keep_the_explicit_downcast_override() => Mapping(_downcast[1], "WirePrevious.direction")[0]!["to"]!.GetValue<string>().ShouldEqual("down-override");
    [Fact] void should_match_typed_endpoint_generations() => (_plain.From == _typed.From && _plain.To == _typed.To).ShouldBeTrue();

    static JsonArray Mapping(JsonObject definition, string path) => definition[path]!["$mapValues"]!["mappings"]!.AsArray();

    static JsonObject Build(IEventTypeMigration migration, INamingPolicy policy, bool upcast)
    {
        var builder = new EventMigrationBuilder(policy);
        if (upcast) migration.Upcast(builder);
        else migration.Downcast(builder);
        return builder.ToJson();
    }

    public record Previous([property: JsonPropertyName("WirePrevious")] PreviousDetails Contact);
    public record Current([property: JsonPropertyName("WireCurrent")] CurrentDetails Contact);
    public record PreviousDetails([property: JsonPropertyName("FullWire")] string FullName, [property: JsonPropertyName("OldAlias")] string Alias, string Status, string Collapsed, string Direction);
    public record CurrentDetails(string FirstName, string LastName, [property: JsonPropertyName("NewAlias")] string Alias, string Added, string Status, string Collapsed, string Direction);

    public class Recipe
    {
        public void Upcast(IEventMigrationBuilder<Current, Previous> builder) => builder.Properties(properties => properties
            .Split(current => current.Contact.FirstName, previous => previous.Contact.FullName, " ", SplitPartIndex.First)
            .Split(current => current.Contact.LastName, previous => previous.Contact.FullName, " ", SplitPartIndex.Second)
            .RenamedFrom(current => current.Contact.Alias, previous => previous.Contact.Alias)
            .DefaultValue(current => current.Contact.Added, "literal_Value")
            .MapValues(current => current.Contact.Direction, previous => previous.Contact.Direction, map => map.Map("old", "up-override")));

        public void Downcast(IEventMigrationBuilder<Previous, Current> builder) => builder.Properties(properties => properties
            .Combine(previous => previous.Contact.FullName, " ", current => current.Contact.FirstName, current => current.Contact.LastName)
            .RenamedFrom(previous => previous.Contact.Alias, current => current.Contact.Alias)
            .MapValues(previous => previous.Contact.Direction, current => current.Contact.Direction, map => map.Map("new", "down-override")));

        public void MapValues(IEventValueMapBuilder<Current, Previous> builder)
        {
            builder.For(current => current.Contact.Status, previous => previous.Contact.Status, map => map.Map("old", "new"))
                .For(current => current.Contact.Collapsed, previous => previous.Contact.Collapsed, map => map.Map("first", "one").Map("second", "one"))
                .For(current => current.Contact.Direction, previous => previous.Contact.Direction, map => map.Map("old", "new"));
        }
    }
}
