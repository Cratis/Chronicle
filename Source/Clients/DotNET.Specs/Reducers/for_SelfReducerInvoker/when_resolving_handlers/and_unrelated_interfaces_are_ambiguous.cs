// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_resolving_handlers;

public class and_unrelated_interfaces_are_ambiguous : given.emitted_methods
{
    Exception[] _failures;

    void Because() => _failures = new[] { false, true }.Select(reverse => Catch.Exception(() =>
    {
        var model = Emit(type =>
        {
            Method(type, reverse ? typeof(IRight) : typeof(ILeft));
            Method(type, reverse ? typeof(ILeft) : typeof(IRight));
        });
        _ = new SelfReducerInvoker(Registry(typeof(Joined)), model, "state");
    })).ToArray();

    [Fact] void should_fail_in_both_declaration_orders() => _failures.All(failure => failure is AmbiguousSelfReducerMethod).ShouldBeTrue();
}
