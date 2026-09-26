// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

public class when_inspecting_a_claimed_return_type : Specification
{
    bool _result;
    ReactorSideEffectHandlers _handlers;

    void Establish()
    {
        var handler = Substitute.For<IReactorSideEffectHandler>();
        handler.CanHandleReturnType(typeof(SomeSideEffect)).Returns(true);
        _handlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([handler]));
    }

    void Because() => _result = _handlers.CanHandleReturnType(typeof(SomeSideEffect));

    [Fact] void should_report_the_type_as_supported() => _result.ShouldBeTrue();

    record SomeSideEffect;
}
