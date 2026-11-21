// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentStatus;

public class when_enrolled_but_never_connected : given.an_enrolled_kernel_with_a_dialer_session
{
    FederationEnrollmentStatus _result;

    async Task Because() => _result = await Read();

    [Fact] void should_be_enrolled() => _result.Enrolled.ShouldBeTrue();
    [Fact] void should_not_be_connected() => _result.Connected.ShouldBeFalse();
    [Fact] void should_have_no_last_exchange() => _result.LastExchange.ShouldBeNull();
    [Fact] void should_have_no_last_error() => _result.LastError.ShouldEqual(string.Empty);
}
