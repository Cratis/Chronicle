// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset;

public class when_setting_properties_that_cause_changes : Specification
{
    Changeset<ExpandoObject, ExpandoObject> _changeset;
    ExpandoObject _initialState;
    ExpandoObject _source;
    IEnumerable<PropertyMapper<ExpandoObject, ExpandoObject>> _propertyMappers;
    IObjectComparer _objectsComparer;

    void Establish()
    {
        _initialState = new();

        ((dynamic)_initialState).Integer = 42;
        ((dynamic)_initialState).String = "Forty Two";
        dynamic nested = ((dynamic)_initialState).Nested = new ExpandoObject();
        nested.Integer = 43;
        nested.String = "Forty Three";

        _propertyMappers =
        [
                (_, target, __) =>
                    {
                        ((dynamic)target).Integer = 44;
                        return new PropertyDifference("integer", 42, 44);
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).String = "Forty Four";
                        return new PropertyDifference("string", "Forty Two", "Forty Four");
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).Nested.Integer = 45;
                        return new PropertyDifference("nested.integer", 43, 45);
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).Nested.String = "Forty Five";
                        return new PropertyDifference("nested.string", "Forty Three", "Forty Five");
                    }
        ];

        _source = new ExpandoObject();

        _objectsComparer = Substitute.For<IObjectComparer>();
        _changeset = new(_objectsComparer, _source, _initialState);
    }

    void Because() => _changeset.SetProperties(_propertyMappers, ArrayIndexers.NoIndexers);

    [Fact] void should_add_one_change_of_correct_type() => _changeset.Changes.First().ShouldBeOfExactType<PropertiesChanged<ExpandoObject>>();
    [Fact] void should_add_a_property_diff_for_top_level_integer() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.First()).Differences.ToArray()[0].PropertyPath.Path.ShouldEqual("integer");
    [Fact] void should_add_a_property_diff_for_top_level_integers_value() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.First()).Differences.ToArray()[0].Changed.ShouldEqual(44);
    [Fact] void should_add_a_property_diff_for_top_level_string() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.First()).Differences.ToArray()[1].PropertyPath.Path.ShouldEqual("string");
    [Fact] void should_add_a_property_diff_for_top_level_strings_value() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.First()).Differences.ToArray()[1].Changed.ShouldEqual("Forty Four");
    [Fact] void should_add_a_property_diff_for_nested_integer() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.First()).Differences.ToArray()[2].PropertyPath.Path.ShouldEqual("nested.integer");
    [Fact] void should_add_a_property_diff_for_nested_integers_value() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.First()).Differences.ToArray()[2].Changed.ShouldEqual(45);
    [Fact] void should_add_a_property_diff_for_nested_string() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.First()).Differences.ToArray()[3].PropertyPath.Path.ShouldEqual("nested.string");
    [Fact] void should_add_a_property_diff_for_nested_strings_value() => ((PropertiesChanged<ExpandoObject>)_changeset.Changes.First()).Differences.ToArray()[3].Changed.ShouldEqual("Forty Five");
}
