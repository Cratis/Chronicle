// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_UnrecoverableProtectedValueReporter;

public class when_the_diagnostic_identity_differs : given.a_reporter
{
    void Because()
    {
        _reporter.Report(Identity, _failure);
        _reporter.Report(Identity with { Store = "other" }, _failure);
        _reporter.Report(Identity with { Namespace = "other" }, _failure);
        _reporter.Report(Identity with { Identifier = "other" }, _failure);
        _reporter.Report(Identity with { Path = "other" }, _failure);
        _reporter.Report(Identity with { Phase = "other" }, _failure);
    }

    [Fact] void should_report_each_distinct_identity() => _count.ShouldEqual(6);
}
