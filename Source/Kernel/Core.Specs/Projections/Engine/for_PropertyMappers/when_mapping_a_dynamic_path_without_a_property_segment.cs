// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_mapping_a_dynamic_path_without_a_property_segment : Specification
{
    Exception _creationException;
    Exception _noPropertyException;
    Exception _trailingDotException;
    Exception _unsupportedNumericException;

    static string Message(string path) => $"Property path '{path}' contains a dynamic segment that could not be resolved. Supported dynamic segments are of the form '.$eventContext.<event context path>', '.$eventContent.<event property path>' and '.$causedBy.(subject|name|userName)'.";

    void Because()
    {
        var mappers = new List<PropertyMapper<Cratis.Chronicle.Concepts.Events.AppendedEvent, ExpandoObject>>();
        _creationException = Catch.Exception(() =>
        {
            mappers.Add(PropertyMappers.FromEventValueProvider(new PropertyPath("counts.$eventContext"), _ => 1));
            mappers.Add(PropertyMappers.FromEventValueProvider(new PropertyPath("counts.$eventContext."), _ => 1));
            mappers.Add(PropertyMappers.Count(new TypeFormats(), "counts.$bogus.x", new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "int64" }));
        });
        var @event = given.an_appended_event.with_content(new ExpandoObject());
        _noPropertyException = Catch.Exception(() => mappers[0](@event, new ExpandoObject(), ArrayIndexers.NoIndexers));
        _trailingDotException = Catch.Exception(() => mappers[1](@event, new ExpandoObject(), ArrayIndexers.NoIndexers));
        _unsupportedNumericException = Catch.Exception(() => mappers[2](@event, new ExpandoObject(), ArrayIndexers.NoIndexers));
    }

    [Fact] void should_not_throw_when_creating_the_mappers() => _creationException.ShouldBeNull();
    [Fact] void should_reject_a_path_without_a_property_segment_at_event_time() => _noPropertyException.ShouldBeOfExactType<UnsupportedDynamicPropertyPathExpression>();
    [Fact] void should_reject_a_path_ending_in_a_dot_at_event_time() => _trailingDotException.ShouldBeOfExactType<UnsupportedDynamicPropertyPathExpression>();
    [Fact] void should_reject_an_unknown_expression_at_event_time() => _unsupportedNumericException.ShouldBeOfExactType<UnsupportedDynamicPropertyPathExpression>();
    [Fact] void should_name_the_path_in_the_message_without_a_property() => _noPropertyException.Message.ShouldEqual(Message("counts.$eventContext"));
    [Fact] void should_name_the_path_in_the_message_ending_in_a_dot() => _trailingDotException.Message.ShouldEqual(Message("counts.$eventContext."));
}
