// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_accumulating_with_byte_type : Specification
{
    ExpandoObject _added;
    ExpandoObject _subtracted;

    void Establish()
    {
        _added = new();
        _subtracted = new();
        ((dynamic)_added).property = (byte)42;
        ((dynamic)_subtracted).property = (byte)42;
    }

    void Because()
    {
        var schema = new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "byte" };
        var @event = given.an_appended_event.with_content(new ExpandoObject());
        PropertyMappers.AddWithEventValueProvider(new TypeFormats(), "property", schema, _ => (byte)10)(@event, _added, ArrayIndexers.NoIndexers);
        PropertyMappers.SubtractWithEventValueProvider(new TypeFormats(), "property", schema, _ => (byte)10)(@event, _subtracted, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_add_as_byte() => ((object)((dynamic)_added).property).ShouldBeOfExactType<byte>();
    [Fact] void should_result_in_expected_sum() => ((byte)((dynamic)_added).property).ShouldEqual((byte)52);
    [Fact] void should_subtract_as_byte() => ((object)((dynamic)_subtracted).property).ShouldBeOfExactType<byte>();
    [Fact] void should_result_in_expected_difference() => ((byte)((dynamic)_subtracted).property).ShouldEqual((byte)32);
}
