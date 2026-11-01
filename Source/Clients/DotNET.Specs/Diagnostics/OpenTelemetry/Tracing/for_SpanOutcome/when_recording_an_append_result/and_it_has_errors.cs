// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_SpanOutcome.when_recording_an_append_result;

/// <summary>Errors take precedence over violations.</summary>
public class and_it_has_errors : given.an_append_result
{
    void Establish()
    {
        _result.HasErrors.Returns(true);
        _result.HasConcurrencyViolations.Returns(true);
        _result.HasConstraintViolations.Returns(true);
    }

    void Because() => _activity.RecordAppendOutcome(_result);

    [Fact] void should_record_error() => _activity.GetTagItem("cratis.chronicle.append.outcome").ShouldEqual("error");
    [Fact] void should_record_error_status() => _activity.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_the_error_code() => _activity.GetTagItem("error.type").ShouldEqual("append_error");
}
