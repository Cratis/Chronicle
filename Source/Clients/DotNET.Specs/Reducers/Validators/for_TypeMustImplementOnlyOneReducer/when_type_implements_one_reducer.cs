// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.Validators.for_TypeMustImplementOnlyOneReducer;

public class when_type_implements_one_reducer : Specification
{
    class MyReducer : IReducerFor<string>
    {
        public ReducerId Id => throw new NotImplementedException();
    }

    Exception _result;

    void Because() => _result = Catch.Exception(() => TypeMustImplementOnlyOneReducer.ThrowIfTypeImplementsMoreThanOneReducer(typeof(MyReducer)));

    [Fact] void should_not_throw_an_exception() => _result.ShouldBeNull();
}
