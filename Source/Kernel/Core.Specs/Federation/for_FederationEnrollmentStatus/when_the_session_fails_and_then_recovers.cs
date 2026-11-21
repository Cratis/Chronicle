// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentStatus;

public class when_the_session_fails_and_then_recovers : given.an_enrolled_kernel_with_a_dialer_session
{
    FederationEnrollmentStatus _failed;
    FederationEnrollmentStatus _recovered;

    async Task Because()
    {
        _dial.RecordDialExchange();
        _dial.RecordDialFailed("The producer did not complete the pull");
        _failed = await Read();
        _time.Advance(TimeSpan.FromMinutes(2));
        _dial.RecordDialExchange();
        _recovered = await Read();
    }

    [Fact] void should_report_the_error_when_it_failed() => _failed.LastError.ShouldEqual("The producer did not complete the pull");
    [Fact] void should_not_be_connected_when_it_failed() => _failed.Connected.ShouldBeFalse();
    [Fact] void should_clear_the_error_when_it_recovered() => _recovered.LastError.ShouldEqual(string.Empty);
    [Fact] void should_be_connected_when_it_recovered() => _recovered.Connected.ShouldBeTrue();
    [Fact] void should_move_the_last_exchange_when_it_recovered() => _recovered.LastExchange.ShouldEqual(_start.AddMinutes(2));
}
