// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Changes.for_Changeset;

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

        property_mappers = new PropertyMapper<ExpandoObject, ExpandoObject>[]
        {
                (_, target, __) => ((dynamic)target).Integer = 42,
                (_, target, __) => ((dynamic)target).String = "Forty Two",
                (_, target, __) => ((dynamic)target).Nested.Integer = 43,
                (_, target, __) => ((dynamic)target).Nested.String = "Forty Three",
        };

        source = new ExpandoObject();

        objects_comparer = new();
        objects_comparer.Setup(_ => _.Equals(initial_state, IsAny<ExpandoObject>(), out Ref<IEnumerable<PropertyDifference>>.IsAny)).Returns(true);
        changeset = new(objects_comparer.Object, source, initial_state);
    }

    void Because() => changeset.SetProperties(property_mappers, ArrayIndexers.NoIndexers);

    [Fact] void should_not_have_any_changes() => changeset.Changes.Count().ShouldEqual(0);
}
