// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_a_property_overrides_an_attributed_member : Specification
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = new given.PrefixJsonPolicy(),
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
    EventMigrationBuilder _builder;
    JsonObject _result;
    string _serializedName;

    void Establish()
    {
        var policy = new given.PrefixPolicy();
        _builder = new(policy);
        _serializedName = _serializerOptions.GetTypeInfo(typeof(Derived)).Properties
            .Single(property => property.AttributeProvider is PropertyInfo member && member.DeclaringType == typeof(Derived)).Name;
    }

    void Because()
    {
        new EventMigrationBuilderFor<Derived, Derived>(_builder).Properties(properties =>
            properties.RenamedFrom(target => target.Name, source => source.Name));
        _result = _builder.ToJson();
    }

    [Fact] void should_use_the_same_target_name_as_the_serializer() => _result.ContainsKey(_serializedName).ShouldBeTrue();
    [Fact] void should_use_the_same_source_name_as_the_serializer() => _result[_serializedName]!["$rename"]!.GetValue<string>().ShouldEqual(_serializedName);

    class Base
    {
        [JsonPropertyName("BaseWireName")]
        public virtual string Name { get; set; } = "base";
    }

    class Derived : Base
    {
        public override string Name { get; set; } = "derived";
    }
}
