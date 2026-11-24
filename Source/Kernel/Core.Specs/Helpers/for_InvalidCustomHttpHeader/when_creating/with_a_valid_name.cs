// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_InvalidCustomHttpHeader.when_creating;

public class with_a_valid_name : Specification
{
    InvalidCustomHttpHeader _exception;

    void Because() => _exception = new("X-Custom");

    [Fact] void should_include_the_name() => _exception.Message.Contains("'X-Custom'", StringComparison.Ordinal).ShouldBeTrue();
}
