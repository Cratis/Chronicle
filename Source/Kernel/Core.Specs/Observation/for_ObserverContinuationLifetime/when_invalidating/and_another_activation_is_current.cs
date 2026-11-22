// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_invalidating;

public class and_another_activation_is_current : given.a_continuation_checkpoint
{
    readonly ObserverContinuationLifetime _otherActivation = new();

    async Task Because()
    {
        _lifetime.Invalidate();
        _error = await Catch.Exception(() => _otherActivation.Capture().Await(() =>
        {
            _continued = true;
            return Task.CompletedTask;
        }));
    }

    [Fact] void should_allow_work_from_the_other_activation() => _continued.ShouldBeTrue();
    [Fact] void should_not_reject_the_other_activation() => _error.ShouldBeNull();
    [Fact] void should_leave_the_original_checkpoint_invalid() => _checkpoint.IsCurrent.ShouldBeFalse();
}
