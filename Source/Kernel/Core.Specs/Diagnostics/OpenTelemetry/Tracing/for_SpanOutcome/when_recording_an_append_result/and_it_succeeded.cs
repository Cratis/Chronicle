// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_SpanOutcome.when_recording_an_append_result;

/// <summary>A successful append has no error status.</summary>
public class and_it_succeeded : given.an_append_result
{
    void Because() => Record(new());

    [Fact] void should_record_success() => _activity.GetTagItem("cratis.chronicle.append.outcome").ShouldEqual("success");
    [Fact] void should_leave_status_unset() => _activity.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_not_record_an_error_type() => _activity.GetTagItem("error.type").ShouldBeNull();
    [Fact] void should_record_many_success() => _manyActivity.GetTagItem("cratis.chronicle.append.outcome").ShouldEqual("success");
    [Fact] void should_leave_many_status_unset() => _manyActivity.Status.ShouldEqual(ActivityStatusCode.Unset);
}
