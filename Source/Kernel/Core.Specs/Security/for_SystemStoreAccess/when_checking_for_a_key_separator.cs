// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreAccess;

public class when_checking_for_a_key_separator : Specification
{
    [Theory]
    [InlineData("#")]
    [InlineData("a#b")]
    [InlineData("#System")]
    [InlineData("System#")]
    [InlineData("event-log#System#Default")]
    public void should_refuse_a_value_with_the_separator(string value) => SystemStoreAccess.HasKeySeparator(value).ShouldBeTrue();

    [Theory]
    [InlineData("%23")]
    [InlineData("\uFF03")]
    [InlineData("Default")]
    [InlineData("")]
    [InlineData(null)]
    public void should_accept_a_value_without_the_separator(string? value) => SystemStoreAccess.HasKeySeparator(value).ShouldBeFalse();

    [Fact] void should_report_no_violation_when_every_value_is_clean() => SystemStoreAccess.FirstSeparatorViolation("a", "b", "c", "d", "e", "f").ShouldEqual(-1);
    [Fact] void should_report_the_first_violating_value() => SystemStoreAccess.FirstSeparatorViolation("a", "b", "c#", "d#").ShouldEqual(2);
    [Fact] void should_report_a_violation_in_the_last_value() => SystemStoreAccess.FirstSeparatorViolation("a", "b", "c", "d", "e", "#").ShouldEqual(5);
    [Fact] void should_report_a_violation_in_a_single_value() => SystemStoreAccess.FirstSeparatorViolation("#").ShouldEqual(0);
}
