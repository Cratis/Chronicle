// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Setup.Serialization.for_ExpandoObjectSerializer.when_round_tripping_through_orleans;

/// <summary>
/// Children that came back as dictionaries could not be looked up by their properties on the receiving silo,
/// which is what broke Chronicle's on-demand read models whenever the projection and its cache ran on different
/// silos (Cratis/Chronicle#4337).
/// </summary>
public class an_object_with_children_in_a_collection : given.a_serializer_for_expando_objects
{
    ExpandoObject _result;

    void Because()
    {
        var children = new List<object>
        {
            Expando(("id", "comment-1"), ("reference", "100")),
            Expando(("id", "comment-2"), ("reference", "200"))
        };
        _result = AcrossSilos(Expando(("id", "issue"), ("associations", children)));
    }

    [Fact] void should_keep_the_top_level_value() => Get(_result, "id").ShouldEqual("issue");
    [Fact] void should_keep_every_child_as_an_expando_object() => Children.ShouldEachConformTo(_ => _ is ExpandoObject);
    [Fact] void should_keep_the_content_of_every_child() => Children.Select(_ => Get(_, "reference")).ShouldContainOnly("100", "200");

    IEnumerable<object> Children => (IEnumerable<object>)Get(_result, "associations")!;
}
