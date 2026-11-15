// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_applying_changes;

public class and_a_join_inserts_the_same_values_into_two_rows : given.a_sink_with_two_joined_rows
{
    ExpandoObject _addedChild;
    ExpandoObject _address;

    void Establish()
    {
        _addedChild = new ExpandoObject();
        ((IDictionary<string, object?>)_addedChild)["childId"] = "added";
        _address = new ExpandoObject();
        ((IDictionary<string, object?>)_address)["street"] = "Main";
        _changeset.Changes.Returns(
        [
            RootJoin(
                "shared",
                "group",
                Set("address", _address),
                new ChildAdded(_addedChild, new PropertyPath("children"), new PropertyPath("childId"), "added", ArrayIndexers.NoIndexers))
        ]);
    }

    async Task Because() => await _sink.ApplyChanges(KeyFor("a"), _changeset, WrittenAt);

    [Fact] void should_add_the_child_to_both_rows() => (Children("a").Length, Children("b").Length).ShouldEqual((2, 2));
    [Fact] void should_not_share_the_added_child_between_rows() => Children("a")[1].ShouldNotBeSame(Children("b")[1]);
    [Fact] void should_not_publish_the_changeset_child() => Children("a")[1].ShouldNotBeSame(_addedChild);
    [Fact] void should_set_the_address_on_both_rows() => ((IDictionary<string, object?>)Row("b")["address"]!)["street"].ShouldEqual("Main");
    [Fact] void should_not_share_the_address_between_rows() => Row("a")["address"].ShouldNotBeSame(Row("b")["address"]);
    [Fact] void should_not_publish_the_changeset_address() => Row("a")["address"].ShouldNotBeSame(_address);
}
