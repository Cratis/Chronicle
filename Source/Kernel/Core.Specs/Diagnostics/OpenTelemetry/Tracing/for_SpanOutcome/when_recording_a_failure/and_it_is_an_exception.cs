// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_SpanOutcome.when_recording_a_failure;

/// <summary>Exception messages must never be recorded.</summary>
public class and_it_is_an_exception : Specification
{
    Activity _activity;

    void Because()
    {
        _activity = new Activity("failure").Start();
        _activity.RecordFailure(new InvalidOperationException("private message"));
        _activity.RecordAppendOutcome(new EventSequences.AppendResult { Errors = ["private message"] });
    }

    [Fact] void should_record_error_status() => _activity.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_preserve_the_exception_type() => _activity.GetTagItem("error.type").ShouldEqual(typeof(InvalidOperationException).FullName);
    [Fact] void should_not_record_the_message_in_tags() => _activity.TagObjects.Any(tag => tag.Value?.ToString()?.Contains("private message") == true).ShouldBeFalse();
    [Fact] void should_not_record_exception_events() => _activity.Events.ShouldBeEmpty();
    [Fact] void should_not_record_a_status_description() => string.IsNullOrEmpty(_activity.StatusDescription).ShouldBeTrue();

    void Destroy() => _activity.Dispose();
}
