// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.DependencyInjection;
using Cratis.Monads;

namespace Cratis.Chronicle.Integration.for_Reactors;

[Singleton]
public class FailingAppendSideEffectHandler : IReactorSideEffectHandler
{
    public bool CanHandle(ReactorContext reactorContext, object value) => value is FailingAppendSideEffect;

    public Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var failure = new ReactorSideEffectFailure(
            [
                new AppendFailure(
                    [new ReactorConstraintViolation(nameof(FailingAppendSideEffect), "Simulated reactor side-effect append failure")],
                    false,
                    [])
            ]);
        return Task.FromResult(Result.Failed(failure));
    }
}
