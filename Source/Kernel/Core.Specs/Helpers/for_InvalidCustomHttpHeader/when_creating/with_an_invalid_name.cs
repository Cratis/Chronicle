// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_InvalidCustomHttpHeader.when_creating;

public class with_an_invalid_name : Specification
{
    const string Secret = "do-not-show-{PropertyValue}";
    InvalidCustomHttpHeader _exception;

    void Because() => _exception = new($"Authorization: Bearer {Secret}");

    [Fact] void should_not_include_the_name() => _exception.Message.Contains(Secret, StringComparison.Ordinal).ShouldBeFalse();
}
