// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_checking_has_for_generic_and_it_exists : given.all_dependencies
{
    class MyReadModel;
    bool _result;

    void Establish()
    {
        var handler = Substitute.For<IReducerHandler>();
        handler.ReadModelType.Returns(typeof(MyReadModel));
        _handlersByModelType[typeof(MyReadModel)] = handler;
    }

    void Because() => _result = _reducers.HasFor<MyReadModel>();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
