// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Setup.Serialization.for_ExpandoObjectSerializer.when_round_tripping_through_orleans;

public class the_same_object_referenced_twice : given.a_serializer_for_expando_objects
{
    ExpandoObject _result;

    void Because()
    {
        var shared = Expando(("name", "shared"));
        _result = AcrossSilos(Expando(("first", shared), ("second", shared)));
    }

    [Fact] void should_keep_both_references() => Get(Get(_result, "second"), "name").ShouldEqual("shared");
    [Fact] void should_keep_them_the_same_object() => ReferenceEquals(Get(_result, "first"), Get(_result, "second")).ShouldBeTrue();
}
