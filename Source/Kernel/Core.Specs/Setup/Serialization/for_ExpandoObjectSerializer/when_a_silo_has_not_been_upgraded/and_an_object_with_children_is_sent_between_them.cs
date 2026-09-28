// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Setup.Serialization.for_ExpandoObjectSerializer.when_a_silo_has_not_been_upgraded;

/// <summary>
/// Silos are upgraded one at a time, so for a while an upgraded silo and one that is not exchange messages. Both
/// directions must keep working - what goes on the wire is unchanged, only how it is read.
/// </summary>
public class and_an_object_with_children_is_sent_between_them : given.a_serializer_for_expando_objects
{
    ExpandoObject _receivedByTheUpgradedSilo;
    ExpandoObject _receivedByThePreviousSilo;

    void Because()
    {
        _receivedByTheUpgradedSilo = FromAPreviousSilo(AnObjectWithChildren());
        _receivedByThePreviousSilo = ToAPreviousSilo(AnObjectWithChildren());
    }

    [Fact] void should_give_the_upgraded_silo_every_child_as_an_expando_object() => ChildrenOf(_receivedByTheUpgradedSilo).ShouldEachConformTo(_ => _ is ExpandoObject);
    [Fact] void should_give_the_upgraded_silo_the_content_of_every_child() => ChildrenOf(_receivedByTheUpgradedSilo).Select(_ => Get(_, "reference")).ShouldContainOnly("100", "200");
    [Fact] void should_still_be_readable_by_the_previous_silo() => ChildrenOf(_receivedByThePreviousSilo).Select(_ => ((IDictionary<string, object?>)_)["reference"]).ShouldContainOnly("100", "200");

    ExpandoObject AnObjectWithChildren()
    {
        var children = new List<object>
        {
            Expando(("id", "comment-1"), ("reference", "100")),
            Expando(("id", "comment-2"), ("reference", "200"))
        };
        return Expando(("id", "issue"), ("associations", children));
    }

    static IEnumerable<object> ChildrenOf(ExpandoObject value) => (IEnumerable<object>)Get(value, "associations")!;
}
