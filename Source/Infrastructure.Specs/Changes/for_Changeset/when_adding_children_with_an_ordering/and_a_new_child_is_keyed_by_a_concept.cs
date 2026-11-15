// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_a_new_child_is_keyed_by_a_concept : Specification
{
    static readonly PropertyPath _itemsProperty = new("[items]");
    static readonly PropertyPath _idProperty = new("id");
    static readonly PropertyPath _rankProperty = new("[items].rank");
    static readonly Guid _first = Guid.Parse("00000000-0000-0000-0000-000000000001");
    static readonly Guid _second = Guid.Parse("00000000-0000-0000-0000-000000000002");
    static readonly Guid _third = Guid.Parse("00000000-0000-0000-0000-000000000003");

    Changeset<object, ExpandoObject> _changeset;

    IEnumerable<Guid> Ids =>
        ((IEnumerable<object>)((IDictionary<string, object?>)_changeset.CurrentState)["items"]!)
            .Select(_ => ((IDictionary<string, object?>)_)["id"] switch
            {
                ChildId childId => childId.Value,
                var id => (Guid)id!
            });

    void Establish()
    {
        var initialState = new ExpandoObject();
        ((IDictionary<string, object?>)initialState)["items"] = new List<object>
        {
            Child(_first, 2),
            Child(_third, 4)
        };
        _changeset = new Changeset<object, ExpandoObject>(Substitute.For<IObjectComparer>(), new object(), initialState);
    }

    void Because()
    {
        var key = new ChildId(_second);
        var indexers = new ArrayIndexers([new ArrayIndexer(_itemsProperty, _idProperty, key)]);
        _changeset.AddChild<ExpandoObject>(_itemsProperty, _idProperty, key, [], indexers, ordering: new ChildrenOrdering(ChildrenPlacement.Sorted, new("rank")));

        PropertyMapper<object, ExpandoObject>[] mappers =
        [
            (_, target, arrayIndexers) =>
            {
                _rankProperty.SetValue(target, 3, arrayIndexers);
                return new PropertyDifference(_rankProperty, null, 3, arrayIndexers);
            }
        ];
        _changeset.SetProperties(mappers, indexers);
    }

    [Fact] void should_order_the_new_child_by_its_final_sort_value() => string.Join(',', Ids).ShouldEqual($"{_first},{_second},{_third}");

    static ExpandoObject Child(Guid id, int rank)
    {
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["id"] = id;
        ((IDictionary<string, object?>)child)["rank"] = rank;
        return child;
    }

    record ChildId(Guid Value) : ConceptAs<Guid>(Value);
}
