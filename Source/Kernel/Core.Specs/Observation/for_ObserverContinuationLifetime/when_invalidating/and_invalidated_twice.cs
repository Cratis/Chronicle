// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverContinuationLifetime.when_invalidating;

public class and_invalidated_twice : given.a_continuation_checkpoint
{
    bool _freshCheckpointIsCurrent;

    void Because()
    {
        _lifetime.Invalidate();
        _lifetime.Invalidate();
        _freshCheckpointIsCurrent = _lifetime.Capture().IsCurrent;
    }

    [Fact] void should_keep_the_original_checkpoint_invalid() => _checkpoint.IsCurrent.ShouldBeFalse();
    [Fact] void should_not_revive_the_lifetime_when_captured_again() => _freshCheckpointIsCurrent.ShouldBeFalse();
}
