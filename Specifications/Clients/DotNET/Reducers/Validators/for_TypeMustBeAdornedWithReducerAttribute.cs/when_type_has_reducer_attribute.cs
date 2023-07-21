// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers.Validators.for_TypeMustBeAdornedWithReducerAttribute;

public class when_type_has_reducer_attribute : Specification
{
    [Reducer("ea927645-ce1f-4521-87b7-8cc74095b946")]
    class TypeWithReducerAttribute
    {
    }

    Exception result;

    void Because() => result = Catch.Exception(() => TypeMustBeAdornedWithReducerAttribute.ThrowIfReducerAttributeMissing(typeof(TypeWithReducerAttribute)));

    [Fact] void should_not_throw() => result.ShouldBeNull();
}
