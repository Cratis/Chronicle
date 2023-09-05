// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation.Reducers;

namespace Aksio.Cratis.Reducers.Validators.for_TypeMustImplementOnlyOneReducer;

public class when_type_implements_multiple_reducers : Specification
{
    class MyReducer : IReducerFor<string>, IReducerFor<object>
    {
        public ReducerId Id => throw new NotImplementedException();
    }

    Exception result;

    void Because() => result = Catch.Exception(() => TypeMustImplementOnlyOneReducer.ThrowIfTypeImplementsMoreThanOneReducer(typeof(MyReducer)));

    [Fact] void should_throw_an_exception() => result.ShouldBeOfExactType<TypeMustImplementOnlyOneReducer>();
}
