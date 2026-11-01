// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_cancelling_one_handler : given.two_discovered_reducers
{
    void Because() => _first.Disconnect();

    [Fact] void should_remove_only_the_cancelled_model() => _reducers.GetAllHandlers().ShouldContainOnly(_second);
    [Fact] void should_remove_the_cancelled_type() => Catch.Exception(() => _reducers.GetHandlerFor(_first.ReducerType)).ShouldBeOfExactType<UnknownReducerType>();
    [Fact] void should_keep_the_other_type() => _reducers.GetHandlerFor(_second.ReducerType).ShouldBeSame(_second);
    [Fact] void should_remove_the_cancelled_model_index() => _reducers.HasFor(_first.ReadModelType).ShouldBeFalse();
    [Fact] void should_keep_the_other_model_index() => _reducers.HasFor(_second.ReadModelType).ShouldBeTrue();
    [Fact] void should_not_cancel_the_other_handler() => _secondToken.IsCancellationRequested.ShouldBeFalse();
}
