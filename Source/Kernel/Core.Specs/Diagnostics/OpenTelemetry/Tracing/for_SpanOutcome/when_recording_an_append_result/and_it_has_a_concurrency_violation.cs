// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_SpanOutcome.when_recording_an_append_result;

/// <summary>A concurrency violation records its outcome.</summary>
public class and_it_has_a_concurrency_violation : given.an_append_result
{
    void Because() => Record(new() { ConcurrencyViolation = new("source", 1, 2), ConstraintViolations = [new("event", 1, default, "constraint", "private message", new())] });

    [Fact] void should_record_concurrency_violation() => _activity.GetTagItem("cratis.chronicle.append.outcome").ShouldEqual("concurrency_violation");
    [Fact] void should_record_error_status() => _activity.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_the_error_code() => _activity.GetTagItem("error.type").ShouldEqual("concurrency_violation");
    [Fact] void should_record_many_concurrency_violation() => _manyActivity.GetTagItem("cratis.chronicle.append.outcome").ShouldEqual("concurrency_violation");
}
