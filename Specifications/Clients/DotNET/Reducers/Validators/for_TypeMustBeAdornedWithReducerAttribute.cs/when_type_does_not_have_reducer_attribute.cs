// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers.Validators.for_TypeMustBeAdornedWithReducerAttribute;

public class when_type_does_not_have_reducer_attribute : Specification
{
    Exception result;

    void Because() => result = Catch.Exception(() => TypeMustBeAdornedWithReducerAttribute.ThrowIfReducerAttributeMissing(typeof(string)));

    [Fact] void should_throw_type_must_be_adorned_with_reducer_attribute() => result.ShouldBeOfExactType<TypeMustBeAdornedWithReducerAttribute>();
}
