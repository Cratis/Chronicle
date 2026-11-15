// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_accumulating_with_unsigned_integer_type : Specification
{
    ExpandoObject _added;
    ExpandoObject _subtracted;

    void Establish()
    {
        _added = new();
        _subtracted = new();
        ((dynamic)_added).property = 42u;
        ((dynamic)_subtracted).property = 42u;
    }

    void Because()
    {
        var schema = new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "uint32" };
        var @event = given.an_appended_event.with_content(new ExpandoObject());
        PropertyMappers.AddWithEventValueProvider(new TypeFormats(), "property", schema, _ => 10u)(@event, _added, ArrayIndexers.NoIndexers);
        PropertyMappers.SubtractWithEventValueProvider(new TypeFormats(), "property", schema, _ => 10u)(@event, _subtracted, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_result_in_expected_sum() => Convert.ToUInt32((object)((dynamic)_added).property).ShouldEqual(52u);
    [Fact] void should_result_in_expected_difference() => Convert.ToUInt32((object)((dynamic)_subtracted).property).ShouldEqual(32u);
}
