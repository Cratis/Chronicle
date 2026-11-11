// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_accumulating_with_time_span_type : Specification
{
    ExpandoObject _added;
    ExpandoObject _subtracted;

    void Establish()
    {
        _added = new();
        _subtracted = new();
        ((dynamic)_added).property = TimeSpan.FromMinutes(42);
        ((dynamic)_subtracted).property = TimeSpan.FromMinutes(42);
    }

    void Because()
    {
        var schema = new JsonSchemaProperty { Type = JsonObjectType.String, Format = "duration" };
        var @event = given.an_appended_event.with_content(new ExpandoObject());
        PropertyMappers.AddWithEventValueProvider(new TypeFormats(), "property", schema, _ => TimeSpan.FromMinutes(10))(@event, _added, ArrayIndexers.NoIndexers);
        PropertyMappers.SubtractWithEventValueProvider(new TypeFormats(), "property", schema, _ => TimeSpan.FromMinutes(10))(@event, _subtracted, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_result_in_expected_sum() => ((TimeSpan)((dynamic)_added).property).ShouldEqual(TimeSpan.FromMinutes(52));
    [Fact] void should_result_in_expected_difference() => ((TimeSpan)((dynamic)_subtracted).property).ShouldEqual(TimeSpan.FromMinutes(32));
}
