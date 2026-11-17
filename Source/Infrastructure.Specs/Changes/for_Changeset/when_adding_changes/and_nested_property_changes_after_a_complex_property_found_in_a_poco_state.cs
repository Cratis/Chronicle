// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_nested_property_changes_after_a_complex_property_found_in_a_poco_state : Specification
{
    sealed class DisplayName
    {
        public string Name { get; set; } = string.Empty;
    }

    sealed class ReadModel
    {
        public DisplayName? DisplayName { get; set; }
    }

    Changeset<object, ReadModel> _changeset;
    PropertyDifference _difference;

    void Establish()
    {
        _changeset = new Changeset<object, ReadModel>(Substitute.For<IObjectComparer>(), new object(), new ReadModel());
        _changeset.Add(new PropertiesChanged<ReadModel>(new ReadModel(), [new PropertyDifference("displayName", null, "Plain")]));
    }

    void Because()
    {
        _changeset.Add(new PropertiesChanged<ReadModel>(new ReadModel { DisplayName = new DisplayName { Name = "Other" } }, [new PropertyDifference("displayName.name", "Plain", "Other")]));
        _difference = _changeset.Changes.OfType<PropertiesChanged<ReadModel>>().Single().Differences.Single();
    }

    [Fact] void should_keep_the_complex_property() => _difference.PropertyPath.Path.ShouldEqual("displayName");
    [Fact] void should_take_the_value_from_the_state() => ((DisplayName)_difference.Changed!).Name.ShouldEqual("Other");
}
