// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_adding_a_non_nullable_one_based_enum : given.additive_evolution_enabled
{
    EventWithState _readBack;

    enum State
    {
        Active = 1,
        Inactive = 2
    }

    record EventWithState(string Name, State State);

    async Task Because()
    {
        await Register("""{"type":"object","properties":{"name":{"type":"string"},"state":{"type":"integer","enum":[1,2],"x-enumNames":["Active","Inactive"]}}}""");
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var schema = _definition.Generations.Single().Schema;
        var content = converter.ToExpandoObject(JsonNode.Parse("""{"name":"before"}""").AsObject(), schema);
        _readBack = converter.ToJsonObject(content, schema).Deserialize<EventWithState>(JsonSerializerOptions.Web);
    }

    [Fact] void should_keep_the_original_property() => _readBack.Name.ShouldEqual("before");
    [Fact] void should_read_the_type_default_not_the_first_enum_member() => _readBack.State.ShouldEqual(default);
    [Fact] void should_not_assume_the_default_is_a_defined_enum_member() => Enum.IsDefined(_readBack.State).ShouldBeFalse();
}
