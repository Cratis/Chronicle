// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_removing_children.given;

public class a_changeset_with_composite_child_keys : Specification
{
    protected static readonly PropertyPath _itemsProperty = new("[items]");
    protected static readonly PropertyPath _idProperty = new("id");
    protected Changeset<object, ExpandoObject> _changeset;
    protected List<ExpandoObject> _initialChildren;

    void Establish()
    {
        _initialChildren =
        [
            new { id = Key("other", "item"), name = "other tenant" }.AsExpandoObject(),
            new { id = Key("tenant", "other"), name = "other item" }.AsExpandoObject(),
            new { id = Key("tenant", "item"), name = "target" }.AsExpandoObject()
        ];
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["items"] = _initialChildren;
        _changeset = new(Substitute.For<IObjectComparer>(), new object(), state);
    }

    protected IEnumerable<IDictionary<string, object?>> Items =>
        ((IEnumerable<object>)((IDictionary<string, object?>)_changeset.CurrentState)["items"]!)
            .Cast<IDictionary<string, object?>>();

    protected void Remove(object key) => _changeset.RemoveChild(
        _itemsProperty,
        _idProperty,
        key,
        new([new ArrayIndexer(_itemsProperty, _idProperty, key)]));

    protected static ExpandoObject Key(string tenantId, string itemId) => new { tenantId, itemId }.AsExpandoObject();
}
