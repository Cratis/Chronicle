// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_resolving_handlers;

public class and_context_overloads_tie : given.emitted_methods
{
    Exception _failure;

    void Because() => _failure = Catch.Exception(() =>
    {
        var model = Emit(type =>
        {
            Method(type, typeof(Joined));
            Method(type, typeof(Joined), context: true);
        });
        _ = new SelfReducerInvoker(Registry(typeof(Joined)), model, "state");
    });

    [Fact] void should_refuse_to_choose_by_reflection_order() => _failure.ShouldBeOfExactType<AmbiguousSelfReducerMethod>();
}
