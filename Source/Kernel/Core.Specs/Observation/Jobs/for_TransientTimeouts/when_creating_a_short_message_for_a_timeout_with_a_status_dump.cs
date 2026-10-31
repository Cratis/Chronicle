// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_creating_a_short_message_for_a_timeout_with_a_status_dump : Specification
{
    string _message;

    void Because()
    {
        var dump = "Response did not arrive on time in 00:00:30 for message: Request [...]\nLast known status is IsExecuting: False\n" + new string('x', 7000);
        _message = TransientTimeouts.ShortMessageFor(new TimeoutException(dump));
    }

    [Fact] void should_be_a_single_line() => _message.ShouldNotContain("\n");
    [Fact] void should_be_short() => (_message.Length <= TransientTimeouts.MaximumMessageLength + 3).ShouldBeTrue();
    [Fact] void should_keep_the_reason() => _message.ShouldContain("Response did not arrive on time");
}
