// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_UnrecoverableProtectedValueReporter;

public class when_the_diagnostic_window_expires : given.a_reporter
{
    void Because()
    {
        _reporter.Report(Identity, _failure);
        _clock.Advance(UnrecoverableProtectedValueReporter.Lifetime);
        _reporter.Report(Identity, _failure);
    }

    [Fact] void should_allow_a_new_report_after_expiry() => _count.ShouldEqual(2);
}
