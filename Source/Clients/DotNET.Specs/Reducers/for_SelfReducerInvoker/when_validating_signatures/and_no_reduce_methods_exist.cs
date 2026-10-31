// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_validating_signatures;

public class and_no_reduce_methods_exist : given.emitted_methods
{
    Exception _failure;

    void Because() => _failure = Catch.Exception(() => _ = new SelfReducerInvoker(Registry(typeof(Joined)), Emit(_ => { }), "state"));

    [Fact] void should_fail_instead_of_creating_an_inert_invoker() => _failure.ShouldBeOfExactType<SelfReducerHasNoReduceMethods>();
}
