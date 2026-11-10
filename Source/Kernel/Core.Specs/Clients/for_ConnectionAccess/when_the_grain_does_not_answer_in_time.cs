// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

public class when_the_grain_does_not_answer_in_time : given.a_connection_check
{
    RegistrationClock _clock;

    void Establish()
    {
        _clock = new();
        _reservation.IsHeldBy(Alice).Returns(new TaskCompletionSource<bool>().Task);
    }
    async Task Because()
    {
        var pending = _ownership.Verify(_factory, Id, Alice, Timeout, timeProvider: _clock);
        await _clock.Created.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _clock.Expire();
        _result = await pending;
    }

    [Fact] void should_report_that_access_cannot_be_verified() => _result.ShouldEqual(ConnectionAccessOutcome.Unverifiable);
    [Fact] void should_use_the_supplied_timeout() => _clock.DueTime.ShouldEqual(Timeout);
}
