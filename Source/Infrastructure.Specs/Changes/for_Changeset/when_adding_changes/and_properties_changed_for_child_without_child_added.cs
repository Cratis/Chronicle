// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_changes;

public class and_properties_changed_for_child_without_child_added : given.a_changeset
{
    PropertyPath _itemsProperty;
    PropertyPath _nameProperty;
    PropertiesChanged<ExpandoObject> _propertiesChanged;
    ArrayIndexers _arrayIndexers;

    void Establish()
    {
        _itemsProperty = new PropertyPath("items");
        _nameProperty = new PropertyPath("name");

        _arrayIndexers = new ArrayIndexers(
        [
            new ArrayIndexer(_itemsProperty, PropertyPath.CreateFrom([new PropertyName("id")]), "child-1")
        ]);

        // Create a PropertiesChanged that targets a child, but no ChildAdded exists
        var propertyDifference = new PropertyDifference(
            _nameProperty,
            "Original Name",
            "Updated Name",
            _arrayIndexers);

        _propertiesChanged = new PropertiesChanged<ExpandoObject>(
            _initialState,
            [propertyDifference]);
    }

    void Because() => _changeset.Add(_propertiesChanged);

    [Fact] void should_keep_properties_changed() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldContainOnly(_propertiesChanged);
    [Fact] void should_have_one_change() => _changeset.Changes.Count().ShouldEqual(1);
}
