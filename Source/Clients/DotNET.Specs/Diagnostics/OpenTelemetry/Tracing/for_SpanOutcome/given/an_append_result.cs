// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_SpanOutcome.given;

/// <summary>Dependencies for recording an append result.</summary>
public class an_append_result : Specification
{
    protected Activity _activity;
    protected IAppendResult _result;

    void Establish()
    {
        _activity = new Activity("append").Start();
        _result = Substitute.For<IAppendResult>();
    }

    void Destroy() => _activity.Dispose();
}
