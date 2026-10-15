// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.Validators.for_TypeMustImplementOnlyOneReducer;

public class when_type_implements_multiple_reducers : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => TypeMustImplementOnlyOneReducer.ThrowIfTypeImplementsMoreThanOneReducer(TypeImplementingTwice.Of(typeof(IReducerFor<>), typeof(string), typeof(object))));

    [Fact] void should_throw_an_exception() => _result.ShouldBeOfExactType<TypeMustImplementOnlyOneReducer>();
}
