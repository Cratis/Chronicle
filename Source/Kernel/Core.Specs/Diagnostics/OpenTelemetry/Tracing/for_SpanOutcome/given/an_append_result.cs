// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_SpanOutcome.given;

/// <summary>Dependencies for recording append outcomes.</summary>
public class an_append_result : Specification
{
    protected Activity _activity;
    protected Activity _manyActivity;

    void Establish()
    {
        _activity = new Activity("append").Start();
        _manyActivity = new Activity("append-many").Start();
    }

    protected void Record(AppendResult result)
    {
        _activity.RecordAppendOutcome(result);
        _manyActivity.RecordAppendOutcome(new AppendManyResult
        {
            Errors = result.Errors,
            ConstraintViolations = result.ConstraintViolations,
            ConcurrencyViolations = result.ConcurrencyViolation is null ? [] : [result.ConcurrencyViolation]
        });
    }

    void Destroy()
    {
        _manyActivity.Dispose();
        _activity.Dispose();
    }
}
