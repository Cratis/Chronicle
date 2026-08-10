// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

/// <summary>
/// The previous event-store-less contract remains abstract, so it is not possible to compile a handler that
/// implements neither contract. The additive overload may default to it without weakening the published contract.
/// </summary>
public class when_a_handler_implements_neither_contract : Specification
{
    System.Reflection.MethodInfo _previousContract;

    void Because() => _previousContract = typeof(IReactorSideEffectHandler).GetMethod(
        nameof(IReactorSideEffectHandler.CanHandle),
        [typeof(ReactorContext), typeof(object)])!;

    [Fact] void should_keep_the_previous_contract_abstract() => _previousContract.IsAbstract.ShouldBeTrue();
}
