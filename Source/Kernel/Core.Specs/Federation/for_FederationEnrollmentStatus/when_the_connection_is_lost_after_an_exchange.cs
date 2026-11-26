// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentStatus;

public class when_the_connection_is_lost_after_an_exchange : given.an_enrolled_kernel_with_a_dialer_session
{
    FederationEnrollmentStatus _result;

    async Task Because()
    {
        _dial.RecordDialExchange();
        _time.Advance(TimeSpan.FromMinutes(1));
        _dial.RecordDialClosed();
        _result = await Read();
    }

    [Fact] void should_not_be_connected() => _result.Connected.ShouldBeFalse();
    [Fact] void should_keep_the_time_of_the_last_exchange() => _result.LastExchange.ShouldEqual(_start);
}
