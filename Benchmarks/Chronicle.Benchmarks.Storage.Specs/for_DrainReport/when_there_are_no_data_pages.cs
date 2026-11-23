// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_DrainReport;

public class when_there_are_no_data_pages : Specification
{
    string _report = default!;

    void Because() => _report = DrainReport.Format(0, [], TimeSpan.Zero);

    [Fact] void should_report_no_pages() => Assert.Contains("Pages: 0", _report);
    [Fact] void should_report_zero_page_times() => Assert.Contains("median=0.000; p90=0.000; max=0.000", _report);
}
