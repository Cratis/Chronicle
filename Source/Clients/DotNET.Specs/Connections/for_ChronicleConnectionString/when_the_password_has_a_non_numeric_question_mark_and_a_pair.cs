// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_the_password_has_a_non_numeric_question_mark_and_a_pair : Specification
{
    ChronicleConnectionString _parsed;
    Exception _constructed;
    string _redacted;
    string _message;

    void Because()
    {
        _constructed = Catch.Exception(() => _parsed = new ChronicleConnectionString("chronicle://alice:pa?k=v@host:35000"));
        _redacted = ChronicleConnectionString.Redact("chronicle://alice:pa?k=v@host:35000");
        _message = new ConnectionUnavailable("chronicle://alice:pa?k=v@host:35000").Message;
    }

    [Fact] void should_reject_the_connection_string() => _constructed.ShouldBeOfExactType<InvalidUserInfo>();
    [Fact] void should_not_expose_alice_in_the_rejection() => _constructed.Message.Contains("alice", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_password_fragment_in_the_rejection() => _constructed.Message.Contains("pa?", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_alice_in_the_redacted_text() => _redacted.Contains("alice", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_password_fragment_in_the_redacted_text() => _redacted.Contains("pa?", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_alice_in_the_unavailable_message() => _message.Contains("alice", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_password_fragment_in_the_unavailable_message() => _message.Contains("pa?", StringComparison.Ordinal).ShouldBeFalse();
}
