// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Applications.Commands.for_EventStreamTypeValuesProvider.when_providing;

public class without_event_stream_type_attribute : Specification
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

    [Fact] void should_return_empty_values() => _result.ShouldBeEmpty();

    record TestCommand;
}
