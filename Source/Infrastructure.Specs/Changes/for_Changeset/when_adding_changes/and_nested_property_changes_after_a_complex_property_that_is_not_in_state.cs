// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_nested_property_changes_after_a_complex_property_that_is_not_in_state : given.a_changeset
{
    PropertyDifference _difference;

    void Establish()
    {
        _changeset.Add(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("displayName", null, "Plain")]));
    }

    void Because()
    {
        _changeset.Add(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("displayName.name", "Plain", "Other")]));
        _difference = _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Single().Differences.Single();
    }

    [Fact] void should_keep_the_complex_property() => _difference.PropertyPath.Path.ShouldEqual("displayName");
    [Fact] void should_keep_the_complex_propertys_changed_value() => _difference.Changed.ShouldEqual("Plain");
}
