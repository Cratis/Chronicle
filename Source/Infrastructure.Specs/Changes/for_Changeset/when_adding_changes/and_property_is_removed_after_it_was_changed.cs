// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_property_is_removed_after_it_was_changed : given.a_changeset
{
    PropertyDifference _difference;

    void Establish()
    {
        _changeset.Add(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", "Before", "After")]));
    }

    void Because()
    {
        _changeset.Add(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [PropertyDifference.Removal("name", "After")]));
        _difference = _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Single().Differences.Single();
    }

    [Fact] void should_keep_one_properties_changed() => _changeset.Changes.Count().ShouldEqual(1);
    [Fact] void should_record_a_removal() => _difference.IsRemoval.ShouldBeTrue();
    [Fact] void should_keep_the_original_value_of_the_first_change() => _difference.Original.ShouldEqual("Before");
}
