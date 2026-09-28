// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Setup.Serialization.for_ExpandoObjectSerializer.when_round_tripping_through_orleans;

public class an_object_nested_several_levels_deep : given.a_serializer_for_expando_objects
{
    ExpandoObject _result;

    void Because()
    {
        var line = Expando(("tags", new List<object> { Expando(("name", "first")) }));
        var details = Expando(("address", Expando(("city", "Oslo"))), ("lines", new List<object> { line }));
        _result = AcrossSilos(Expando(("details", details)));
    }

    [Fact] void should_keep_a_nested_object_as_an_expando_object() => Get(_result, "details").ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_keep_an_object_nested_in_a_nested_object() => Get(Get(Get(_result, "details"), "address"), "city").ShouldEqual("Oslo");
    [Fact] void should_keep_a_child_of_a_child_as_an_expando_object() => Tag.ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_keep_the_content_of_a_child_of_a_child() => Get(Tag, "name").ShouldEqual("first");

    object Tag => ((IEnumerable<object>)Get(((IEnumerable<object>)Get(Get(_result, "details"), "lines")!).Single(), "tags")!).Single();
}
