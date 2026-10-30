// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_comparison_throws_after_a_pending_pass_skipped_the_definition : given.a_manager_with_a_pass_skipping_an_announced_definition
{
    Task Because() => RegisterWhilePassIsPending(fail: true);

    [Fact] void should_subscribe_after_the_pending_pass_finishes() => _subscribed.ShouldBeTrue();
    [Fact] void should_preserve_the_registration_failure() => _registrationFailure.ShouldBeOfExactType<InvalidOperationException>();
}
