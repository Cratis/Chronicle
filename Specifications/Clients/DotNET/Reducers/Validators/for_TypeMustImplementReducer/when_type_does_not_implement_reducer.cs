// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers.Validators.for_TypeMustImplementReducer;

public class when_type_does_not_implement_reducer : Specification
{
    Exception result;

    void Because() => result = Catch.Exception(() => TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(typeof(string)));

    [Fact] void should_throw_TypeDoesNotImplementReducer() => result.ShouldBeOfExactType<TypeMustImplementReducer>();
}
