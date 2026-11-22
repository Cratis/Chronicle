// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_invalidating;

public class and_an_old_checkpoint_is_used : given.a_continuation_checkpoint
{
    void Because()
    {
        _lifetime.Invalidate();
        _error = Catch.Exception(_checkpoint.ThrowIfInvalidated);
    }

    [Fact] void should_reject_the_old_checkpoint() => _error.ShouldBeOfExactType<ObserverContinuationInvalidated>();
}
