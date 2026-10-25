// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class with_ambient_state_on_the_caller : given.a_receiver
{
    readonly AsyncLocal<string?> _ambient = new();
    string _seen;
    Activity _activity;
    Activity _seenActivity;
    string _after;

    void Establish() => _reactors.Deliver(Arg.Any<ReactorId>(), Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>()).Returns(_ =>
    {
        _seen = _ambient.Value;
        _seenActivity = Activity.Current;
        return new ReactorResult { State = ObservationState.Success, Partition = "order-42", ExceptionStackTrace = string.Empty };
    });

    async Task Because()
    {
        using var activity = new Activity("caller").Start();
        _activity = activity;
        _ambient.Value = "request-unit-of-work";
        try
        {
            await Receive();
            _after = _ambient.Value;
        }
        finally
        {
            _ambient.Value = null;
        }
    }

    [Fact] void should_not_flow_the_callers_ambient_state() => _seen.ShouldBeNull();
    [Fact] void should_keep_the_callers_activity() => _seenActivity.ShouldEqual(_activity);
    [Fact] void should_not_clear_the_callers_state() => _after.ShouldEqual("request-unit-of-work");
}
