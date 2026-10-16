// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_LifecycleActivation;

public class when_starting_without_an_identity : Specification
{
    Exception? _failure;

    void Because() => _failure = Catch.Exception(() => LifecycleActivation.Resolve(new()));

    [Fact] void should_require_a_host_supplied_activation() => _failure.ShouldBeOfExactType<ActivationIdRequired>();
}
