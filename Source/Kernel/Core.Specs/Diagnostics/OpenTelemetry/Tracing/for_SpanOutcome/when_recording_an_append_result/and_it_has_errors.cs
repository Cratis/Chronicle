// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_SpanOutcome.when_recording_an_append_result;

/// <summary>An error takes precedence over violations.</summary>
public class and_it_has_errors : given.an_append_result
{
    void Because() => Record(new() { Errors = ["private message"], ConcurrencyViolation = new("source", 1, 2) });

    [Fact] void should_record_error() => _activity.GetTagItem("cratis.chronicle.append.outcome").ShouldEqual("error");
    [Fact] void should_record_error_status() => _activity.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_a_code_not_a_message() => _activity.GetTagItem("error.type").ShouldEqual("append_error");
    [Fact] void should_record_many_error() => _manyActivity.GetTagItem("cratis.chronicle.append.outcome").ShouldEqual("error");
    [Fact] void should_record_many_error_status() => _manyActivity.Status.ShouldEqual(ActivityStatusCode.Error);
}
