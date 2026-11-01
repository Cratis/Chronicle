// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_releasing_discovered_handlers : given.two_discovered_reducers
{
    void Because()
    {
        _reducers.Release();
        _reducers.Release();
    }

    [Fact] void should_cancel_the_first_handler() => _firstToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_cancel_the_second_handler() => _secondToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_dispose_the_first_handler() => Catch.Exception(() => _ = _first.CancellationToken).ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_dispose_the_second_handler() => Catch.Exception(() => _ = _second.CancellationToken).ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_empty_the_model_index() => _reducers.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_empty_the_first_type_index() => Catch.Exception(() => _reducers.GetHandlerFor(_first.ReducerType)).ShouldBeOfExactType<UnknownReducerType>();
    [Fact] void should_empty_the_second_type_index() => Catch.Exception(() => _reducers.GetHandlerFor(_second.ReducerType)).ShouldBeOfExactType<UnknownReducerType>();
}
