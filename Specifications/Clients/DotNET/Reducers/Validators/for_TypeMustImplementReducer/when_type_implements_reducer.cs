// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation.Reducers;

namespace Aksio.Cratis.Reducers.Validators.for_TypeMustImplementReducer;

public class when_type_implements_reducer : Specification
{
    class MyReducer : IReducerFor<string>
    {
        public ReducerId Id => throw new NotImplementedException();
    }

    Exception result;

    void Because() => result = Catch.Exception(() => TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(typeof(MyReducer)));

    [Fact] void should_not_throw_an_exception() => result.ShouldBeNull();
}
