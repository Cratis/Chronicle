// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_LifecycleActivation;

public class when_using_per_process_identity : Specification
{
    ActivationId _first;
    ActivationId _second;

    void Because()
    {
        _first = LifecycleActivation.Resolve(new() { ActivationScope = ActivationScope.PerProcess });
        _second = LifecycleActivation.Resolve(new() { ActivationScope = ActivationScope.PerProcess });
    }

    [Fact] void should_reuse_the_identity_for_this_process() => _first.ShouldEqual(_second);
    [Fact] void should_not_use_an_empty_identity() => _first.Value.ShouldNotBeEmpty();
}
