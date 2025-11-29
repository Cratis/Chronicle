// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventStreamTypeValuesProvider.when_providing;

public class with_event_stream_type_attribute : Specification
{
    EventStreamTypeValuesProvider _provider;
    CommandContextValues _result;
    TestCommand _command;

    void Establish()
    {
        _provider = new EventStreamTypeValuesProvider();
        _command = new TestCommand();
    }

    void Because() => _result = _provider.Provide(_command);

    [Fact] void should_return_event_stream_type_value() => _result.ContainsKey(WellKnownCommandContextKeys.EventStreamType).ShouldBeTrue();
    [Fact] void should_have_correct_event_stream_type() => ((EventStreamType)_result[WellKnownCommandContextKeys.EventStreamType]).Value.ShouldEqual("Onboarding");

    [EventStreamType("Onboarding")]
    record TestCommand;
}
