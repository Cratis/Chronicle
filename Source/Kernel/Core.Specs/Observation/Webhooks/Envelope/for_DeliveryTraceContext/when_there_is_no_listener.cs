// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_DeliveryTraceContext;

public class when_there_is_no_listener : Specification
{
    string _traceParent;
    Activity _current;

    void Because()
    {
        _current = Activity.Current!;
        Activity.Current = null;
        using var trace = DeliveryTraceContext.Create(CorrelationId.NotSet);
        _traceParent = trace.TraceParent;
        Activity.Current = _current;
    }

    [Fact] void should_supply_valid_w3c_context() => ActivityContext.TryParse(_traceParent, null, out _).ShouldBeTrue();
    [Fact] void should_preserve_the_ambient_activity() => Activity.Current.ShouldEqual(_current);
}
