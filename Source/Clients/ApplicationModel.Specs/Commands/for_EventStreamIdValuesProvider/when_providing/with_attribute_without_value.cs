// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Applications.Commands.for_EventStreamIdValuesProvider.when_providing;

public class with_attribute_without_value : Specification
{
    EventStreamIdValuesProvider _provider;
    CommandContextValues _result;
    TestCommand _command;

    void Establish()
    {
        _provider = new EventStreamIdValuesProvider();
        _command = new TestCommand();
    }

    void Because() => _result = _provider.Provide(_command);

    [Fact] void should_return_empty_values() => _result.Count.ShouldEqual(0);

    [EventStreamId]
    record TestCommand;
}
