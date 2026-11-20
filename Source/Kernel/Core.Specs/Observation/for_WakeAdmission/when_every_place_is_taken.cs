// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_WakeAdmission;

public class when_every_place_is_taken : Specification
{
    WakeAdmission _admission;
    Task _waiting;

    async Task Establish()
    {
        _admission = new(2, 2);
        await _admission.WaitAsync(CancellationToken.None);
        await _admission.WaitAsync(CancellationToken.None);
    }

    void Because() => _waiting = _admission.WaitAsync(CancellationToken.None);

    [Fact] void should_make_the_next_wake_wait() => _waiting.IsCompleted.ShouldBeFalse();
    [Fact] void should_count_the_wakes_in_progress() => _admission.InProgress.ShouldEqual(2);
}
