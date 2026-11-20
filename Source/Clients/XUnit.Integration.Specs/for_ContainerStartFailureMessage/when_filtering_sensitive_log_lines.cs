// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ContainerStartFailureMessage;

public class when_filtering_sensitive_log_lines : Specification
{
    string[] _sensitiveLines;
    string _logs;
    string _message;

    void Establish()
    {
        _sensitiveLines =
        [
            "password=example-not-a-secret", "PASSWD=example-only", "pwd=example-only",
            "credential=example-only", "secret=example-only", "token=example-only",
            "authorization=example-only", "authentication=example-only",
            "api_key=example-only", "private-key=example-only",
            "mongodb://example.invalid", "mongodb+srv://example.invalid",
            "http://example.invalid", "https://example.invalid"
        ];
        _logs = string.Join('\n', new[] { "earlier ordinary line" }
            .Concat(Enumerable.Range(0, 20).Select(index => $"ordinary line {index}"))
            .Concat(_sensitiveLines)
            .Concat(Enumerable.Repeat("password=example-not-a-secret", 20)));
    }

    void Because() => _message = ContainerStartFailureMessage.Describe("example/kernel:local", 10, 1, _logs, 20);

    [Fact] void should_omit_credential_like_values() => _message.Contains("example-not-a-secret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_omit_every_sensitive_line() => _sensitiveLines.Any(line => _message.Contains(line, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_filter_before_taking_the_tail() => _message.Contains("ordinary line 0" + Environment.NewLine, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_last_ordinary_line() => _message.EndsWith("ordinary line 19", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_omit_earlier_ordinary_lines() => _message.Contains("earlier ordinary line", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_bound_the_filtered_tail_to_twenty_lines() => (_message.Split(Environment.NewLine).Length - 1).ShouldEqual(20);
}
