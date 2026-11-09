// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering.given;

public class a_changeset_with_existing_children : Specification
{
    protected static readonly PropertyPath _itemsProperty = new("[items]");
    protected static readonly PropertyPath _idProperty = new("id");
    protected static readonly PropertyPath _rankProperty = new("[items].rank");

    protected Changeset<object, ExpandoObject> _changeset;

    void Establish()
    {
        var initialState = new ExpandoObject();
        ((IDictionary<string, object?>)initialState)["items"] = new List<object>
        {
            Child("b", 2),
            Child("d", 4)
        };
        _changeset = new Changeset<object, ExpandoObject>(Substitute.For<IObjectComparer>(), new object(), initialState);
    }

    protected IEnumerable<string> Ids =>
        ((IEnumerable<object>)((IDictionary<string, object?>)_changeset.CurrentState)["items"]!)
            .Select(_ => (string)((IDictionary<string, object?>)_)["id"]!);

    protected void AddChild(string id, int? rank, ChildrenOrdering? ordering)
    {
        var arrayIndexers = new ArrayIndexers([new ArrayIndexer(_itemsProperty, _idProperty, id)]);
        PropertyMapper<object, ExpandoObject>[] mappers = rank is null ? [] : [SetRank];
        _changeset.AddChild<ExpandoObject>(_itemsProperty, _idProperty, id, mappers, arrayIndexers, ordering: ordering);

        PropertyDifference SetRank(object source, ExpandoObject target, ArrayIndexers indexers)
        {
            _rankProperty.SetValue(target, rank.Value, indexers);
            return new PropertyDifference(_rankProperty, null, rank.Value, indexers);
        }
    }

    static ExpandoObject Child(string id, int rank)
    {
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["id"] = id;
        ((IDictionary<string, object?>)child)["rank"] = rank;
        return child;
    }
}
