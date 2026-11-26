// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children.given;

public class a_changeset_with_an_items_collection : Specification
{
    protected static readonly PropertyPath _itemsProperty = new("[items]");
    protected static readonly PropertyPath _idProperty = new("id");
    protected static readonly PropertyPath _nameProperty = new("[items].name");

    protected Changeset<object, ExpandoObject> _changeset;

    void Establish()
    {
        var initialState = new ExpandoObject();
        ((IDictionary<string, object?>)initialState)["items"] = new List<object>();
        _changeset = new Changeset<object, ExpandoObject>(Substitute.For<IObjectComparer>(), new object(), initialState);
    }

    protected IEnumerable<IDictionary<string, object?>> Items =>
        ((IEnumerable<object>)((IDictionary<string, object?>)_changeset.CurrentState)["items"]!)
            .Cast<IDictionary<string, object?>>();

    protected PropertyMapper<object, ExpandoObject> SetName(string name) => (_, target, indexers) =>
    {
        _nameProperty.SetValue(target, name, indexers);
        return new PropertyDifference(_nameProperty, null, name, indexers);
    };

    protected static ArrayIndexers IndexersFor(object identifier, PropertyPath? identifiedBy = default) =>
        new([new ArrayIndexer(_itemsProperty, identifiedBy ?? _idProperty, identifier)]);
}
