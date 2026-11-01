// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>Records low-cardinality outcomes without exception messages or payloads.</summary>
internal static class SpanOutcome
{
    const string ErrorType = "error.type";
    const string AppendOutcome = "cratis.chronicle.append.outcome";

    /// <summary>Records an exception's type, never its message.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="exception">The failure.</param>
    /// <returns>The activity with the exception type recorded.</returns>
    internal static Activity RecordFailure(this Activity activity, Exception exception) => activity.RecordFailure(exception.GetType().FullName!);

    /// <summary>Records a low-cardinality failure code.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="errorType">The failure type or code.</param>
    /// <returns>The activity with the failure code recorded.</returns>
    internal static Activity RecordFailure(this Activity activity, string errorType) => activity.SetStatus(ActivityStatusCode.Error).SetTag(ErrorType, errorType);

    /// <summary>Records an append result without its messages.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="result">The append result.</param>
    /// <returns>The activity with the append outcome recorded.</returns>
    internal static Activity RecordAppendOutcome(this Activity activity, IAppendResult result)
    {
        var outcome = GetAppendOutcome(result);
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

    static string GetAppendOutcome(IAppendResult result) =>
        (result.HasErrors, result.HasConcurrencyViolations, result.HasConstraintViolations) switch
        {
            (true, _, _) => "error",
            (_, true, _) => "concurrency_violation",
            (_, _, true) => "constraint_violation",
            _ => "success"
        };
}
