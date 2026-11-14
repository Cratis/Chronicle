// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_UnrecoverableProtectedValueReporter;

public class when_the_report_cache_reaches_capacity : given.a_reporter
{
    int _beforeEviction;

    void Because()
    {
        for (var index = 0; index < UnrecoverableProtectedValueReporter.Capacity; index++) _reporter.Report(Identity with { Path = index.ToString() }, _failure);
        _reporter.Report(Identity with { Path = "1" }, _failure);
        _beforeEviction = _count;
        _reporter.Report(Identity, _failure);
        _reporter.Report(Identity with { Path = "0" }, _failure);
    }

    [Fact] void should_not_repeat_an_existing_entry_at_capacity() => _beforeEviction.ShouldEqual(UnrecoverableProtectedValueReporter.Capacity);
    [Fact] void should_evict_the_oldest_entry() => _count.ShouldEqual(UnrecoverableProtectedValueReporter.Capacity + 2);
}
