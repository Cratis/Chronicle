// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset;

public class when_setting_properties_that_does_not_cause_changes : Specification
{
    Changeset<ExpandoObject, ExpandoObject> changeset;
    ExpandoObject initial_state;
    ExpandoObject source;
    IEnumerable<PropertyMapper<ExpandoObject, ExpandoObject>> property_mappers;
    Mock<IObjectComparer> objects_comparer;

    void Establish()
    {
        initial_state = new();

        ((dynamic)initial_state).Integer = 42;
        ((dynamic)initial_state).String = "Forty Two";
        dynamic nested = ((dynamic)initial_state).Nested = new ExpandoObject();
        nested.Integer = 43;
        nested.String = "Forty Three";

        property_mappers =
        [
                (_, target, __) =>
                    {
                        ((dynamic)target).Integer = 44;
                        return new PropertyDifference("integer", 44, 44);
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).String = "Forty Four";
                        return new PropertyDifference("string", "Forty Four", "Forty Four");
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).Nested.Integer = 45;
                        return new PropertyDifference("nested.integer", 45, 45);
                    },
                (_, target, __) =>
                    {
                        ((dynamic)target).Nested.String = "Forty Five";
                        return new PropertyDifference("nested.string", "Forty Five", "Forty Five");
                    }
        ];

        source = new ExpandoObject();

        objects_comparer = new();
        changeset = new(objects_comparer.Object, source, initial_state);
    }

    void Because() => changeset.SetProperties(property_mappers, ArrayIndexers.NoIndexers);

    [Fact] void should_not_have_any_changes() => changeset.Changes.Count().ShouldEqual(0);
}
