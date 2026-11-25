// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_building;

public class and_a_primitive_is_added_from_an_event_property : Specification
{
    public record Container(IEnumerable<string> Values);

    [EventType]
    public record ValueAdded(string Value);

    ProjectionBuilderFor<Container> _builder;
    ChildrenDefinition _result;

    void Establish() => _builder = new("values", GetType(), new CamelCaseNamingPolicy(), new EventTypesForSpecifications([typeof(ValueAdded)]), new JsonSerializerOptions());

    void Because()
    {
        _builder.From<ValueAdded>(from => from.AddChild(_ => _.Values, _ => _.Value));
        _result = _builder.Build().Children["values"];
    }

    [Fact] void should_register_the_event_property() => _result.FromEventProperty!.PropertyExpression.ShouldEqual("value");
    [Fact] void should_register_the_event_type() => _result.FromEventProperty!.Event.ToClient().ShouldEqual(typeof(ValueAdded).GetEventType());
    [Fact] void should_not_register_an_ordinary_from() => _result.From.ShouldBeEmpty();
    [Fact] void should_not_identify_the_value_as_an_object() => _result.IdentifiedBy.ShouldEqual(PropertyPath.NotSetValue);
}
