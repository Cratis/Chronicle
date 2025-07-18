// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.Validators.for_TypeMustImplementReducer;

public class when_type_does_not_implement_reducer : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(typeof(string)));

    [Fact] void should_throw_TypeDoesNotImplementReducer() => _result.ShouldBeOfExactType<TypeMustImplementReducer>();
}
