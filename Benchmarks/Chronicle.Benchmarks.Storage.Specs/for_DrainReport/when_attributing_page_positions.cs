// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_DrainReport;

public class when_attributing_page_positions : Specification
{
    string _report = default!;

    void Because()
    {
        DrainPage[] pages = [.. Enumerable.Range(0, 8).Select(index => new DrainPage(index, (ulong)(index * 1000), index == 7 ? 500 : 1000, index + 1)), new(8, 7500, 0, 999)];
        _report = DrainReport.Format(7500, pages, TimeSpan.FromMilliseconds(1040));
    }

    [Fact] void should_include_actual_page_sizes_and_event_count() => Assert.Contains("Pages: 8; page sizes (events): 500, 1000; events read: 7500", _report);
    [Fact] void should_exclude_the_empty_probe_from_percentiles() => Assert.Contains("median=4.000; p90=8.000; max=8.000", _report);
    [Fact] void should_compare_first_and_last_quarters_in_page_order() => Assert.Contains("first=1.000 ms; last=7.000 ms", _report);
    [Fact] void should_keep_the_terminal_probe_and_start_position() => Assert.Contains("| 8 | 7500 | 0 | 999.000 |", _report);
    [Fact] void should_include_both_wall_and_move_next_totals() => Assert.Contains("1040.000 ms wall; 1035.000 ms MoveNext", _report);
}
