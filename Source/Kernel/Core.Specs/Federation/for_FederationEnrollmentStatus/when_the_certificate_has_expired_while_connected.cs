// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentStatus;

public class when_the_certificate_has_expired_while_connected : given.an_enrolled_kernel_with_a_dialer_session
{
    FederationEnrollmentStatus _result;

    async Task Because()
    {
        _dial.RecordDialExchange();
        _time.Advance(TimeSpan.FromDays(30));
        _result = await Read();
    }

    [Fact] void should_be_expired() => _result.Expired.ShouldBeTrue();
    [Fact] void should_not_report_connected() => _result.Connected.ShouldBeFalse();
}
