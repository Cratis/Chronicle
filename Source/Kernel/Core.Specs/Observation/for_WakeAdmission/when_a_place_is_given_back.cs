// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_WakeAdmission;

public class when_a_place_is_given_back : Specification
{
    WakeAdmission _admission;
    Task _waiting;

    async Task Establish()
    {
        _admission = new(1, 2);
        await _admission.WaitAsync(CancellationToken.None);
        _waiting = _admission.WaitAsync(CancellationToken.None);
    }

    void Because() => _admission.Release();

    [Fact] async Task should_let_the_waiting_wake_in() => await _waiting.WaitAsync(TimeSpan.FromSeconds(5));
}
