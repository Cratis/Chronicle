// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_mapping_a_static_path_never_resolving_a_dynamic_segment : Specification
{
    ExpandoObject _setTarget;
    ExpandoObject _addTarget;
    PropertyDifference _setDifference;
    PropertyDifference _addDifference;

    void Establish()
    {
        _setTarget = new();
        _addTarget = new();
        ((IDictionary<string, object?>)_addTarget)["total"] = 5;
    }

    void Because()
    {
        var @event = given.an_appended_event.with_content(new ExpandoObject());
        _setDifference = PropertyMappers.FromEventValueProvider(new PropertyPath("a.b.c"), _ => "value")(@event, _setTarget, ArrayIndexers.NoIndexers);
        _addDifference = PropertyMappers.AddWithEventValueProvider(new TypeFormats(), new PropertyPath("total"), new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "int32" }, _ => 3)(@event, _addTarget, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_set_the_static_path_value() => _setDifference.Changed.ShouldEqual("value");
    [Fact] void should_report_the_static_path_as_the_changed_property() => _setDifference.PropertyPath.ShouldEqual(new PropertyPath("a.b.c"));
    [Fact] void should_add_to_the_existing_value() => _addDifference.Changed.ShouldEqual(8);
    [Fact] void should_report_the_original_value_when_adding() => _addDifference.Original.ShouldEqual(5);
}
