// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_VerdictFormatter;

/// <summary>
/// Specifies the exact shell-facing verdict lines.
/// </summary>
public class when_formatting_results : Specification
{
    [Fact]
    void should_format_the_clean_line() => VerdictFormatter.Format(new(0, 0, 0, 0, 0, 0, 0, 0, []), 17, 42, 3).ShouldEqual("VERDICT: clean, 42 events checked, 3 observers checked (seed 17)");

    [Fact]
    void should_format_the_dirty_line() => VerdictFormatter.Format(new(1, 2, 3, 4, 5, 6, 7, 8, []), 17, 42, 3).ShouldEqual("VERDICT: 1 lost, 2 duplicated, 3 gaps, 4 observers behind, 5 projections behind, 6 reducers behind, 7 failed partitions, 8 reactor duplicates (seed 17)");
}
