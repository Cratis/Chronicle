// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentStatus;

public class when_connected_with_a_recent_exchange : given.an_enrolled_kernel_with_a_dialer_session
{
    FederationEnrollmentStatus _result;

    async Task Because()
    {
        _time.Advance(TimeSpan.FromMinutes(5));
        _dial.RecordDialExchange();
        _result = await Read();
    }

    [Fact] void should_be_connected() => _result.Connected.ShouldBeTrue();
    [Fact] void should_report_the_time_of_the_exchange() => _result.LastExchange.ShouldEqual(_start.AddMinutes(5));
    [Fact] void should_have_no_last_error() => _result.LastError.ShouldEqual(string.Empty);
}
