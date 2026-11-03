// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerHandlerKernelObservation.when_checking;

public class and_the_reducer_is_active_with_methods : Specification
{
    IReducerHandler _handler;
    bool _result;

    void Establish()
    {
        _handler = Substitute.For<IReducerHandler>();
        _handler.IsActive.Returns(true);
        _handler.EventTypes.Returns([EventType.Unknown]);
    }

    void Because() => _result = _handler.IsObservedByKernel();

    [Fact] void should_be_observed_by_the_kernel() => _result.ShouldBeTrue();
}
