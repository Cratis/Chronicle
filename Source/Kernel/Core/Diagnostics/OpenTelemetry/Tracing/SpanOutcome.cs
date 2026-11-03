// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>Records low-cardinality outcomes without exception messages or payloads.</summary>
public static class SpanOutcome
{
    const string ErrorType = "error.type";
    const string AppendOutcome = "cratis.chronicle.append.outcome";

    /// <summary>Records an exception's type, never its message.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="exception">The failure.</param>
    /// <returns>The activity with the exception type recorded.</returns>
    public static Activity RecordFailure(this Activity activity, Exception exception) => activity.RecordFailure(exception.GetType().FullName!);

    /// <summary>Records a low-cardinality failure code.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="errorType">The failure type or code.</param>
    /// <returns>The activity with the failure code recorded.</returns>
    public static Activity RecordFailure(this Activity activity, string errorType) => activity.SetStatus(ActivityStatusCode.Error).SetTag(ErrorType, errorType);

    /// <summary>Records the outcome of a single append.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="result">The append result.</param>
    /// <returns>The activity with the append outcome recorded.</returns>
    public static Activity RecordAppendOutcome(this Activity activity, AppendResult result) =>
        RecordAppendOutcome(activity, result.HasErrors, result.HasConcurrencyViolations, result.HasConstraintViolations);

    /// <summary>Records the outcome of a batch append.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="result">The append result.</param>
    /// <returns>The activity with the batch outcome recorded.</returns>
    public static Activity RecordAppendOutcome(this Activity activity, AppendManyResult result) =>
        RecordAppendOutcome(activity, result.HasErrors, result.HasConcurrencyViolations, result.HasConstraintViolations);

    static Activity RecordAppendOutcome(Activity activity, bool hasErrors, bool hasConcurrencyViolations, bool hasConstraintViolations)
    {
        var outcome = GetAppendOutcome(hasErrors, hasConcurrencyViolations, hasConstraintViolations);
        activity.SetTag(AppendOutcome, outcome);
        if (outcome != "success")
        {
            activity.SetStatus(ActivityStatusCode.Error);
            if (activity.GetTagItem(ErrorType) is null)
            {
                activity.SetTag(ErrorType, outcome == "error" ? "append_error" : outcome);
            }
        }
        return activity;
    }

    static string GetAppendOutcome(bool hasErrors, bool hasConcurrencyViolations, bool hasConstraintViolations) =>
        (hasErrors, hasConcurrencyViolations, hasConstraintViolations) switch
        {
            (true, _, _) => "error",
            (_, true, _) => "concurrency_violation",
            (_, _, true) => "constraint_violation",
            _ => "success"
        };
}
