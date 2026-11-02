// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_connection_string_text_that_does_not_parse : Specification
{
    string _result;
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _result = ChronicleConnectionString.Redact("chronicle://user:s3cret/x@host"));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_not_expose_the_password() => _result.Contains("s3cret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_return_the_placeholder() => _result.ShouldEqual(ChronicleConnectionString.HiddenConnectionString);
}
