// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_DerivedPropertyFunctions;

public class when_resolving_week_year_receiver_types : Specification
{
    IReadOnlyCollection<Type> _result;

    void Because() => _result = DerivedPropertyFunctions.All["WeekYear"].SupportedReceiverTypes;

    [Fact] void should_support_date_time_offset() => _result.ShouldContain(typeof(DateTimeOffset));
    [Fact] void should_support_date_time() => _result.ShouldContain(typeof(DateTime));
    [Fact] void should_support_date_only() => _result.ShouldContain(typeof(DateOnly));
    [Fact] void should_not_support_strings_despite_runtime_normalization() => _result.ShouldNotContain(typeof(string));
    [Fact] void should_not_support_numbers() => _result.ShouldNotContain(typeof(int));
}
