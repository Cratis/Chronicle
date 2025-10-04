// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_checking_can_handle;

public class with_null_value : given.a_single_event_command_response_value_handler
{
    bool _result;

    void Because() => _result = _handler.CanHandle(_commandContext, null!);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
