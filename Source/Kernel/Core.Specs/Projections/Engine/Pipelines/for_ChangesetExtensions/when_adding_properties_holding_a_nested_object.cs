// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

public class when_adding_properties_holding_a_nested_object : Specification
{
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    ExpandoObject _source;
    ExpandoObject _nested;
    List<object> _items;
    List<PropertiesChanged<ExpandoObject>> _changes;

    void Establish()
    {
        _nested = new ExpandoObject();
        ((IDictionary<string, object?>)_nested)["Name"] = "Nested";
        _items = [_nested];
        _source = new ExpandoObject();
        var sourceAsDictionary = (IDictionary<string, object?>)_source;
        sourceAsDictionary["Nested"] = _nested;
        sourceAsDictionary["Items"] = _items;

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns([]);
        _changes = [];
        _changeset
            .When(_ => _.Add(Arg.Any<Change>()))
            .Do(_ => _changes.Add(_.Arg<PropertiesChanged<ExpandoObject>>()));
    }

    void Because() => _changeset.AddPropertiesFrom(_source);

    object Changed(string property) => _changes[0].Differences.First(_ => _.PropertyPath == property).Changed!;

    [Fact] void should_not_share_the_nested_object_with_the_source() => Changed("Nested").ShouldNotBeSame(_nested);
    [Fact] void should_keep_the_value_of_the_nested_object() => ((IDictionary<string, object?>)Changed("Nested"))["Name"].ShouldEqual("Nested");
    [Fact] void should_not_share_the_collection_with_the_source() => Changed("Items").ShouldNotBeSame(_items);
    [Fact] void should_not_share_the_collection_items_with_the_source() => ((IEnumerable<object>)Changed("Items")).Single().ShouldNotBeSame(_nested);
}
