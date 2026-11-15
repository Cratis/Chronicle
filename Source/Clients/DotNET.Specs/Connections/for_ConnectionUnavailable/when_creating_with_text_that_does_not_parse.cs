// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionUnavailable;

public class when_creating_with_text_that_does_not_parse : Specification
{
    ConnectionUnavailable _result;

    void Because() => _result = new ConnectionUnavailable("user:s3cret");

    [Fact] void should_not_expose_the_password() => _result.Message.Contains("s3cret", StringComparison.Ordinal).ShouldBeFalse();
}
