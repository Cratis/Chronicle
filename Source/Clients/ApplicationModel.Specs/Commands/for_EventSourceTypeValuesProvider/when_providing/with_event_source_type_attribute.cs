// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceTypeValuesProvider.when_providing;

public class with_event_source_type_attribute : Specification
{
    EventSourceTypeValuesProvider _provider;
    CommandContextValues _result;
    TestCommand _command;

    void Establish()
    {
        _provider = new EventSourceTypeValuesProvider();
        _command = new TestCommand();
    }

    void Because() => _result = _provider.Provide(_command);

    [Fact] void should_return_event_source_type_value() => _result.ContainsKey(WellKnownCommandContextKeys.EventSourceType).ShouldBeTrue();
    [Fact] void should_have_correct_event_source_type() => ((EventSourceType)_result[WellKnownCommandContextKeys.EventSourceType]).Value.ShouldEqual("TestAccount");

    [EventSourceType("TestAccount")]
    record TestCommand;
}
