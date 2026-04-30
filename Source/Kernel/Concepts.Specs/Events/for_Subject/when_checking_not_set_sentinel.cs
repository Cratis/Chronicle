// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_Subject;

public class when_checking_not_set_sentinel : Specification
{
    [Fact] void should_report_not_set_as_unset() => Subject.NotSet.IsSet.ShouldBeFalse();
    [Fact] void should_report_empty_string_subject_as_unset() => new Subject(string.Empty).IsSet.ShouldBeFalse();
    [Fact] void should_report_non_empty_subject_as_set() => new Subject("anything").IsSet.ShouldBeTrue();
}
